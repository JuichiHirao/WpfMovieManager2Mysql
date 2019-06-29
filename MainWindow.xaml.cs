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
using wpfMovieManager2Mysql.common;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Text;

namespace wpfMovieManager2Mysql
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

        DbConnection dbcon;

        common.Image image = null;

        MovieContentsFilterAndSort ColViewMovieContents;
        MovieGroupFilterAndSorts ColViewMovieGroup;
        SiteDetail ColViewSiteDetail;
        detail.FileDetail ColViewFileDetail;

        // 画面情報
        string dispinfoGroupButton = "";
        bool dispinfoIsGroupVisible = false; // グループの表示を有効にする場合はtrue
        bool dispinfoIsGroupAddVisible = false; // グループ追加の表示を有効にする場合はtrue
        bool dispinfoIsContentsVisible = false;
        int dispinfoContentsVisibleKind = 0;
        contents.TargetList targetList = null;


        double dispctrlContentsWidth = 800;

        MovieContents dispinfoSelectContents = null;
        MovieGroup dispinfoTargetGroupBySelectContents = null;
        MovieGroup dispinfoSelectGroup = null;

        BackgroundWorker bgworkerFileDetailCopy;
        Stopwatch stopwatchFileDetailCopy = new Stopwatch();

        public MainWindow()
        {

            InitializeComponent();

            dbcon = new DbConnection();
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
                ColViewMovieGroup = new MovieGroupFilterAndSorts(dbcon);

                dgridMovieContents.ItemsSource = ColViewMovieContents.ColViewListMovieContents;
                dgridMovieGroup.ItemsSource = ColViewMovieGroup.ColViewListMovieGroup;
                dgridMovieGroup.Visibility = Visibility.Collapsed;

                cmbSiteName.ItemsSource = ColViewMovieGroup.listSiteName;
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

                dgridMovieGroup_SelectionChanged(null, null);

                if (dispinfoGroupButton == "S")
                    cmbSiteName.Visibility = Visibility.Visible;
                else
                    cmbSiteName.Visibility = Visibility.Collapsed;

                if (!dispinfoIsGroupAddVisible)
                    lgridMovieGroup.RowDefinitions[1].Height = new GridLength(0);
                else
                    lgridMovieGroup.RowDefinitions[1].Height = new GridLength(300);
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

                ColViewMovieGroup.SetSort(Convert.ToString(cmbSortGroup.SelectedValue), sortOrder);
                ColViewMovieGroup.Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NORMAL);
            }
            else
            {
                sortOrder = GetSortOrder(btnSortOrder, isReverse);
                sortColumns = Convert.ToString(cmbContentsSort.SelectedValue);

                ColViewMovieContents.SetSort(Convert.ToString(cmbContentsSort.SelectedValue), sortOrder);
                ColViewMovieGroup.Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NORMAL);
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
            int kind = MovieGroups.KindByButton[dispinfoGroupButton];
            dispinfoIsGroupVisible = true;

            ColViewMovieContents.Clear();
            string sortColumns = Convert.ToString(cmbContentsSort.SelectedValue);
            ColViewMovieContents.SetSort(sortColumns, GetSortOrder(btnSortOrder, false));

            ColViewMovieGroup.SetSort("UpdateDate", ListSortDirection.Descending);
            ColViewMovieGroup.SetFilterKind(kind);
            if (dispinfoGroupButton != "S")
                ColViewMovieGroup.SetSiteName("");
            ColViewMovieGroup.Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NORMAL);

            LayoutChange();
        }

        private void dgridMovieGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSelectGroup = (MovieGroup)dgridMovieGroup.SelectedItem;
            if (dispinfoSelectGroup == null)
                return;

            // 画像表示はクリア
            OnDisplayImage(null, dispinfoTargetGroupBySelectContents);

            // 選択されているグループで表示
            GroupFilesInfo filesInfo = ColViewMovieContents.ClearAndExecute(ColViewMovieGroup.FilterKind, dispinfoSelectGroup);

            this.Title = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
            txtbGroupInfo.Text = "未評価 [" + filesInfo.Unrated + "/" + filesInfo.FileCount + "]  Size [" + CommonMethod.GetDisplaySize(filesInfo.Size) + "]";
        }

        private void txtSearchGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            ColViewMovieGroup.SetSearchText(txtSearchGroup.Text);
            ColViewMovieGroup.Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NORMAL);
        }

        private void OnCloseGroupFilter_Click(object sender, RoutedEventArgs e)
        {
            if (dispinfoIsGroupAddVisible)
                dispinfoIsGroupAddVisible  = false;
            else
                dispinfoIsGroupVisible = false;

            LayoutChange();
        }

        private void cmbSiteName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectSiteName = Convert.ToString(cmbSiteName.SelectedItem);

            ColViewMovieGroup.SetSiteName(selectSiteName);
            ColViewMovieGroup.Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NATURAL_COMPARE);

            ColViewMovieContents.Clear();
            ColViewMovieContents.SetSiteContents(selectSiteName, "");
            ColViewMovieContents.Execute();
        }

        private void OnDisplayImage(MovieContents myMovieContents, MovieGroup myTargetGroup)
        {
            // パラメータがnullの場合は画像表示を全てクリア
            if (myMovieContents == null)
            {
                imagePackage.Source = null;
                return;
            }

            if (image == null) image = new common.Image(myMovieContents, myTargetGroup);

            // PackageImage
            // Visibleの場合のみ表示
            FileInfo fileInfoPackage = image.GetDefaultPackageFileInfo();

            if (fileInfoPackage != null && fileInfoPackage.Exists)
            {
                string path = myMovieContents.GetExistPath(myTargetGroup);
                if (path != null)
                {
                    txtStatusBar.Text = myMovieContents.ExistMovie[0];
                    txtStatusBarFileLength.Text = CommonMethod.GetDisplaySize(myMovieContents.Size);
                    imagePackage.Source = ImageMethod.GetImageStream(fileInfoPackage.FullName);
                }
            }
            else
            {
                imagePackage.Source = null;
            }

            // ImageContents
            // Visibleの場合のみ表示
            //   Kind == 1 ( File )
            //     isThumbnail
            //       true  サムネイル画像を表示
            //       false Dirがあれば、Dir内の画像、無い場合はパッケージを表示、ImagePackageを非表示
            //   Kind == 2 ( Site )
            if (image.IsThumbnail())
            {
                List<FileInfo> listFileInfo = image.listImageFileInfo;

                if (listFileInfo != null && listFileInfo.Count >= 1)
                {
                    imageSitesImageOne.Source = ImageMethod.GetImageStream(listFileInfo[0].FullName);
                    imageSitesImageOne.ToolTip = listFileInfo[0].Name;
                    imageSitesImageTwo.Visibility = Visibility.Collapsed;
                    imageSitesImageThree.Visibility = Visibility.Collapsed;
                    imageSitesImageFour.Visibility = Visibility.Collapsed;

                    imageSitesImageOne.SetValue(Grid.RowSpanProperty, 2);
                    imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 2);

                    BitmapImage bitmapImage = (BitmapImage)imageSitesImageOne.Source;
                    imageSitesImageOne.Width = lgridMain.ColumnDefinitions[MAIN_COLUMN_NO_CONTENTS].ActualWidth;
                    imageSitesImageOne.Height = (imageSitesImageOne.Width / bitmapImage.Width) * bitmapImage.Height;
                    imageSitesImageOne.Stretch = Stretch.Uniform;
                }
            }
            else
            {
                List<FileInfo> listFileInfo = image.listImageFileInfo;
                txtbImageInfo.Text = image.DisplayPage;

                txtStatusBar.Text = Path.Combine(myMovieContents.Label, myMovieContents.Name);
                txtStatusBarFileLength.Text = CommonMethod.GetDisplaySize(myMovieContents.Size);

                if (listFileInfo.Count > 1)
                {
                    imageSitesImageOne.SetValue(Grid.RowSpanProperty, 1);
                    imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 1);

                    imageSitesImageOne.Width = imageSitesImageTwo.Width;
                    imageSitesImageOne.Height = imageSitesImageTwo.Height;

                    if (listFileInfo.Count >= 1)
                    {
                        imageSitesImageOne.Source = ImageMethod.GetImageStream(listFileInfo[0].FullName);
                        imageSitesImageOne.ToolTip = listFileInfo[0].Name;
                        imageSitesImageOne.Visibility = Visibility.Visible;
                    }
                    if (listFileInfo.Count >= 2)
                    {
                        imageSitesImageTwo.Source = ImageMethod.GetImageStream(listFileInfo[1].FullName);
                        imageSitesImageTwo.ToolTip = listFileInfo[1].Name;
                        imageSitesImageTwo.Visibility = Visibility.Visible;
                    }
                    else
                        imageSitesImageTwo.Visibility = Visibility.Collapsed;

                    if (listFileInfo.Count >= 3)
                    {
                        imageSitesImageThree.Source = ImageMethod.GetImageStream(listFileInfo[2].FullName);
                        imageSitesImageThree.ToolTip = listFileInfo[2].Name;
                        imageSitesImageThree.Visibility = Visibility.Visible;
                    }
                    else
                        imageSitesImageThree.Visibility = Visibility.Collapsed;

                    if (listFileInfo.Count >= 4)
                    {
                        imageSitesImageFour.Source = ImageMethod.GetImageStream(listFileInfo[3].FullName);
                        imageSitesImageFour.ToolTip = listFileInfo[3].Name;
                        imageSitesImageFour.Visibility = Visibility.Visible;
                    }
                    else
                        imageSitesImageFour.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (listFileInfo.Count >= 1)
                    {
                        imageSitesImageOne.Source = ImageMethod.GetImageStream(listFileInfo[0].FullName);
                        imageSitesImageOne.ToolTip = listFileInfo[0].Name;
                        imageSitesImageOne.Visibility = Visibility.Visible;
                    }
                    else
                        imageSitesImageOne.Source = null;

                    imageSitesImageTwo.Visibility = Visibility.Collapsed;
                    imageSitesImageThree.Visibility = Visibility.Collapsed;
                    imageSitesImageFour.Visibility = Visibility.Collapsed;

                    imageSitesImageOne.SetValue(Grid.RowSpanProperty, 2);
                    imageSitesImageOne.SetValue(Grid.ColumnSpanProperty, 2);
                }
            }
        }
        private void dgridMovieContents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSelectContents = (MovieContents)dgridMovieContents.SelectedItem;
            image = null;

            if (dispinfoSelectContents != null)
            {
                txtLargeComment.Text = dispinfoSelectContents.Comment;
                cmbLargeRating.SelectedValue = dispinfoSelectContents.Rating;
            }
            else
                return;

            if (dispinfoSelectContents.Name.IndexOf("?") >= 0)
            {
                FileInfo fileInfo = null;
                string searchPattern = "*[" + dispinfoSelectContents.ProductNumber + " " + dispinfoSelectContents.SellDate.ToString("yyyyMMdd") + "].jpg";
                string[] arrFiles = Directory.GetFiles(dispinfoSelectContents.Label, searchPattern, System.IO.SearchOption.TopDirectoryOnly);
                if (arrFiles.Length >= 1)
                {
                    fileInfo = new FileInfo(arrFiles[0]);
                    dispinfoSelectContents.Name = fileInfo.Name.Replace(fileInfo.Extension, "");
                }
            }

            dispinfoTargetGroupBySelectContents = ColViewMovieGroup.GetMatchDataByContents(dispinfoSelectContents);

            if (dispinfoTargetGroupBySelectContents == null)
                txtStatusBar.Text = "" + dispinfoSelectContents.Label + "に一致するグループが存在しませんでした";

            OnDisplayImage(dispinfoSelectContents, dispinfoTargetGroupBySelectContents);

            if (dispinfoContentsVisibleKind == CONTENTS_VISIBLE_KIND_DETAIL)
            {
                if (dispinfoSelectContents == null)
                    return;

                if (dispinfoSelectContents.Kind == MovieContents.KIND_FILE
                    || dispinfoSelectContents.Kind == MovieContents.KIND_CONTENTS)
                {
                    lgridSiteDetail.Visibility = Visibility.Collapsed;
                    lgridFileDetail.Visibility = Visibility.Visible;

                    ColViewFileDetail = new detail.FileDetail(dispinfoSelectContents, dispinfoTargetGroupBySelectContents);
                    dgridFileDetail.ItemsSource = ColViewFileDetail.listFileInfo;

                    OnRefreshFileDetailInfo(null, null);

                    return;
                }
                lgridSiteDetail.Visibility = Visibility.Visible;
                lgridFileDetail.Visibility = Visibility.Collapsed;

                txtSiteDetailContentsName.Text = dispinfoSelectContents.Name;
                txtSiteDetailContentsTag.Text = dispinfoSelectContents.Tag;
                string path = dispinfoSelectContents.GetExistPath(dispinfoTargetGroupBySelectContents);
                txtSiteDetailContentsPath.Text = path;
                txtSiteDetailContentsComment.Text = dispinfoSelectContents.Comment;

                if (path != null)
                {
                    ScreenDisableBorderSiteDetail.Width = 0;
                    ScreenDisableBorderSiteDetail.Height = 0;
                    ScreenDisableBorderImageContents.Width = 0;
                    ScreenDisableBorderImageContents.Height = 0;

                    txtSiteDetailContentsPath.Text = path;
                    txtSiteDetailContentsComment.Text = dispinfoSelectContents.Comment;
                    ColViewSiteDetail = new SiteDetail(txtSiteDetailContentsPath.Text);

                    dgridSiteDetail.ItemsSource = ColViewSiteDetail.listFileInfo;
                    btnSiteDetailImage.Content = "Image (" + ColViewSiteDetail.ImageCount + ")";
                    btnSiteDetailMovie.Content = "Movie (" + ColViewSiteDetail.MovieCount + ")";
                    btnSiteDetailList.Content = "List (" + ColViewSiteDetail.ListCount + ")";

                    imageSiteDetail.Source = ImageMethod.GetImageStream(ColViewSiteDetail.StartImagePathname);

                    targetList = new contents.TargetList(path);
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
                Player.Execute(dispinfoSelectContents, "GOM", dispinfoTargetGroupBySelectContents);
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

                    Player.Execute(dispinfoSelectContents, player.Name, dispinfoTargetGroupBySelectContents);
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

            if (dispinfoSelectContents.Kind == MovieContents.KIND_FILE
                || dispinfoSelectContents.Kind == MovieContents.KIND_CONTENTS)
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

        private void OnAddGroupRegistOrEdit(object sender, RoutedEventArgs e)
        {
            // ClickしたのがButtonでは無い場合は編集ボタンを押下されたと判断
            Button button = sender as Button;
            if (button == null)
            {
                if (dispinfoSelectGroup == null)
                    return;

                txtAddGroupName.Text = dispinfoSelectGroup.Name;
                txtAddGroupExplanation.Text = dispinfoSelectGroup.Explanation;
                txtAddGroupLabel.Text = dispinfoSelectGroup.Label;
                cmbAddGroupKind.SelectedValue = dispinfoSelectGroup.Kind;
                txtAddGroupId.Text = Convert.ToString(dispinfoSelectGroup.Id);
                txtbAddGroupMode.Text = "Edit";
            }
            else
            {
                txtAddGroupName.Text = "";
                txtAddGroupExplanation.Text = "";
                txtAddGroupLabel.Text = "";
                cmbAddGroupKind.SelectedValue = null;
                txtAddGroupId.Text = "";
                txtbAddGroupMode.Text = "Regist";
            }
            dispinfoIsGroupAddVisible = true;
            LayoutChange();
        }
        private void OnAddGroupDelete(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectGroup == null)
                return;

            MessageBoxResult result = MessageBox.Show("DBから削除します", "削除確認", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            ColViewMovieGroup.DbDelete(dispinfoSelectGroup, null);
            dispinfoSelectGroup = null;
        }

        private void OnAddGroupCheck(object sender, RoutedEventArgs e)
        {
            if (dispinfoSelectGroup == null)
                return;

            //MessageBoxResult result = MessageBox.Show("フォルダのチェックをします", "チェック確認", MessageBoxButton.OKCancel);

            //if (result == MessageBoxResult.Cancel)
            //    return;

            if (dispinfoSelectGroup.Kind == 3)
            {
                if (Directory.Exists(dispinfoSelectGroup.Explanation))
                {
                    string[] arrDir = Directory.GetDirectories(dispinfoSelectGroup.Explanation);
                    ColViewMovieContents.Clear();
                    ColViewMovieContents.SetSiteContents(dispinfoSelectGroup.Label, dispinfoSelectGroup.Name);
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
                            contents.SiteName = dispinfoSelectGroup.Label;
                            contents.Name = dirinfo.Name;
                            contents.Kind = MovieContents.KIND_SITECHK_UNREGISTERED;
                            contents.Label = new DirectoryInfo(dispinfoSelectGroup.Explanation).Name; // Filterにかからなくなるので格納
                            contents.ParentPath = new DirectoryInfo(dispinfoSelectGroup.Explanation).Name;

                            SiteDetail s = new SiteDetail(Path.Combine(dispinfoSelectGroup.Explanation, dirinfo.Name));
                            contents.MovieCount = Convert.ToString(s.MovieCount);
                            contents.PhotoCount = Convert.ToString(s.ImageCount);
                            contents.Extension = s.Extention;
                            contents.MovieNewDate = s.MovieNewDate;

                            ColViewMovieContents.AddContents(contents);
                        }
                    }

                    foreach (MovieContents contents in ColViewMovieContents.ColViewListMovieContents)
                    {
                        string contentsFilename = Path.Combine(dispinfoSelectGroup.Explanation, contents.Name);

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

            int kind = Convert.ToInt32(cmbAddGroupKind.SelectedValue);

            if (kind == MovieGroup.KIND_DIR)
            {
                DirectoryInfo dir = new DirectoryInfo(txtAddGroupExplanation.Text);

                if (!dir.Exists)
                {
                    MessageBox.Show("説明に指定されているフォルダが存在しません");
                    return;
                }
            }

            if (txtbAddGroupMode.Text == "Regist")
            {
                MovieGroup registerData = new MovieGroup();
                registerData.Name = txtAddGroupName.Text;
                registerData.Explanation = txtAddGroupExplanation.Text;
                registerData.Label = txtAddGroupLabel.Text;
                registerData.Kind = kind;

                registerData = MovieGroups.DbExport(registerData, dbcon);
                ColViewMovieGroup.Add(registerData);

                ColViewMovieGroup.Refresh(); // .Execute(MovieGroupFilterAndSorts.EXECUTE_MODE_NORMAL);
            }
            else
            {
                dispinfoSelectGroup.Name = txtAddGroupName.Text;
                dispinfoSelectGroup.Explanation = txtAddGroupExplanation.Text;
                dispinfoSelectGroup.Label = txtAddGroupLabel.Text;

                dispinfoSelectGroup.DbUpdate(dbcon);
            }

            txtAddGroupName.Text = "";
            txtAddGroupExplanation.Text = "";
            txtAddGroupLabel.Text = "";
            cmbAddGroupKind.SelectedValue = null;
            txtAddGroupId.Text = "";
            txtbAddGroupMode.Text = "";

            dispinfoIsGroupAddVisible = false;
            LayoutChange();
        }

        private void txtAddGroupId_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (dispinfoSelectGroup == null || textbox.Text.Length <= 0)
            {
                txtbAddGroupMode.Text = "Regist";
            }
            else
            {
                try
                {
                    int id = Convert.ToInt32(txtAddGroupId.Text);
                    if (dispinfoSelectGroup.Id == id)
                        txtbAddGroupMode.Text = "Edit";
                    else
                        txtbAddGroupMode.Text = "Regist";
                }
                catch (Exception)
                {
                    txtbAddGroupMode.Text = "Regist";
                }
            }
        }

        private void btnAddGroupCheck_Click(object sender, RoutedEventArgs e)
        {
            int kind = Convert.ToInt32(cmbAddGroupKind.SelectedValue);

            if (kind == MovieGroup.KIND_DIR)
            {
                DirectoryInfo dir = new DirectoryInfo(txtAddGroupExplanation.Text);

                if (!dir.Exists)
                {
                    MessageBox.Show("説明に指定されているフォルダが存在しません");
                    return;
                }
            }

            MovieGroup filterGroup = new MovieGroup();
            filterGroup.Name = txtAddGroupName.Text;
            filterGroup.Explanation = txtAddGroupExplanation.Text;
            filterGroup.Label = txtAddGroupLabel.Text;
            filterGroup.Kind = kind;

            // 登録・更新で入力されているグループで表示
            ColViewMovieContents.ClearAndExecute(ColViewMovieGroup.FilterKind, filterGroup);
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
            var SelectIedContents = dgridMovieContents.SelectedItems;

            List<MovieContents> MovieContentsList = new List<MovieContents>();

            foreach(MovieContents data in SelectIedContents)
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

        private void txtSiteDetailContentsName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (textbox == null)
                return;

            if (dispinfoSelectContents == null || dispinfoSelectContents.Name.Equals(textbox.Text))
                return;

            MovieContents data = dispinfoSelectContents;
            data.Name = textbox.Text;
            string path = data.GetExistPath(dispinfoTargetGroupBySelectContents);

            if (path == null)
            {
                textbox.Background = new LinearGradientBrush(Colors.Red, Colors.Red, 45);
                return;
            }

            dispinfoSelectContents.Name = textbox.Text;
            dgridMovieContents_SelectionChanged(null, null);
            textbox.Background = new LinearGradientBrush(Colors.Cyan, Colors.Cyan, 45);
        }

        private void btnSiteDetailUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool isChangeFile = false, isChangeDir = false;
            string msg;

            MovieContents dataTarget = null;

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
                string srcPath = Path.Combine(dispinfoTargetGroupBySelectContents.Explanation, dispinfoSelectContents.Name);
                string destPath = Path.Combine(dispinfoTargetGroupBySelectContents.Explanation, txtSiteDetailContentsName.Text);

                if (Directory.Exists(srcPath) && !Directory.Exists(destPath))
                    isChangeDir = true;

                if (isChangeDir)
                {
                    isChangeDir = true;
                    msg = "DBとフォルダを更新します";
                }
                else
                    msg = "DBを更新します（ファイル変更は無し）";

                dataTarget = GetMovieContentsFromTextbox(txtSiteDetailContentsName
                            , txtSiteDetailContentsTag
                            , null
                            , null
                            , null
                            , null
                            , null
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
                    string destFilename = Path.Combine(dispinfoSelectContents.Label, data.FileInfo.Name.Replace(dispinfoSelectContents.Name, txtFileDetailContentsName.Text));
                    File.Move(data.FileInfo.FullName, destFilename);
                }

            }
            if (isChangeDir)
            {
                string srcPath = Path.Combine(dispinfoSelectGroup.Explanation, dispinfoSelectContents.Name);
                string destPath = Path.Combine(dispinfoSelectGroup.Explanation, txtSiteDetailContentsName.Text);

                if (Directory.Exists(srcPath) && !Directory.Exists(destPath))
                    Directory.Move(srcPath, destPath);
            }

            if (dataTarget != null)
            {
                dispinfoSelectContents.RefrectData(dataTarget);

                dispinfoSelectContents.DbUpdate(dbcon);
            }

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
                data.Label = myTextBoxLabel.Text;
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
            if (dispinfoSelectContents.Name != txtFileDetailContentsName.Text)
                txtFileDetailContentsName.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsName.Background = null;

            if (dispinfoSelectContents.Tag != txtFileDetailContentsTag.Text)
                txtFileDetailContentsTag.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
            else
                txtFileDetailContentsTag.Background = null;

            if (Directory.Exists(txtFileDetailContentsLabel.Text))
            {
                if (dispinfoSelectContents.Label != txtFileDetailContentsLabel.Text)
                    txtFileDetailContentsLabel.Background = new LinearGradientBrush(Colors.LightPink, Colors.LightPink, 0.5);
                else
                    txtFileDetailContentsLabel.Background = null;
            }
            else
            {
                txtFileDetailContentsLabel.Background = new LinearGradientBrush(Colors.PaleVioletRed, Colors.PaleVioletRed, 0.5);
                btnFileDetailUpdate.IsEnabled = false;
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

        private void OnDisplayImageMovePage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button.Content.ToString() == "Before")
                image.Before();
            else
                image.Next();

            OnDisplayImage(dispinfoSelectContents, dispinfoTargetGroupBySelectContents);
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
                    // SITE以外のDIRなどの場合
                    if (image == null) return;

                    var point = Mouse.GetPosition(lgridImageContents);

                    int col = 0;
                    double accumulatedWidth = 0.0;

                    // calc col mouse was over
                    foreach (var columnDefinition in lgridImageContents.ColumnDefinitions)
                    {
                        accumulatedWidth += columnDefinition.ActualWidth;
                        if (accumulatedWidth >= point.X)
                            break;
                        col++;
                    }

                    if (col == 0)
                        image.Before();
                    else
                        image.Next();

                    OnDisplayImage(dispinfoSelectContents, dispinfoTargetGroupBySelectContents);
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
            dispinfoSelectContents.RefrectFileInfoAndDbUpdate(ColViewFileDetail, dbcon);

            // ファイル情報の各Controlへの表示を更新
            txtFileDetailContentsName.Text = dispinfoSelectContents.Name;
            txtFileDetailContentsTag.Text = dispinfoSelectContents.Tag;
            txtFileDetailContentsLabel.Text = dispinfoSelectContents.Label;
            txtFileDetailContentsSellDate.Text = dispinfoSelectContents.SellDate.ToString("yyyy/MM/dd");
            txtFileDetailContentsProductNumber.Text = dispinfoSelectContents.ProductNumber;
            txtFileDetailContentsExtension.Text = dispinfoSelectContents.Extension;
            txtFileDetailContentsFileDate.Text = dispinfoSelectContents.FileDate.ToString("yyyy/MM/dd HH:mm:ss");
            txtFileDetailContentsFileCount.Text = Convert.ToString(dispinfoSelectContents.FileCount);
            txtFileDetailContentsCreateDate.Text = dispinfoSelectContents.CreateDate.ToString("yyyy/MM/dd HH:mm:ss");
            txtFileDetailContentsUpdateDate.Text = dispinfoSelectContents.UpdateDate.ToString("yyyy/MM/dd HH:mm:ss");
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

            if (dispinfoSelectContents.Tag != null && dispinfoSelectContents.Tag.Length > 0)
            {
                List<MovieContents> matchData = ColViewMovieContents.GetMatchData(dispinfoSelectContents.Tag);

                int unEvaluate = 0, maxEvaluate = 0, avg = 0;

                if (matchData.Count > 0)
                {
                    int cnt = 0;
                    int total = 0;
                    foreach (MovieContents data in matchData)
                    {
                        if (data.Rating <= 0)
                        {
                            unEvaluate++;
                            continue;
                        }

                        if (maxEvaluate < data.Rating)
                            maxEvaluate = data.Rating;

                        total = total + data.Rating;
                        cnt++;
                    }
                    if (total > 0 && cnt > 0)
                        avg = total / cnt;
                    txtStatusBarFileDate.Text = "未 " + unEvaluate + "/" + matchData.Count + "  Max " + maxEvaluate + "  Avg " + avg;
                }
                else
                    txtStatusBarFileDate.Text = "";
            }
            else
                txtStatusBarFileDate.Text = "";
        }
    }
}
