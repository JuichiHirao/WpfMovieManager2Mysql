using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WpfMovieManager2.data;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2Mysql
{
    public class MovieContentsFilterAndSort
    {
        MySqlDbConnection dbcon;
        MySqlDbConnection dockerMySqlConn;
        public List<MovieContents> listMovieContens;
        public ICollectionView ColViewListMovieContents;

        string FilterSearchText = "";
        string FilterLabel = "";
        string FilterActress = "";
        string FilterSiteName = "";

        public bool IsFilterAv = false;
        public bool IsFilterIv = false;
        public bool IsFilterUra = false;
        public bool IsFilterComment = false;
        public bool IsFilterTag = false;
        public bool IsFilterGroupCheck = false;

        public void AddContents(MovieContents myContents)
        {
            listMovieContens.Add(myContents);
        }

        public MovieContentsFilterAndSort(MySqlDbConnection myDbCon)
        {
            dbcon = myDbCon;

            DataSet();
        }

        public MovieContentsFilterAndSort()
        {
            dbcon = new MySqlDbConnection();
            try
            {
                dockerMySqlConn = new MySqlDbConnection(0);
            }
            catch(Exception e)
            {
                Debug.Write(e);
                dockerMySqlConn = null;
            }

            DataSet();
        }

        private void DataSet()
        {
            if (dockerMySqlConn == null)
                listMovieContens = MovieContentsParent.GetDbViewContents(dbcon);
            else
                listMovieContens = MovieContentsParent.GetDbViewContents(dockerMySqlConn);

            ColViewListMovieContents = CollectionViewSource.GetDefaultView(listMovieContens);
        }

        public void Clear()
        {
            FilterSearchText = "";
            FilterLabel = "";
            FilterActress = "";
            FilterSiteName = "";

            //ColViewListMovieContents.Filter = null;
            //ColViewListMovieContents.SortDescriptions.Clear();
        }

        public void SetSort(string mySortItem, ListSortDirection sortOrder)
        {
            if (ColViewListMovieContents != null && ColViewListMovieContents.CanSort == true)
            {
                ColViewListMovieContents.SortDescriptions.Clear();
                ColViewListMovieContents.SortDescriptions.Add(new SortDescription(mySortItem, sortOrder));
            }
        }

        public void SetSearchText(string myText)
        {
            FilterSearchText = myText;
        }

        public void SetLabel(string myLabel)
        {
            FilterLabel = myLabel;
        }
        public void SetActress(string myActress)
        {
            FilterActress = myActress;
        }
        public void SetSiteContents(string mySiteName, string myParentPath)
        {
            FilterSiteName = mySiteName;
        }
        // dgridMovieGroup_SelectionChangedから呼び出し
        public StoreGroupInfoData ClearAndExecute(MovieGroupData myGroupData)
        {
            Clear();

            SetLabel(myGroupData.Label);

            StoreGroupInfoData infoData = Execute();

            return infoData;
        }
        public StoreGroupInfoData ClearAndExecute(FavData myGroupData)
        {
            Clear();

            SetActress(myGroupData.Label);

            StoreGroupInfoData infoData = Execute();

            return infoData;
        }

        public StoreGroupInfoData Execute()
        {
            string[] manyActress = null;
            string[] sepa = { "／" };

            StoreGroupInfoData infoData = new StoreGroupInfoData();

            if (FilterActress.IndexOf("／") >= 0)
            {
                manyActress = FilterActress.Split(sepa, StringSplitOptions.None);
            }

            int TargetMatchCount = 0;
            if (FilterSearchText.Length > 0)
                TargetMatchCount++;
            if (FilterLabel.Length > 0)
                TargetMatchCount++;
            if (FilterActress.Length > 0)
                TargetMatchCount++;
            if (FilterSiteName.Length > 0)
                TargetMatchCount++;

            ColViewListMovieContents.Filter = delegate (object o)
            {
                MovieContents data = o as MovieContents;
                if (IsFilterAv || IsFilterIv || IsFilterUra || IsFilterComment || IsFilterTag)
                {
                    if (IsFilterAv)
                        if (data.Name.IndexOf("[AV") < 0 && data.Name.IndexOf("[DMMR-AV") < 0)
                            return false;

                    if (IsFilterIv)
                        if (data.Name.IndexOf("[IV") < 0)
                            return false;

                    if (IsFilterUra)
                        if (data.Name.IndexOf("[裏") < 0)
                            return false;

                    if (IsFilterComment)
                        if (data.Comment == null || data.Comment.Length <= 0)
                            return false;

                    if (IsFilterTag)
                        if (data.Tag == null || data.Tag.Length <= 0)
                            return false;
                }

                int matchCount = 0;
                if (FilterSearchText.Length > 0)
                {
                    if (data.Name.ToUpper().IndexOf(FilterSearchText.ToUpper()) >= 0)
                        matchCount++;
                    else if (data.Comment.ToUpper().IndexOf(FilterSearchText.ToUpper()) >= 0)
                        matchCount++;
                    else if (data.Tag.ToUpper().IndexOf(FilterSearchText.ToUpper()) >= 0)
                        matchCount++;
                }

                if (FilterLabel.Length > 0)
                {
                    if (data.StoreLabel.ToUpper() == FilterLabel.ToUpper())
                        matchCount++;
                }

                if (FilterActress.Length > 0)
                {
                    if (manyActress != null)
                    {
                        foreach(string actress in manyActress)
                        {
                            if (data.Tag.IndexOf(actress) >= 0
                            || data.Name.IndexOf(actress) >= 0)
                                matchCount++;
                        }
                    }
                    else if (data.Tag.IndexOf(FilterActress) >= 0
                            || data.Name.IndexOf(FilterActress) >= 0)
                        matchCount++;
                }

                if (FilterSiteName.Length > 0)
                {
                    if (data.StoreLabel.IndexOf(FilterSiteName) >= 0)
                        matchCount++;
                }

                if (TargetMatchCount <= matchCount)
                {
                    infoData.Size += data.Size;
                    infoData.FileCount++;
                    if (data.Rating <= 0)
                        infoData.Unrated++;

                    return true;
                }

                return false;
            };

            return infoData;
        }

        public List<MovieContents> GetMatchData(string[] myArrActress)
        {
            List<MovieContents> listMatchData = new List<MovieContents>();

            //string[] arrTagActress = myTag.Split(',');
            foreach (MovieContents data in listMovieContens)
            {
                foreach(string actress in myArrActress)
                {
                    if (data.Tag.IndexOf(actress) >= 0)
                        listMatchData.Add(data);
                }
            }

            return listMatchData;
        }

        public List<MovieContents> GetLikeFilenameData(string[] myArrActress)
        {
            List<MovieContents> listMatchData = new List<MovieContents>();

            //string[] arrTagActress = myTag.Split(',');
            foreach (MovieContents data in listMovieContens)
            {
                foreach (string actress in myArrActress)
                {
                    if (data.Name.IndexOf(actress) >= 0 && data.Tag.IndexOf(actress) < 0)
                        listMatchData.Add(data);
                }
            }

            return listMatchData;
        }

        public void Delete(MovieContents myData)
        {
            listMovieContens.Remove(myData);
        }
    }
}
