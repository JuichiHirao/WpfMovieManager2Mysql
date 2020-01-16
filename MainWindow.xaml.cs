using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using WpfMovieManager2Mysql.common;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Text;
using WpfMovieManager2Mysql;
using WpfMovieManager2.collection;
using WpfMovieManager2.data;
using WpfMovieManager2.common;
using System.Linq;

namespace WpfMovieManager2Mysql
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MAIN_COLUMN_NO_CONTROL = 0;
        const int MAIN_COLUMN_NO_GROUP = 1;
        const int MAIN_COLUMN_NO_LIST = 2;
        const int MAIN_COLUMN_NO_CONTROL_CONTENTS = 3;
        const int MAIN_COLUMN_NO_CONTENTS = 4;

        const int CONTENTS_VISIBLE_KIND_IMAGE = 1;
        const int CONTENTS_VISIBLE_KIND_DETAIL = 2;
        const int CONTENTS_VISIBLE_KIND_MATCH = 3;

        Player Player;

        MySqlDbConnection dbcon;
        MySqlDbConnection dockerMysqlConn = null;

        StoreCollection ColViewStore;
        FavCollection ColViewFav;

        MovieContentsFilterAndSort ColViewMovieContents;

        SiteDetail ColViewSiteDetail;
        detail.FileDetail ColViewFileDetail;

        // 画面情報
        string dispinfoGroupButton = "";
        bool dispinfoIsGroupVisible = false; // グループの表示を有効にする場合はtrue
        bool dispinfoIsGroupAddVisible = false; // グループ追加の表示を有効にする場合はtrue
        bool dispinfoIsGroupFavAddVisible = false; // グループ追加の表示を有効にする場合はtrue
        bool dispinfoIsContentsVisible = false;
        int dispinfoContentsVisibleKind = 0;
        string dispinfoGroupVisibleType = "file";
        contents.TargetList targetList = null;

        bool dispctrlIsDisplayImageUseColumn = false;


        double dispctrlContentsWidth = 800;

        MovieContents dispinfoSelectContents = null;
        MovieGroupData dispinfoSelectGroup = null;
        FavData dispinfoSelectFavData = null;

        BackgroundWorker bgworkerFileDetailCopy;
        Stopwatch stopwatchFileDetailCopy = new Stopwatch();

        public MainWindow()
        {

            InitializeComponent();

            dockerMysqlConn = new MySqlDbConnection(0);

            dbcon = new MySqlDbConnection();
            Player = new Player();

            bgworkerFileDetailCopy = new BackgroundWorker();
            bgworkerFileDetailCopy.WorkerSupportsCancellation = true;
            bgworkerFileDetailCopy.WorkerReportsProgress = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ColViewMovieContents = new MovieContentsFilterAndSort(dbcon);
                ColViewStore = new StoreCollection();
                ColViewFav = new FavCollection();

                dgridMovieContents.ItemsSource = ColViewMovieContents.ColViewListMovieContents;
                dgridMovieGroup.ItemsSource = ColViewStore.ColViewListData;
                dgridMovieGroup.Visibility = Visibility.Collapsed;

                dgridGroupFav.ItemsSource = ColViewFav.ColViewListData;
                dgridGroupFav.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            dgridMovieContents_SizeChanged(null, null);

            // Sortの初期値を設定
            cmbContentsSort.SelectedIndex = 0;
            btnSortOrder.Content = "↑";
            OnSortButtonClick(cmbContentsSort, null);

            txtStatusBar.IsReadOnly = true;
            txtStatusBar.Width = statusbarMain.ActualWidth;

            // RowColorを初期設定にする
            cmbColor_SelectionChanged(null, null);

            txtSearch.Focus();

            // 一度だけ
            //TemporaryTools tempTools = new TemporaryTools();
            //tempTools.DbExportGroupFromSiteStore();
        }

        private void OnSiteDetailKindButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Content.ToString().IndexOf("Image") >= 0)
                ColViewSiteDetail.Execute(SiteDetail.FILTER_KIND_IMAGE);
            else if (button.Content.ToString().IndexOf("Movie") >= 0)
                ColViewSiteDetail.Execute(SiteDetail.FILTER_KIND_MOVIE);
            else if (button.Content.ToString().IndexOf("List") >= 0)
                ColViewSiteDetail.Execute(SiteDetail.FILTER_KIND_LIST);
            else if (button.Content.ToString().IndexOf("All") >= 0)
                ColViewSiteDetail.Execute(0);
        }

        private void OnLayoutSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutChange();

            txtStatusBar.IsReadOnly = true;
            txtStatusBar.Width = stsbaritemDispDetail.ActualWidth;
        }

        // SizeChangedでNameの表示幅を広くする
        private void dgridMovieContents_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int COL_FILECOUNT = 0;
            int COL_EXTENSION = 1;
            int COL_NAME = 2;
            int COL_RATING = 3;
            dgridMovieContents.Columns[COL_FILECOUNT].Width = 40;
            dgridMovieContents.Columns[COL_EXTENSION].Width = 60;
            dgridMovieContents.Columns[COL_RATING].Width = 70;

            dgridMovieContents.Columns[COL_NAME].Width = CalcurateColumnWidth(dgridMovieContents);
        }

        private double CalcurateColumnWidth(DataGrid datagrid)
        {
            double winX = lgridMovieContents.ActualWidth - 20;
            double colTotal = 0;
            foreach (DataGridColumn col in datagrid.Columns)
            {
                if (col.Header != null && col.Header.Equals("Name"))
                    continue;

                DataGridLength colw = col.ActualWidth;
                double w = colw.DesiredValue;
                colTotal += w;
            }

            return winX - colTotal - 25; // ScrollBarが表示されない場合は8
        }

        private void LayoutChange()
        {
            double ColWidth1Group = 0;
            if (dispinfoIsGroupVisible)
            {
                dgridMovieGroup.Visibility = Visibility.Visible;
                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_GROUP].Width = new GridLength(500);

                dgridMovieGroup.Width = lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_GROUP].Width.Value - 10;
                ColWidth1Group = 500;

                // dispinfoIsGroupFavAddVisible
                if (dispinfoGroupVisibleType == "actress")
                {
                    lgridMovieGroup.Visibility = Visibility.Collapsed;
                    lgridGroupFav.Visibility = Visibility.Visible;

                    if (!dispinfoIsGroupFavAddVisible)
                        lgridGroupFav.RowDefinitions[1].Height = new GridLength(0);
                    else
                        lgridGroupFav.RowDefinitions[1].Height = new GridLength(370);
                }
                else
                {
                    lgridGroupFav.Visibility = Visibility.Collapsed;
                    lgridMovieGroup.Visibility = Visibility.Visible;

                    dgridMovieGroup_SelectionChanged(null, null);

                    if (dispinfoGroupButton == "S")
                        cmbSiteName.Visibility = Visibility.Visible;
                    else
                        cmbSiteName.Visibility = Visibility.Collapsed;

                    if (!dispinfoIsGroupAddVisible)
                        lgridMovieGroup.RowDefinitions[1].Height = new GridLength(0);
                    else
                        lgridMovieGroup.RowDefinitions[1].Height = new GridLength(370);
                }
            }
            else
            {
                dgridMovieGroup.SelectedItem = null;
                dgridMovieGroup.Visibility = Visibility.Hidden;
                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_GROUP].Width = new GridLength(0);
            }

            // List,Contentsの有効な表示領域幅 ＝ グループボタン領域幅 － グループリスト領域幅 － 調整幅(50)
            double VisibleWidth = this.ActualWidth - lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTROL].Width.Value - ColWidth1Group - 50;
            
            double ColWidth2List = 0, ColWidth4Contents = 0;
            if (dispinfoIsContentsVisible)
            {
                if (VisibleWidth > dispctrlContentsWidth)
                {
                    ColWidth2List = VisibleWidth - dispctrlContentsWidth;
                    ColWidth4Contents = dispctrlContentsWidth;
                }

                lgridMain.UpdateLayout();

                lgridImageContents.Visibility = Visibility.Visible;
                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_LIST].Width = new GridLength(ColWidth2List);
                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].Width = new GridLength(ColWidth4Contents);

                if (ColWidth2List > 10) dgridMovieContents.Width = ColWidth2List - 10;
                dgridMovieContents.UpdateLayout();

                btnContentsWide.Visibility = Visibility.Visible;
                btnContentsNarrow.Visibility = Visibility.Visible;
                btnContentsOpen.Visibility = Visibility.Collapsed;
                btnCloseImageContents.Visibility = Visibility.Visible;
            }
            else
            {
                lgridImageContents.Visibility = Visibility.Collapsed;
                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].Width = new GridLength(0);
                lgridMain.UpdateLayout();

                lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_LIST].Width = new GridLength(VisibleWidth);

                dgridMovieContents.Width = VisibleWidth - 10;
                dgridMovieContents.UpdateLayout();

                btnContentsWide.Visibility = Visibility.Collapsed;
                btnContentsNarrow.Visibility = Visibility.Collapsed;
                btnContentsOpen.Visibility = Visibility.Visible;
                btnCloseImageContents.Visibility = Visibility.Collapsed;
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            ColViewMovieContents.Clear();
            ColViewMovieContents.SetSearchText(txtSearch.Text);
            ColViewMovieContents.Execute();
        }

        private void OnSortButtonClick(object sender, RoutedEventArgs e)
        {
            string sortColumns = "";
            ListSortDirection sortOrder;

            Button btn;
            ComboBox cmb;

            bool isGroup = false;
            bool isReverse = false;

            btn = sender as Button;
            if (btn == null)
            {
                cmb = sender as ComboBox;
                if (cmb == null)
                {
                    return;
                }
                else
                {
                    if (cmb.Name.IndexOf("Group") >= 0)
                        isGroup = true;
                }
            }
            // 押下されたのがボタンの場合はisReverseによって順を逆にしてボタンのContensも変える
            else
            {
                isReverse = true;
                if (btn.Name.IndexOf("Group") >= 0)
                    isGroup = true;
            }

            if (isGroup)
            {
                sortOrder = GetSortOrder(btnSortGroupOrder, isReverse);
                sortColumns = Convert.ToString(cmbSortGroup.SelectedValue);

                ColViewStore.SetSort(sortColumns, sortOrder);
                ColViewStore.Execute();
            }
            else
            {
                sortOrder = GetSortOrder(btnSortOrder, isReverse);
                sortColumns = Convert.ToString(cmbContentsSort.SelectedValue);

                ColViewMovieContents.SetSort(Convert.ToString(cmbContentsSort.SelectedValue), sortOrder);
                ColViewStore.Execute();
            }
        }
        private ListSortDirection GetSortOrder(Button myButton, bool myIsReverse)
        {
            ListSortDirection sortOrder;

            if (!myIsReverse)
            {
                if (myButton.Content.Equals("↑"))
                    sortOrder = ListSortDirection.Descending;
                else
                    sortOrder = ListSortDirection.Ascending;

                return sortOrder;
            }
            if (myButton != null && (myButton.Content.Equals("↑")
                || myButton.Content.Equals("↓")))
            {
                if (myButton.Content.Equals("↑"))
                {
                    sortOrder = ListSortDirection.Ascending;
                    myButton.Content = "↓";
                }
                else
                {
                    sortOrder = ListSortDirection.Descending;
                    myButton.Content = "↑";
                }
            }
            else
            {
                sortOrder = ListSortDirection.Ascending;
            }

            return sortOrder;
        }

        private void OnGroupButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleButton clickButton = sender as ToggleButton;

            if (clickButton == null)
                return;

            bool chk = Convert.ToBoolean(clickButton.IsChecked);
            if (chk)
            {
                List<ToggleButton> listtbtn = CommonMethod.FindVisualChild<ToggleButton>(wrappGroupButton, "ToggleButton");

                foreach(ToggleButton tbutton in listtbtn)
                {
                    if (clickButton == tbutton)
                        continue;

                    tbutton.IsChecked = false;
                }
            }
            else
            {
                dispinfoGroupButton = "";
                dispinfoIsGroupVisible = false;

                LayoutChange();

                return;
            }

            dispinfoGroupButton =  clickButton.Content.ToString();
            dispinfoGroupVisibleType = CommonMethod.ToggleButtonType[dispinfoGroupButton];
            dispinfoIsGroupVisible = true;

            ColViewMovieContents.Clear();
            string sortColumns = Convert.ToString(cmbContentsSort.SelectedValue);
            ColViewMovieContents.SetSort(sortColumns, GetSortOrder(btnSortOrder, false));

            if (dispinfoGroupVisibleType == "actress")
            {
                ColViewFav.SetSort("UpdatedAt", ListSortDirection.Descending);
                ColViewFav.SetType(dispinfoGroupVisibleType);
                dgridGroupFav.Visibility = Visibility.Visible;
                ColViewFav.Execute();
            }
            else
            {
                ColViewStore.SetSort("UpdatedAt", ListSortDirection.Descending);
                ColViewStore.SetType(dispinfoGroupVisibleType);
                if (dispinfoGroupButton != "S")
                    ColViewStore.SetSiteName("");
                ColViewStore.Execute();
            }
            LayoutChange();
        }

        private void dgridMovieGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSelectGroup = (MovieGroupData)dgridMovieGroup.SelectedItem;
            if (dispinfoSelectGroup == null)
                return;

            // 画像表示はクリア
            OnDisplayImage(null, dispinfoSelectGroup);

            // 選択されているグループで表示
            StoreGroupInfoData filesInfo = ColViewMovieContents.ClearAndExecute(dispinfoSelectGroup);

            this.Title = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
            txtbGroupInfo.Text = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
        }

        private void dgridGroupFav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSelectFavData = (FavData)dgridGroupFav.SelectedItem;
            if (dispinfoSelectFavData == null)
                return;

            // 画像表示はクリア
            // OnDisplayImage(null, dispinfoSelectFavData);

            // 選択されているグループで表示
            StoreGroupInfoData filesInfo = ColViewMovieContents.ClearAndExecute(dispinfoSelectFavData);

            this.Title = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
            txtbGroupInfo.Text = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
        }

        private void txtSearchGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            ColViewStore.SetSearchText(txtSearchGroup.Text);
            ColViewStore.Execute();
        }

        private void OnTextSearchFavTextChanged(object sender, TextChangedEventArgs e)
        {
            ColViewFav.SetSearchText(txtFavSearch.Text);
            ColViewFav.Execute();
        }

        private void OnCloseGroupFilter_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoIsGroupAddVisible)
                dispinfoIsGroupAddVisible  = false;
            else
                dispinfoIsGroupVisible = false;

            LayoutChange();
        }

        private void OnCloseGroupFavFilter_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoIsGroupFavAddVisible)
            {
                dispinfoIsGroupFavAddVisible = false;
                dispinfoIsGroupVisible = false;

            }
            else
                dispinfoIsGroupVisible = false;

            LayoutChange();
        }

        private void cmbSiteName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectSiteName = Convert.ToString(cmbSiteName.SelectedItem);

            ColViewStore.SetSiteName(selectSiteName);
            ColViewStore.Execute();

            ColViewMovieContents.Clear();
            ColViewMovieContents.SetSiteContents(selectSiteName, "");
            ColViewMovieContents.Execute();
        }

        private void DisplayMedia(FileInfo myFileInfo, System.Windows.Controls.Image myImage, MediaElement myMedia, int myRowSpanProperty, int myColumnSpanProperty)
        {
            if (myFileInfo.Extension == ".gif")
            {
                BitmapImage bitmapImage = (BitmapImage)imageSitesImageOne.Source;
                myImage.Visibility = Visibility.Collapsed;
                myMedia.Visibility = Visibility.Visible;
                myMedia.SetValue(Grid.RowSpanProperty, myRowSpanProperty);
                myMedia.SetValue(Grid.ColumnSpanProperty, myColumnSpanProperty);

                //mediaSitesImageGifOne.Width = lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].ActualWidth;
                //mediaSitesImageGifOne.Height = (imageSitesImageOne.Width / bitmapImage.Width) * bitmapImage.Height;
                // Source="file://C:\waiting.GIF"
                myMedia.Source = new Uri("file://" + myFileInfo.FullName);
            }
            else
            {
                BitmapImage bitmapImage = (BitmapImage)imageSitesImageOne.Source;
                myMedia.Visibility = Visibility.Collapsed;
                myImage.Visibility = Visibility.Visible;
                myImage.SetValue(Grid.RowSpanProperty, myRowSpanProperty);
                myImage.SetValue(Grid.ColumnSpanProperty, myColumnSpanProperty);

                //imageSitesImageOne.Width = lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].ActualWidth;
                //imageSitesImageOne.Height = (imageSitesImageOne.Width / bitmapImage.Width) * bitmapImage.Height;

                myImage.Source = ImageMethod.GetImageStream(myFileInfo.FullName);
            }
        }

        private void OnDisplayImage(MovieContents myMovieContents, MovieGroupData myTargetGroup)
        {
            // パラメータがnullの場合は画像表示を全てクリア
            if (myMovieContents == null)
            {
                imagePackage.Source = null;
                return;
            }

            if (myMovieContents.PackageImage == null && myMovieContents.ThumbnailImage == null && 
                (myMovieContents.ImageList == null || myMovieContents.ImageList.Count <= 0))
                return;

            lgridImageContents.RowDefinitions[0].Height = GridLength.Auto;
            lgridImageContents.RowDefinitions[1].Height = GridLength.Auto;
            lgridImageContents.RowDefinitions[2].Height = GridLength.Auto;
            lgridImageContents.RowDefinitions[3].Height = GridLength.Auto;
            imageSitesImageOne.Source = null;
            mediaSitesImageGifOne.Source = null;
            imageSitesImageTwo.Source = null;
            mediaSitesImageGifTwo.Source = null;
            imageSitesImageThree.Source = null;
            mediaSitesImageGifThree.Source = null;
            imageSitesImageFour.Source = null;
            mediaSitesImageGifFour.Source = null;

            if (myMovieContents.PackageImage != null && myMovieContents.ThumbnailImage != null)
            {
                if (myMovieContents.MovieList.Count > 0)
                    txtStatusBar.Text = myMovieContents.MovieList[0].FullName;
                txtStatusBarFileLength.Text = CommonMethod.GetDisplaySize(myMovieContents.Size);
                imagePackage.Source = ImageMethod.GetImageStream(myMovieContents.PackageImage.FullName);

                imageSitesImageOne.Source = ImageMethod.GetImageStream(dispinfoSelectContents.ThumbnailImage.FullName);
                imageSitesImageOne.ToolTip = dispinfoSelectContents.ThumbnailImage.Name;
                imageSitesImageTwo.Visibility = Visibility.Collapsed;
                imageSitesImageThree.Visibility = Visibility.Collapsed;
                imageSitesImageFour.Visibility = Visibility.Collapsed;

                imageSitesImageOne.SetValue(Grid.RowSpanProperty, 4);
                imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 2);

                BitmapImage bitmapImage = (BitmapImage)imageSitesImageOne.Source;
                imageSitesImageOne.Width = lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].ActualWidth;
                imageSitesImageOne.Height = (imageSitesImageOne.Width / bitmapImage.Width) * bitmapImage.Height;
                imageSitesImageOne.Stretch = Stretch.Uniform;

                return;
            }

            imagePackage.Source = null;


            bool existPackage = false;
            if (dispinfoSelectContents.PackageImage != null)
                existPackage = true;

            int currentLength = 0;
            if (dispinfoSelectContents.CurrentImages != null)
                currentLength = dispinfoSelectContents.CurrentImages.Length;


            //Debug.Print("Window.ActualHeight [" + this.ActualHeight + "] AWidth [" + this.ActualWidth + "]");
            //Debug.Print("Package [" + existPackage + "]  ImageList.Count [" + myMovieContents.ImageList.Count + "]個 Current [" + currentLength + "]個");
            //Debug.Print("lgridImageContents AHeight [" + lgridImageContents.ActualHeight + "] AWidth [" + lgridImageContents.ActualWidth + "]");
            //double visibleHeight = lgridImageContents.DesiredSize.Height - lgridImageContents.Margin.Top - lgridImageContents.Margin.Bottom;
            //double visibleWidth = lgridImageContents.DesiredSize.Width - lgridImageContents.Margin.Left - lgridImageContents.Margin.Right;
            //Debug.Print("lgridImageContents Height有効 [" + visibleHeight + "]  DesiredSize Height [" + lgridImageContents.DesiredSize.Height + "] ");
            //Debug.Print("lgridImageContents Width有効 [" + visibleWidth + "]  DesiredSize Width [" + lgridImageContents.DesiredSize.Width + "] ");
            //Debug.Print("imageSitesImageOne.ActualWidth [" + imageSitesImageOne.ActualWidth + "]");
            //Debug.Print("imageSitesImageOne.ActualHeight [" + imageSitesImageOne.ActualHeight + "]");

            double visibleWindowActualHeight = this.ActualHeight - 220;
            //double visibleWindowActualWidth = lgridImageContents.DesiredSize.Width - lgridImageContents.Margin.Left - lgridImageContents.Margin.Right;
            double visibleWindowActualWidth = lgridImageContents.ActualWidth - lgridImageContents.Margin.Left - lgridImageContents.Margin.Right;
            double abc = lgridImageContents.ActualWidth;

            if (visibleWindowActualHeight <= 0)
                visibleWindowActualHeight = 100;

            if (visibleWindowActualWidth <= 0)
                visibleWindowActualWidth = 100;
            
            int RowSpanProperty = 1;
            int ColumnSpanProperty = 2;
            double height = 0, width = 0;

            if (dispinfoSelectContents.ImageList.Count > 4)
            {
                //Debug.Print("lgridImageContents.ColumnDefinitions[0].ActualWidth [" + lgridImageContents.ColumnDefinitions[0].ActualWidth + "]");
                //Debug.Print("lgridImageContents.ColumnDefinitions[1].ActualWidth [" + lgridImageContents.ColumnDefinitions[1].ActualWidth + "]");
                //Debug.Print("imageSitesImageOne.ActualWidth [" + imageSitesImageOne.ActualWidth + "]");
                //Debug.Print("imageSitesImageOne.ActualHeight [" + imageSitesImageOne.ActualHeight + "]");
                height = visibleWindowActualHeight / 2;
                if (visibleWindowActualWidth > 0)
                    width = visibleWindowActualWidth / 2;
                ColumnSpanProperty = 1;

                if (dispctrlIsDisplayImageUseColumn == false)
                {
                    imageSitesImageOne.SetValue(Grid.RowProperty, 0);
                    imageSitesImageOne.SetValue(Grid.RowSpanProperty, 1);
                    imageSitesImageOne.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 1);
                    imageSitesImageTwo.SetValue(Grid.RowProperty, 0);
                    imageSitesImageTwo.SetValue(Grid.ColumnProperty, 1);
                    imageSitesImageThree.SetValue(Grid.RowProperty, 1);
                    imageSitesImageThree.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageFour.SetValue(Grid.RowProperty, 1);
                    imageSitesImageFour.SetValue(Grid.ColumnProperty, 1);
                    dispctrlIsDisplayImageUseColumn = true;
                    Debug.Print("dispctrlIsDisplayImageUseColumn = true になりました");
                }
            }
            else
            {
                height = visibleWindowActualHeight / dispinfoSelectContents.CurrentImages.Length;
                ColumnSpanProperty = 2;
                imageSitesImageOne.Width = visibleWindowActualWidth;
                if (dispctrlIsDisplayImageUseColumn == true)
                {
                    imageSitesImageOne.SetValue(Grid.RowProperty, 0);
                    imageSitesImageOne.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 2);
                    imageSitesImageTwo.SetValue(Grid.RowProperty, 1);
                    imageSitesImageTwo.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageTwo.SetValue(Grid.ColumnSpanProperty, 2);
                    imageSitesImageThree.SetValue(Grid.RowProperty, 2);
                    imageSitesImageThree.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageThree.SetValue(Grid.ColumnSpanProperty, 2);
                    imageSitesImageFour.SetValue(Grid.RowProperty, 3);
                    imageSitesImageFour.SetValue(Grid.ColumnProperty, 0);
                    imageSitesImageFour.SetValue(Grid.ColumnSpanProperty, 2);
                    dispctrlIsDisplayImageUseColumn = false;
                }
            }

            if (dispinfoSelectContents.CurrentImages.Length == 1)
            {
                imageSitesImageOne.Height = visibleWindowActualHeight;

                lgridImageContents.RowDefinitions[0].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[1].Height = new GridLength(0);
                lgridImageContents.RowDefinitions[2].Height = new GridLength(0);
                lgridImageContents.RowDefinitions[3].Height = new GridLength(0);
            }

            if (dispinfoSelectContents.CurrentImages.Length == 2)
            {
                lgridImageContents.RowDefinitions[0].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[1].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[2].Height = new GridLength(0);
                lgridImageContents.RowDefinitions[3].Height = new GridLength(0);
            }
            else if (dispinfoSelectContents.CurrentImages.Length == 3)
            {
                lgridImageContents.RowDefinitions[0].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[1].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[2].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[3].Height = new GridLength(0);
            }
            //else if (dispinfoSelectContents.ImageList.Count == 4)
            //    RowSpanProperty = 1;
            else if (dispinfoSelectContents.ImageList.Count > 4)
            {
                lgridImageContents.RowDefinitions[0].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[1].Height = new GridLength(height);
                lgridImageContents.RowDefinitions[2].Height = new GridLength(0);
                lgridImageContents.RowDefinitions[3].Height = new GridLength(0);

                lgridImageContents.ColumnDefinitions[0].Width = new GridLength(width);
                lgridImageContents.ColumnDefinitions[1].Width = new GridLength(width);

                imageSitesImageOne.Width = width;
            }

            DisplayMedia(dispinfoSelectContents.CurrentImages[0], imageSitesImageOne, mediaSitesImageGifOne, RowSpanProperty, ColumnSpanProperty);

            if (dispinfoSelectContents.CurrentImages.Length >= 2)
                DisplayMedia(dispinfoSelectContents.CurrentImages[1], imageSitesImageTwo, mediaSitesImageGifTwo, RowSpanProperty, ColumnSpanProperty);

            if (dispinfoSelectContents.CurrentImages.Length >= 3)
                DisplayMedia(dispinfoSelectContents.CurrentImages[2], imageSitesImageThree, mediaSitesImageGifThree, RowSpanProperty, ColumnSpanProperty);

            if (dispinfoSelectContents.CurrentImages.Length >= 4)
                DisplayMedia(dispinfoSelectContents.CurrentImages[3], imageSitesImageFour, mediaSitesImageGifFour, RowSpanProperty, ColumnSpanProperty);
        }
        private void dgridMovieContents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSelectContents = (MovieContents)dgridMovieContents.SelectedItem;

            if (dispinfoSelectContents != null)
            {
                txtLargeComment.Text = dispinfoSelectContents.Comment;
                cmbLargeRating.SelectedValue = dispinfoSelectContents.Rating;
            }
            else
                return;

            dispinfoSelectContents.ParseMedia();

            txtStatusBarFileDate.Text = "";

            OnDisplayImage(dispinfoSelectContents, dispinfoSelectGroup);
            if (dispinfoContentsVisibleKind == CONTENTS_VISIBLE_KIND_DETAIL)
            {
                if (dispinfoSelectContents == null)
                    return;

                if (dispinfoSelectContents.Type != "site")
                {
                    lgridSiteDetail.Visibility = Visibility.Collapsed;
                    lgridFileDetail.Visibility = Visibility.Visible;

                    ColViewFileDetail = new detail.FileDetail(dispinfoSelectContents, dispinfoSelectGroup);
                    dgridFileDetail.ItemsSource = ColViewFileDetail.listFileInfo;

                    OnRefreshFileDetailInfo(null, null);
                    btnMatchContents_Click(null, null);

                    return;
                }
                lgridSiteDetail.Visibility = Visibility.Visible;
                lgridFileDetail.Visibility = Visibility.Collapsed;

                txtSiteDetailContentsName.Text = dispinfoSelectContents.Name;
                txtSiteDetailContentsTag.Text = dispinfoSelectContents.Tag;
                txtSiteDetailContentsStoreLabel.Text = dispinfoSelectContents.StoreLabel;
                txtSiteDetailContentsFileDate.Text = dispinfoSelectContents.FileDate.ToString("yyyy/MM/dd HH:mm:ss");
                txtbSiteDetailContentsId.Text = Convert.ToString(dispinfoSelectContents.Id);
                txtbSiteDetailContentsCreatedAt.Text = dispinfoSelectContents.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss");
                txtbSiteDetailContentsUpdatedAt.Text = dispinfoSelectContents.UpdatedAt.ToString("yyyy/MM/dd HH:mm:ss");

                string sitePathname = Path.Combine(dispinfoSelectContents.Path, dispinfoSelectContents.Name);
                if (Directory.Exists(sitePathname))
                {
                    ScreenDisableBorderSiteDetail.Width = 0;
                    ScreenDisableBorderSiteDetail.Height = 0;
                    ScreenDisableBorderImageContents.Width = 0;
                    ScreenDisableBorderImageContents.Height = 0;

                    ColViewSiteDetail = new SiteDetail(sitePathname);

                    dgridSiteDetail.ItemsSource = ColViewSiteDetail.listFileInfo;
                    btnSiteDetailImage.Content = "Image (" + ColViewSiteDetail.ImageCount + ")";
                    btnSiteDetailMovie.Content = "Movie (" + ColViewSiteDetail.MovieCount + ")";
                    btnSiteDetailList.Content = "List (" + ColViewSiteDetail.ListCount + ")";

                    imageSiteDetail.Source = ImageMethod.GetImageStream(ColViewSiteDetail.StartImagePathname);

                    targetList = new contents.TargetList(sitePathname);
                    if (targetList.DisplayTargetFiles != null)
                    {
                        lstSiteDetailSelectedList.ItemsSource = targetList.DisplayTargetFiles;
                        txtbSiteDetalSelectedListCount.Text = Convert.ToString(targetList.DisplayTargetFiles.Count);
                    }
                }
                // 存在しないpathの場合
                else
                {
                    ScreenDisableBorderSiteDetail.Width = Double.NaN;
                    ScreenDisableBorderSiteDetail.Height = Double.NaN;
                    ScreenDisableBorderImageContents.Width = Double.NaN;
                    ScreenDisableBorderImageContents.Height = Double.NaN;

                    dgridSiteDetail.ItemsSource = null;
                    imageSiteDetail.Source = null;
                }
            }

            btnMatchContents_Click(null, null);
        }

        private void dgridMovieContents_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dispinfoSelectContents == null)
                return;

            try
            {
                Player.Execute(dispinfoSelectContents, "GOM", dispinfoSelectGroup);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgridMovieContents_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            foreach (PlayerInfo player in Player.GetPlayers())
            {
                foreach (var menu in dgridMovieContents.ContextMenu.Items)
                {
                    MenuItem item = menu as MenuItem;
                    if (item == null)
                        break;
                    if (item.Header.ToString().IndexOf(player.Name) >= 0)
                    {
                        dgridMovieContents.ContextMenu.Items.Remove(item);
                        break;
                    }
                }
            }
            foreach (var menu in dgridMovieContents.ContextMenu.Items)
            {
                MenuItem item = menu as MenuItem;
                if (item == null)
                    break;
                if (item.Header.ToString().Equals("選択データの追加"))
                {
                    dgridMovieContents.ContextMenu.Items.Remove(item);
                    break;
                }
            }

            foreach (PlayerInfo player in Player.GetPlayers())
            {
                MenuItem menuitem = new MenuItem();
                menuitem.Header = player.Name + "で再生";
                menuitem.Click += OnPlayExecute;

                dgridMovieContents.ContextMenu.Items.Add(menuitem);
            }

            foreach (var data in dgridMovieContents.SelectedItems)
            {
                MovieContents movieContents = data as MovieContents;

                if (movieContents.Kind == MovieContents.KIND_SITECHK_UNREGISTERED)
                {
                    MenuItem menuitem = new MenuItem();
                    menuitem.Header = "選択データの追加";
                    menuitem.Click += menuitemAddSelectedDataAdd_Click;

                    dgridMovieContents.ContextMenu.Items.Add(menuitem);
                    break;
                }
            }

        }

        private void OnPlayExecute(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;

            if (menuitem == null)
            {
                MessageBox.Show("sender as MenuItemの戻りがnull");
                return;
            }

            foreach (PlayerInfo player in Player.GetPlayers())
            {
                if (menuitem.Header.ToString().IndexOf(player.Name) >= 0)
                {
                    if (dispinfoSelectContents == null)
                        return;

                    Player.Execute(dispinfoSelectContents, player.Name, dispinfoSelectGroup);
                    return;
                }
            }
        }

        private void OnChangedRating(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            int changeRating = Convert.ToInt32(combo.SelectedItem);
            int beforeRating = 0;

            if (dispinfoSelectContents == null)
                return;

            beforeRating = dispinfoSelectContents.Rating;

            if (changeRating == beforeRating)
                return;

            dispinfoSelectContents.DbUpdateRating(changeRating, dbcon);
        }

        private void OnEditEndComment(object sender, RoutedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (dispinfoSelectContents != null && !dispinfoSelectContents.Comment.Equals(textbox.Text))
            {
                dispinfoSelectContents.Comment = textbox.Text;
                dispinfoSelectContents.DbUpdateComment(textbox.Text, dbcon);
            }
        }

        private void OnButtonClickAreaControl(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            if (button.Name.ToString().ToUpper().IndexOf("WIDE") >= 0)
                dispctrlContentsWidth = dispctrlContentsWidth + 100;
            else
                dispctrlContentsWidth = dispctrlContentsWidth - 100;

            LayoutChange();
        }

        private void btnSwitchContents_Click(object sender, RoutedEventArgs e)
        {
            Grid lgridDetail;

            if (dispinfoSelectContents == null)
                return;

            if (dispinfoSelectContents.Type == "file")
                lgridDetail = lgridFileDetail;
            else
                lgridDetail = lgridSiteDetail;

            if (lgridDetail.Visibility == Visibility.Visible)
            {
                dispinfoContentsVisibleKind = CONTENTS_VISIBLE_KIND_IMAGE;
                lgridImageContents.Visibility = Visibility.Visible;
                lgridDetail.Visibility = Visibility.Collapsed;
            }
            else
            {
                dispinfoContentsVisibleKind = CONTENTS_VISIBLE_KIND_DETAIL;
                lgridImageContents.Visibility = Visibility.Collapsed;
                lgridDetail.Visibility = Visibility.Visible;
            }

            dgridMovieContents_SelectionChanged(null, null);
        }

        // SiteDetailの行を削除、ファイルも削除
        private void btnSiteDetailRowDelete_Click(object sender, RoutedEventArgs e)
        {
            common.FileContents selSiteDetail = (common.FileContents)dgridSiteDetail.SelectedItem;
            if (selSiteDetail == null)
                return;

            string msg = "選択したファイルを削除しますか？";

            var result = MessageBox.Show(msg, "削除確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            ColViewSiteDetail.Delete(selSiteDetail);

            FileSystem.DeleteFile(
                selSiteDetail.FileInfo.FullName,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin);
        }

        // SiteDetailの選択行からlistを作成する
        private void btnSiteDetailList_Click(object sender, RoutedEventArgs e)
        {
            if (targetList.DisplayTargetFiles == null || targetList.DisplayTargetFiles.Count <= 0)
                return;

            string msg = "選択したファイルでリストを作成しますか？";

            var result = MessageBox.Show(msg, "作成確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            targetList.Export();
        }

        private void OnSiteDetailRowDelete(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                btnSiteDetailRowDelete_Click(null, null);
        }

        private void OnAddGroupFavRegisterOrEdit(object sender, RoutedEventArgs e)
        {
            // ClickしたのがButtonでは無い場合は編集ボタンを押下されたと判断
            Button button = sender as Button;
            if (button == null)
            {
                if (dispinfoSelectFavData == null)
                    return;

                txtAddFavLabel.Text = dispinfoSelectFavData.Label;
                txtAddFavName.Text = dispinfoSelectFavData.Name;
                txtAddFavComment.Text = dispinfoSelectFavData.Comment;
                cmbAddFavType.SelectedValue = dispinfoSelectFavData.Type;
                txtAddFavId.Text = Convert.ToString(dispinfoSelectFavData.Id);
                txtbAddFavMode.Text = "Edit";
            }
            else
            {
                string type = "";
                if (dispinfoSelectFavData != null)
                    type = Convert.ToString(dispinfoSelectFavData.Type);

                txtAddFavLabel.Text = "";
                txtAddFavName.Text = "";
                txtAddFavComment.Text = "";
                cmbAddFavType.SelectedValue = type;
                txtAddFavId.Text = "";
                txtbAddFavMode.Text = "Register";
            }
            dispinfoIsGroupAddVisible = true;
            dispinfoIsGroupFavAddVisible = true;
            LayoutChange();
        }

        private void OnAddGroupRegisterOrEdit(object sender, RoutedEventArgs e)
        {
            // ClickしたのがButtonでは無い場合は編集ボタンを押下されたと判断
            Button button = sender as Button;
            if (button == null)
            {
                if (dispinfoSelectGroup == null)
                    return;

                txtAddGroupLabel.Text = dispinfoSelectGroup.Label;
                txtAddGroupName1.Text = dispinfoSelectGroup.Name1;
                txtAddGroupName2.Text = dispinfoSelectGroup.Name2;
                txtAddGroupPath.Text = dispinfoSelectGroup.Path;
                cmbAddGroupKind.SelectedValue = dispinfoSelectGroup.Type;
                txtAddGroupId.Text = Convert.ToString(dispinfoSelectGroup.Id);
                txtbAddGroupMode.Text = "Edit";
            }
            else
            {
                txtAddGroupLabel.Text = "";
                txtAddGroupName1.Text = "";
                txtAddGroupName2.Text = "";
                txtAddGroupPath.Text = "";
                cmbAddGroupKind.SelectedValue = null;
                txtAddGroupId.Text = "";
                txtbAddGroupMode.Text = "Register";
            }
            dispinfoIsGroupAddVisible = true;
            dispinfoIsGroupFavAddVisible = false;
            LayoutChange();
        }
        private void OnAddGroupDelete(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectGroup == null)
                return;

            MessageBoxResult result = MessageBox.Show("DBから削除します", "削除確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            ColViewStore.Delete(dispinfoSelectGroup);
            dispinfoSelectGroup = null;
        }

        private void OnAddGroupCheck(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectGroup == null)
                return;

            //MessageBoxResult result = MessageBox.Show("フォルダのチェックをします", "チェック確認", MessageBoxButton.OKCancel);

            //if (result == MessageBoxResult.Cancel)
            //    return;

            if (dispinfoSelectGroup.Type == "site")
            {
                if (Directory.Exists(dispinfoSelectGroup.Path))
                {
                    string[] arrDir = Directory.GetDirectories(dispinfoSelectGroup.Path);
                    ColViewMovieContents.Clear();
                    ColViewMovieContents.SetSiteContents(dispinfoSelectGroup.Name1, dispinfoSelectGroup.Name2);
                    ColViewMovieContents.IsFilterGroupCheck = true;

                    ColViewMovieContents.Execute();


                    bool isExist = false;
                    foreach(string dir in arrDir)
                    {
                        isExist = false;
                        DirectoryInfo dirinfo = new DirectoryInfo(dir);
                        foreach (MovieContents contents in ColViewMovieContents.ColViewListMovieContents)
                        {
                            if (contents.Name.Equals(dirinfo.Name))
                            {
                                isExist = true;
                                break;
                            }
                        }

                        if (!isExist)
                        {
                            MovieContents contents = new MovieContents();
                            contents.Name = dirinfo.Name;
                            contents.Kind = MovieContents.KIND_SITECHK_UNREGISTERED;
                            contents.Label = new DirectoryInfo(dispinfoSelectGroup.Path).Name; // Filterにかからなくなるので格納
                            contents.ParentPath = new DirectoryInfo(dispinfoSelectGroup.Path).Name;

                            SiteDetail s = new SiteDetail(Path.Combine(dispinfoSelectGroup.Path, dirinfo.Name));
                            contents.MovieCount = Convert.ToString(s.MovieCount);
                            contents.PhotoCount = Convert.ToString(s.ImageCount);
                            contents.Extension = s.Extention;
                            contents.MovieNewDate = s.MovieNewDate;

                            ColViewMovieContents.AddContents(contents);
                        }
                    }

                    foreach (MovieContents contents in ColViewMovieContents.ColViewListMovieContents)
                    {
                        string contentsFilename = Path.Combine(dispinfoSelectGroup.Path, contents.Name);

                        if (!Directory.Exists(contentsFilename))
                            contents.Kind = MovieContents.KIND_SITECHK_NOTEXIST;
                    }

                    string sortColumns = Convert.ToString(cmbContentsSort.SelectedValue);
                    ColViewMovieContents.SetSort("Kind", ListSortDirection.Descending);

                    ColViewMovieContents.Execute();
                }
            }
        }

        private void btnAddGroupExecute_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAddGroupKind.SelectedValue == null)
            {
                MessageBox.Show("KINDが指定されていません");
                return;
            }

            string typeName = Convert.ToString(cmbAddGroupKind.SelectedValue);

            if (typeName == "file")
            {
                DirectoryInfo dir = new DirectoryInfo(txtAddGroupPath.Text);

                if (!dir.Exists)
                {
                    MessageBox.Show("Pathに指定されているフォルダが存在しません");
                    return;
                }
            }

            if (txtbAddGroupMode.Text == "Register")
            {
                MovieGroupData registerData = new MovieGroupData();
                registerData.Label = txtAddGroupLabel.Text;
                registerData.Name1 = txtAddGroupName1.Text;
                registerData.Name2 = txtAddGroupName2.Text;
                registerData.Path = txtAddGroupPath.Text;
                registerData.Type = typeName;

                ColViewStore.Add(registerData);
                ColViewStore.Refresh();
            }
            else
            {
                dispinfoSelectGroup.Label = txtAddGroupLabel.Text;
                dispinfoSelectGroup.Name1 = txtAddGroupName1.Text;
                dispinfoSelectGroup.Name2 = txtAddGroupName2.Text;
                dispinfoSelectGroup.Path = txtAddGroupPath.Text;

                ColViewStore.Update(dispinfoSelectGroup);
            }

            txtAddGroupLabel.Text = "";
            txtAddGroupName1.Text = "";
            txtAddGroupName2.Text = "";
            txtAddGroupPath.Text = "";
            cmbAddGroupKind.SelectedValue = null;
            txtAddGroupId.Text = "";
            txtbAddGroupMode.Text = "";

            dispinfoIsGroupAddVisible = false;
            LayoutChange();
        }

        private void OnButtonFavRegisterClick(object sender, RoutedEventArgs e)
        {
            if (cmbAddFavType.SelectedValue == null)
            {
                MessageBox.Show("Type[actress,keyword]が指定されていません");
                return;
            }

            if (txtbAddFavMode.Text == "Register")
            {
                FavData registerData = new FavData();
                registerData.Label = txtAddFavLabel.Text;
                registerData.Name = txtAddFavName.Text;
                registerData.Comment = txtAddFavComment.Text;
                registerData.Type = Convert.ToString(cmbAddFavType.SelectedValue);

                ColViewFav.Add(registerData);
                ColViewFav.Refresh();
            }
            else
            {
                dispinfoSelectFavData.Label = txtAddFavLabel.Text;
                dispinfoSelectFavData.Name = txtAddFavName.Text;
                dispinfoSelectFavData.Comment = txtAddFavComment.Text;
                dispinfoSelectFavData.Type = Convert.ToString(cmbAddFavType.SelectedValue);

                ColViewFav.Update(dispinfoSelectFavData);
            }

            txtAddFavLabel.Text = "";
            txtAddFavName.Text = "";
            txtAddFavComment.Text = "";
            cmbAddFavType.SelectedValue = null;
            txtAddFavId.Text = "";

            dispinfoIsGroupAddVisible = false;
            dispinfoIsGroupFavAddVisible = false;
            LayoutChange();
        }

        private void txtAddGroupId_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (dispinfoSelectGroup == null || textbox.Text.Length <= 0)
            {
                txtbAddGroupMode.Text = "Register";
            }
            else
            {
                try
                {
                    int id = Convert.ToInt32(txtAddGroupId.Text);
                    if (dispinfoSelectGroup.Id == id)
                        txtbAddGroupMode.Text = "Edit";
                    else
                        txtbAddGroupMode.Text = "Register";
                }
                catch (Exception)
                {
                    txtbAddGroupMode.Text = "Register";
                }
            }
        }

        private void OnTextFavIdTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (dispinfoSelectFavData == null || textbox.Text.Length <= 0)
            {
                txtbAddFavMode.Text = "Register";
            }
            else
            {
                try
                {
                    int id = Convert.ToInt32(textbox.Text);
                    if (dispinfoSelectGroup.Id == id)
                        txtbAddFavMode.Text = "Edit";
                    else
                        txtbAddFavMode.Text = "Register";
                }
                catch (Exception)
                {
                    txtbAddFavMode.Text = "Register";
                }
            }
        }

        private void btnAddGroupCheck_Click(object sender, RoutedEventArgs e)
        {
            string typeName = Convert.ToString(cmbAddGroupKind.SelectedValue);

            if (typeName == "file")
            {
                DirectoryInfo dir = new DirectoryInfo(txtAddGroupPath.Text);

                if (!dir.Exists)
                {
                    MessageBox.Show("Pathに指定されているフォルダが存在しません");
                    return;
                }
            }

            MovieGroupData filterGroup = new MovieGroupData();
            filterGroup.Label = txtAddGroupLabel.Text;
            filterGroup.Name1 = txtAddGroupName1.Text;
            filterGroup.Name2 = txtAddGroupName2.Text;
            filterGroup.Path = txtAddGroupPath.Text;
            filterGroup.Type = typeName;

            // 登録・更新で入力されているグループで表示
            ColViewMovieContents.ClearAndExecute(filterGroup);
        }

        private void menuitemAddTagContents_Click(object sender, RoutedEventArgs e)
        {
            lgridTagAdd.Visibility = Visibility.Visible;

            txtbTagOriginal.Text = dispinfoSelectContents.Tag;
            txtTag.Text = dispinfoSelectContents.Tag;

            // Autoの設定にする
            ScreenDisableBorderTag.Width = Double.NaN;
            ScreenDisableBorderTag.Height = Double.NaN;

            txtTag.Focus();
        }

        private void menuitemAddSelectedDataAdd_Click(object sender, RoutedEventArgs e)
        {
            var SelectedContents = dgridMovieContents.SelectedItems;

            List<MovieContents> MovieContentsList = new List<MovieContents>();

            foreach(MovieContents data in SelectedContents)
            {
                if (data.Kind == MovieContents.KIND_SITECHK_UNREGISTERED)
                {
                    data.DbExportSiteContents(dbcon);
                    data.Kind = MovieContents.KIND_SITE;
                }
                //MovieContentsList.Add(data);
            }
        }

        private void btnTagUpdate_Click(object sender, RoutedEventArgs e)
        {
            dispinfoSelectContents.DbUpdateTag(txtTag.Text, dbcon);

            ScreenDisableBorderTag.Width = 0;
            ScreenDisableBorderTag.Height = 0;

            lgridTagAdd.Visibility = Visibility.Hidden;
        }

        private void btnTagCancel_Click(object sender, RoutedEventArgs e)
        {
            lgridTagAdd.Visibility = Visibility.Hidden;
        }

        private void OnSiteDetail_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (textbox == null)
                return;

            if (!String.IsNullOrEmpty(txtSiteDetailContentsName.Text))
            {
                if (dispinfoSelectContents.Name != txtSiteDetailContentsName.Text)
                {
                    string pathname = Path.Combine(dispinfoSelectContents.Path, txtSiteDetailContentsName.Text);
                    if (Directory.Exists(pathname))
                        txtSiteDetailContentsName.Background = new LinearGradientBrush(Colors.Cyan, Colors.Cyan, 45);
                    else
                        txtSiteDetailContentsName.Background = new LinearGradientBrush(Colors.Red, Colors.Red, 45);
                }
                else
                    txtSiteDetailContentsName.Background = null;
            }
            else
                txtFileDetailContentsName.Background = null;

            if (textbox.Name == "txtSiteDetailContentsTag" && !String.IsNullOrEmpty(txtSiteDetailContentsTag.Text))
            {
                if (dispinfoSelectContents.Tag != txtSiteDetailContentsTag.Text)
                    txtSiteDetailContentsTag.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
                else
                    txtSiteDetailContentsTag.Background = null;
            }
            else if (textbox.Name == "txtSiteDetailContentsTag")
                txtSiteDetailContentsTag.Background = null;

            if (!String.IsNullOrEmpty(txtSiteDetailContentsStoreLabel.Text))
            {
                if (dispinfoSelectContents.StoreLabel != txtSiteDetailContentsStoreLabel.Text)
                {
                    MovieGroupData matchStoreLabel = ColViewStore.GetMatchLabel(txtSiteDetailContentsStoreLabel.Text);

                    if (matchStoreLabel == null)
                        txtSiteDetailContentsStoreLabel.Background = new LinearGradientBrush(Colors.Red, Colors.Red, 45);
                    else
                        txtSiteDetailContentsStoreLabel.Background = new LinearGradientBrush(Colors.Cyan, Colors.Cyan, 45);
                }
                else
                    txtSiteDetailContentsStoreLabel.Background = null;
            }

            if (!String.IsNullOrEmpty(txtSiteDetailContentsFileDate.Text))
            {
                if (dispinfoSelectContents.FileDate.ToString("yyyy/MM/dd HH:mm:ss") != txtSiteDetailContentsFileDate.Text)
                    txtSiteDetailContentsFileDate.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
                else
                    txtSiteDetailContentsFileDate.Background = null;
            }

            /*
            MovieContents data = dispinfoSelectContents;
            data.Name = textbox.Text;
            string path = data.Path;

            if (path == null)
            {
                textbox.Background = new LinearGradientBrush(Colors.Red, Colors.Red, 45);
                return;
            }

            //dispinfoSelectContents.Name = textbox.Text;
            //dgridMovieContents_SelectionChanged(null, null);
            textbox.Background = new LinearGradientBrush(Colors.Cyan, Colors.Cyan, 45);
             */
        }

        private void btnSiteDetailUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool isChangeFile = false, isChangedDir = false;
            bool isSiteDetail = false;
            string msg;

            MovieContents dataTarget = null;

            string sourcePathname = "", destPathname = "";
            if (lgridFileDetail.Visibility == Visibility.Visible)
            {
                if (dispinfoSelectContents.Name != txtFileDetailContentsName.Text)
                {
                    isChangeFile = true;
                    msg = "DBとファイルを更新します";
                }
                else
                    msg = "DBを更新します（ファイル変更は無し）";

                dataTarget = GetMovieContentsFromTextbox(txtFileDetailContentsName
                                                            , txtFileDetailContentsTag
                                                            , txtFileDetailContentsLabel
                                                            , txtFileDetailContentsSellDate
                                                            , txtFileDetailContentsProductNumber
                                                            , txtFileDetailContentsExtension
                                                            , txtFileDetailContentsFileDate
                                                            , txtFileDetailContentsFileCount);
            }
            else
            {
                isSiteDetail = true;
                sourcePathname = Path.Combine(dispinfoSelectContents.Path, dispinfoSelectContents.Name);
                destPathname = Path.Combine(dispinfoSelectContents.Path, txtSiteDetailContentsName.Text);

                if (Directory.Exists(destPathname))
                    isChangedDir = true;

                if (isChangedDir)
                    msg = "DBを更新します（フォルダ変更済み）";
                else if (Directory.Exists(sourcePathname))
                    msg = "DBとフォルダを更新します";
                else
                    msg = "DBを更新します\nソースフォルダ無し：「" + sourcePathname + "」";

                dataTarget = GetMovieContentsFromTextbox(txtSiteDetailContentsName
                                , txtSiteDetailContentsTag
                                , txtSiteDetailContentsStoreLabel
                                , null
                                , null
                                , null
                                , txtSiteDetailContentsFileDate
                                , null);
            }

            MessageBoxResult result = MessageBox.Show(msg, "更新確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            if (isChangeFile)
            {
                List<common.FileContents> files = (List<common.FileContents>)dgridFileDetail.ItemsSource;

                foreach(common.FileContents data in files)
                {
                    string destFilename = Path.Combine(dispinfoSelectContents.Path, data.FileInfo.Name.Replace(dispinfoSelectContents.Name, txtFileDetailContentsName.Text));
                    File.Move(data.FileInfo.FullName, destFilename);
                }

            }
            if (dataTarget != null)
            {
                dispinfoSelectContents.RefrectData(dataTarget);

                dispinfoSelectContents.DbUpdate(dbcon);
            }

            if (isSiteDetail)
            {
                if (isChangedDir == false && Directory.Exists(sourcePathname))
                    Directory.Move(sourcePathname, destPathname);

                OnSiteDetail_TextChanged(null, null);
            }
            else
                OnFileDetail_TextChanged(null, null);
        }
        private MovieContents GetMovieContentsFromTextbox(
            TextBox myTextBoxName
            , TextBox myTextBoxTag
            , TextBox myTextBoxLabel
            , TextBox myTextBoxSellDate
            , TextBox myTextBoxProductNumber
            , TextBox myTextBoxExtension
            , TextBox myTextBoxFileDate
            , TextBox myTextBoxFileCount)
        {
            MovieContents data = new MovieContents();

            if (myTextBoxName != null && myTextBoxName.Text.Trim().Length > 0)
                data.Name = myTextBoxName.Text;
            if (myTextBoxTag != null && myTextBoxTag.Text.Trim().Length > 0)
                data.Tag = myTextBoxTag.Text;
            if (myTextBoxLabel != null && myTextBoxLabel.Text.Trim().Length > 0)
                data.StoreLabel = myTextBoxLabel.Text;
            if (myTextBoxSellDate != null && myTextBoxSellDate.Text.Trim().Length > 0)
                data.SellDate = Convert.ToDateTime(myTextBoxSellDate.Text);
            if (myTextBoxProductNumber != null && myTextBoxProductNumber.Text.Trim().Length > 0)
                data.ProductNumber = myTextBoxProductNumber.Text;
            if (myTextBoxExtension != null && myTextBoxExtension.Text.Trim().Length > 0)
                data.Extension = myTextBoxExtension.Text;
            if (myTextBoxFileDate != null && myTextBoxFileDate.Text.Trim().Length > 0)
                data.FileDate = Convert.ToDateTime(myTextBoxFileDate.Text);
            if (myTextBoxFileCount != null && myTextBoxFileCount.Text.Trim().Length > 0)
                data.FileCount = Convert.ToInt32(myTextBoxFileCount.Text);

            return data;
        }

        private void OnFileDetail_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnFileDetailUpdate.IsEnabled = true;
            btnFileDetailUpdate.Background = null;
            if (dispinfoSelectContents.Name != txtFileDetailContentsName.Text)
                txtFileDetailContentsName.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsName.Background = null;

            if (dispinfoSelectContents.Tag != txtFileDetailContentsTag.Text)
                txtFileDetailContentsTag.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsTag.Background = null;

            MovieGroupData storeData = ColViewStore.GetMatchLabel(dispinfoSelectContents.StoreLabel);
            if (storeData != null)
            {
                if (dispinfoSelectContents.StoreLabel != txtFileDetailContentsLabel.Text)
                    txtFileDetailContentsLabel.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
                else
                    txtFileDetailContentsLabel.Background = null;
            }
            else
            {
                txtFileDetailContentsLabel.Background = new LinearGradientBrush(Colors.PaleVioletRed, Colors.PaleVioletRed, 0.5);
                btnFileDetailUpdate.IsEnabled = false;
                btnFileDetailUpdate.Background = new LinearGradientBrush(Colors.LightGray, Colors.LightGray, 0.5);
            }

            if (txtFileDetailContentsSellDate.Text.Length > 0)
            {
                DateTime dt = new DateTime(1901,1,1);
                try
                {
                    dt = Convert.ToDateTime(txtFileDetailContentsSellDate.Text);
                }
                catch(FormatException)
                {
                    txtFileDetailContentsSellDate.Background = new LinearGradientBrush(Colors.PaleVioletRed, Colors.PaleVioletRed, 0.5);
                    btnFileDetailUpdate.IsEnabled = false;
                }
                if (dt.Year != 1901)
                {
                    if (dispinfoSelectContents.SellDate.CompareTo(dt) != 0)
                        txtFileDetailContentsSellDate.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
                    else
                        txtFileDetailContentsSellDate.Background = null;
                }
            }
            else
            {
                if (dispinfoSelectContents.SellDate != null)
                    txtFileDetailContentsSellDate.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            }

            if (dispinfoSelectContents.ProductNumber != txtFileDetailContentsProductNumber.Text)
                txtFileDetailContentsProductNumber.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsProductNumber.Background = null;

            if (dispinfoSelectContents.Extension != txtFileDetailContentsExtension.Text)
                txtFileDetailContentsExtension.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsExtension.Background = null;

            if (dispinfoSelectContents.FileDate.ToString("yyyy/MM/dd HH:mm:ss") != txtFileDetailContentsFileDate.Text)
                txtFileDetailContentsFileDate.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsFileDate.Background = null;

            if (dispinfoSelectContents.FileCount.ToString() != txtFileDetailContentsFileCount.Text)
                txtFileDetailContentsFileCount.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsFileCount.Background = null;

            txtFileDetailContentsCreateDate.Background = null;
            txtFileDetailContentsUpdateDate.Background = null;
        }

        private void lgridImageContents_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid lgrid = sender as Grid;

            // イベントの処理済みフラグ（設定しないと2回イベントが発生する）
            e.Handled = true;

            if (e.ClickCount == 2)
            {
                btnSwitchContents_Click(null, null);
                return;
            }

            if (lgrid.Name == "lgridImageContents")
            {
                // http://stackoverflow.com/questions/6363312/get-grid-cell-by-mouse-click
                if (e.ClickCount == 1) // for double-click, remove this condition if only want single click
                {
                    double widthMiddlePosi = lgridImageContents.ActualWidth / 2;
                    var point = Mouse.GetPosition(lgridImageContents);

                    Debug.Print("lgridImageContents.ActualWidth [" + lgridImageContents.ActualWidth + "]   point.X [" + point.X + "]  point.Y [" + point.Y + "]");

                    if (widthMiddlePosi >= point.X)
                        dispinfoSelectContents.BackImage();
                    else
                        dispinfoSelectContents.NextImage();

                    OnDisplayImage(dispinfoSelectContents, dispinfoSelectGroup);
                }
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                dispinfoIsContentsVisible = true;
                dispinfoContentsVisibleKind = CONTENTS_VISIBLE_KIND_IMAGE;

                dispinfoIsGroupVisible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                dispinfoIsContentsVisible = false;
                dispinfoIsGroupVisible = true;
            }
        }

        private void cmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBox combobox = sender as ComboBox;
            string selValue = "";
            if (combobox != null)
                selValue = combobox.SelectedValue.ToString();

            Style style = new Style();
            DataGridRow row = new DataGridRow();
            style.TargetType = row.GetType();

            Binding bgbinding = null;
            if (selValue == "NotRated")
            {
                RatingRowColorConverter rowColorConverter = new RatingRowColorConverter();
                bgbinding = new Binding("Rating") { Converter = rowColorConverter };
                //style.Setters.Add(new Setter(ListBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            }
            else
            {
                RowColorConverter rowColorConverter = new RowColorConverter();
                bgbinding = new Binding("Kind") { Converter = rowColorConverter };
            }

            style.Setters.Add(new Setter(DataGridRow.BackgroundProperty, bgbinding));

            dgridMovieContents.ItemContainerStyle = style;
        }

        private void menuitemDeleteMovieFiles_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectContents == null)
                return;

            dispinfoSelectContents.DbDelete(dbcon);
            ColViewMovieContents.Delete(dispinfoSelectContents);

            dgridMovieGroup_SelectionChanged(null, null);
        }

        private void btnFileDetailPasteFile_Click(object sender, RoutedEventArgs e)
        {
            txtFileDetailPasteFilename.Text = common.ClipBoard.GetTextPath();

            lgridProgressBar.Visibility = Visibility.Collapsed;
            txtFileDetailPasteFilename.Visibility = Visibility.Visible;
            txtFileDetailPasteFilename.Background = null;
        }

        private void OnRefreshFileDetailInfo(object sender, RoutedEventArgs e)
        {
            // ファイル情報を再取得
            ColViewFileDetail.Refresh();

            // ファイル情報は反映、DB更新
            //dispinfoSelectContents.RefrectFileInfoAndDbUpdate(ColViewFileDetail, dbcon);

            // ファイル情報の各Controlへの表示を更新
            txtbFileDetailId.Text = Convert.ToString(dispinfoSelectContents.Id);
            txtFileDetailContentsName.Text = dispinfoSelectContents.Name;
            txtFileDetailContentsTag.Text = dispinfoSelectContents.Tag;
            txtFileDetailContentsLabel.Text = dispinfoSelectContents.StoreLabel;
            txtFileDetailContentsSellDate.Text = dispinfoSelectContents.SellDate.ToString("yyyy/MM/dd");
            txtFileDetailContentsProductNumber.Text = dispinfoSelectContents.ProductNumber;
            txtFileDetailContentsExtension.Text = dispinfoSelectContents.Extension;
            txtFileDetailContentsFileDate.Text = dispinfoSelectContents.FileDate.ToString("yyyy/MM/dd HH:mm:ss");
            txtFileDetailContentsFileCount.Text = Convert.ToString(dispinfoSelectContents.FileCount);
            txtFileDetailContentsCreateDate.Text = dispinfoSelectContents.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss");
            txtFileDetailContentsUpdateDate.Text = dispinfoSelectContents.UpdatedAt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        private void btnFileDetailDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgridFileDetail.SelectedItems == null || dgridFileDetail.SelectedItems.Count <= 0)
                return;

            var selFiles = dgridFileDetail.SelectedItems;
            if (selFiles.Count > 1)
            {
                MessageBox.Show("選択可能ファイルは1つだけです", "警告");
                return;
            }

            common.FileContents selFile = (common.FileContents)dgridFileDetail.SelectedItem;

            detail.FileCopyDetail fileCopy = new detail.FileCopyDetail(ColViewFileDetail, dispinfoSelectContents);
            fileCopy.DeleteExecute(selFile);

            OnRefreshFileDetailInfo(null, null);
        }

        private void btnFileDetailAdd_Click(object sender, RoutedEventArgs e)
        {
            if (txtFileDetailPasteFilename.Text.Length <= 0)
                return;

            MessageBoxResult result;

            detail.FileCopyDetail fileCopy = new detail.FileCopyDetail(ColViewFileDetail, dispinfoSelectContents);
            if (dgridFileDetail.SelectedItems == null || dgridFileDetail.SelectedItems.Count <= 0)
                fileCopy.SetAdd(txtFileDetailPasteFilename.Text);
            else
            {
                var selFiles = dgridFileDetail.SelectedItems;
                if (selFiles.Count > 1)
                {
                    MessageBox.Show("選択可能ファイルは1つだけです", "警告");
                    return;
                }

                common.FileContents selFile = (common.FileContents)dgridFileDetail.SelectedItem;
                Regex regexMov = new Regex(MovieContents.REGEX_MOVIE_EXTENTION, RegexOptions.IgnoreCase);

                if (!regexMov.IsMatch(selFile.FileInfo.Name))
                {
                    MessageBox.Show("動画のみが選択可能です", "警告");
                    return;
                }

                fileCopy.SetReplace(selFile, txtFileDetailPasteFilename.Text);
            }

            string message = "";
            if (fileCopy.IsOverride)
                message = "拡張子が同じファイルが存在するので上書きします";
            else
            {
                if (fileCopy.Status == detail.FileCopyDetail.STATUS_ADD)
                    message = "ファイルを追加します";
                else
                    message = "拡張子が" + dispinfoSelectContents.Extension + "のファイルは削除してコピーします";
            }

            result = MessageBox.Show(message, "確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            if (bgworkerFileDetailCopy == null)
            {
                bgworkerFileDetailCopy = new BackgroundWorker();
                bgworkerFileDetailCopy.WorkerSupportsCancellation = true;
                bgworkerFileDetailCopy.WorkerReportsProgress = true;
            }

            bgworkerFileDetailCopy.DoWork += new DoWorkEventHandler(bgworkerFileDetailCopy_DoWork);
            bgworkerFileDetailCopy.ProgressChanged += new ProgressChangedEventHandler(bgworkerFileDetailCopy_ProgressChanged);
            bgworkerFileDetailCopy.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgworkerFileDetailCopy_RunWorkerCompleted);

            lgridProgressBar.Visibility = Visibility.Visible;
            txtFileDetailPasteFilename.Visibility = Visibility.Collapsed;

            if (bgworkerFileDetailCopy.IsBusy != true)
            {
                var param = Tuple.Create(fileCopy);
                stopwatchFileDetailCopy.Start();
                bgworkerFileDetailCopy.RunWorkerAsync(param);
            }
        }

        private void bgworkerFileDetailCopy_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            var value = e.Argument as Tuple<detail.FileCopyDetail>;

            detail.FileCopyDetail fileCopyDetail = value.Item1;
            fileCopyDetail.Execute(worker, e);
        }

        private void bgworkerFileDetailCopy_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.txtbFileDetailProgressStatus.Text = (e.ProgressPercentage.ToString() + "%");
        }

        private void bgworkerFileDetailCopy_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
                this.txtbFileDetailProgressStatus.Text = "Canceled!";
            else if (!(e.Error == null))
                this.txtbFileDetailProgressStatus.Text = ("Error: " + e.Error.Message);
            else
            {
                stopwatchFileDetailCopy.Stop();
                TimeSpan timespan = stopwatchFileDetailCopy.Elapsed;
                if (timespan.Minutes > 0)
                    this.txtbFileDetailProgressStatus.Text = "正常終了 " + timespan.Minutes + "分" + timespan.Seconds + "秒";
                else
                    this.txtbFileDetailProgressStatus.Text = "正常終了 " + timespan.Seconds + "秒";

                OnRefreshFileDetailInfo(null, null);

                txtFileDetailPasteFilename.Text = "";
                bgworkerFileDetailCopy = null;
            }
        }

        private void OnFilterToggleButtonClick(object sender, RoutedEventArgs e)
        {
            ColViewMovieContents.IsFilterAv = GetToggleChecked(tbtnFilterAv);
            ColViewMovieContents.IsFilterIv = GetToggleChecked(tbtnFilterIv);
            ColViewMovieContents.IsFilterUra = GetToggleChecked(tbtnFilterUra);
            ColViewMovieContents.IsFilterComment = GetToggleChecked(tbtnFilterComment);
            ColViewMovieContents.IsFilterTag = GetToggleChecked(tbtnFilterTag);

            ColViewMovieContents.Execute();
        }

        private bool GetToggleChecked(ToggleButton myToggleButton)
        {
            if (myToggleButton == null)
                return false;

            return (bool)myToggleButton.IsChecked;
        }

        private void btnContentsOpen_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoIsContentsVisible)
                dispinfoIsContentsVisible = false;
            else
                dispinfoIsContentsVisible = true;

            dispinfoContentsVisibleKind = CONTENTS_VISIBLE_KIND_IMAGE;

            LayoutChange();

            dgridMovieContents_SelectionChanged(null, null);
        }

        private void OnCloseImageContents(object sender, RoutedEventArgs e)
        {
            dispinfoIsContentsVisible = false;

            LayoutChange();
        }

        private void OnSiteDetailSelectedListButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button.Content.Equals("Add"))
            {
                var files = dgridSiteDetail.SelectedItems;

                if (files == null)
                    return;

                List<FileContents> listFileContents = new List<FileContents>();
                foreach(FileContents fileContents in files)
                {
                    listFileContents.Add(fileContents);
                }
                targetList.Add(listFileContents);
            }
            if (button.Content.Equals("Delete"))
            {
                var files = lstSiteDetailSelectedList.SelectedItems;

                if (files == null)
                    return;

                List<string> listFileContents = new List<string>();
                foreach (string selectedFile in files)
                {
                    targetList.Delete(selectedFile);
                }
            }
            if (button.Content.Equals("↑"))
            {
                var files = lstSiteDetailSelectedList.SelectedItems;

                if (files == null)
                    return;

                List<string> listFileContents = new List<string>();
                foreach (string selectedFile in files)
                {
                    listFileContents.Add(selectedFile);
                }
                targetList.Up(listFileContents);
            }
            if (button.Content.Equals("↓"))
            {
                var files = lstSiteDetailSelectedList.SelectedItems;

                if (files == null)
                    return;

                List<string> listFileContents = new List<string>();
                foreach (string selectedFile in files)
                {
                    listFileContents.Add(selectedFile);
                }
                targetList.Down(listFileContents);
            }

            lstSiteDetailSelectedList.Items.Refresh();
        }

        private void btnMatchContents_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectContents == null)
                return;

            string resultEvaluation = "";
            string evaluation = "";
            int maxFav = 0;
            string maxActress = "";
            bool isFav = false;

            if (dispinfoSelectContents.Tag != null && dispinfoSelectContents.Tag.Length > 0)
            {
                string[] arrActresses = Actress.ParseTag(dispinfoSelectContents.Tag);
                foreach(string actress in arrActresses)
                {
                    evaluation = "";
                    string[] arrData = ColViewFav.GetMatch(actress);

                    bool isBool = ColViewFav.isOnlyMatch(actress);
                    if (isBool)
                        isFav = isBool;

                    List<MovieContents> matchData = ColViewMovieContents.GetMatchData(arrData);
                    List<MovieContents> likeData = new List<MovieContents>();

                    foreach (MovieContents data in ColViewMovieContents.GetLikeFilenameData(arrData))
                    {
                        if (!matchData.Exists(x => x.Id == data.Id))
                            likeData.Add(data);
                    }

                    int sumEvaluate = 0, unEvaluate = 0, maxEvaluate = 0;

                    if (matchData.Count > 0)
                    {
                        sumEvaluate = matchData.Sum(x => x.Rating);
                        unEvaluate = matchData.Where(x => x.Rating == 0).Count();
                        maxEvaluate = matchData.Max(x => x.Rating);
                    }

                    if (arrActresses.Length > 1)
                    {
                        if (maxFav < maxEvaluate)
                        {
                            maxFav = maxEvaluate;
                            maxActress = actress;
                        }
                    }

                    if (sumEvaluate <= 0 || matchData.Count - unEvaluate <= 0)
                        evaluation = String.Format("全未評価 {0} ({1})", matchData.Count, likeData.Count);
                    else
                        evaluation = String.Format("未 {0}/全 {1} Max {2} Avg {3} ({4})", unEvaluate, matchData.Count, maxEvaluate, sumEvaluate / (matchData.Count - unEvaluate), likeData.Count);

                    resultEvaluation = String.Format("{0} {1} {2}", resultEvaluation, actress, evaluation);
                }

                if (arrActresses.Length > 1)
                    resultEvaluation = String.Format("【{0} Max{1}】{2}", maxActress, maxFav, resultEvaluation);

                if (isFav)
                    resultEvaluation = "Fav " + resultEvaluation;

                if (arrActresses.Length == 1)
                    txtStatusBarFileDate.Text = resultEvaluation.Trim();
                else
                    txtStatusBar.Text = txtStatusBar.Text + " " + resultEvaluation.Trim();
            }
        }

        private void mediaSitesImageGifOne_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaSitesImageGifOne.Position = new TimeSpan(0, 0, 1);
            mediaSitesImageGifOne.Play();
        }
    }
}
