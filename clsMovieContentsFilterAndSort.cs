using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WpfMovieManager2Mysql;

namespace wpfMovieManager2Mysql
{
    public class MovieContentsFilterAndSort
    {
        MySqlDbConnection dbcon;
        public List<MovieContents> listMovieContens;
        public ICollectionView ColViewListMovieContents;

        string sortItem;
        ListSortDirection sortOrder;

        string FilterSearchText = "";
        string FilterLabel = "";
        string FilterActress = "";
        string FilterSiteName = "";
        string FilterParentPath = "";

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

        public MovieContents GetTestNumberSite(string mySiteName, int myNum)
        {
            int idx = 0;
            foreach(MovieContents data in listMovieContens)
            {
                if (data.Kind != MovieContents.KIND_SITE)
                    continue;

                if (mySiteName.Equals(data.SiteName))
                {
                    if (idx == myNum)
                        return data;

                    idx++;
                }
            }
            return null;
        }
        public MovieContentsFilterAndSort(MySqlDbConnection myDbCon)
        {
            dbcon = myDbCon;

            DataSet();
        }

        public MovieContentsFilterAndSort()
        {
            dbcon = new MySqlDbConnection();

            DataSet();
        }

        private void DataSet()
        {
            listMovieContens = MovieContentsParent.GetDbViewContents(dbcon);

            ColViewListMovieContents = CollectionViewSource.GetDefaultView(listMovieContens);
        }

        public void Clear()
        {
            FilterSearchText = "";
            FilterLabel = "";
            FilterActress = "";
            FilterSiteName = "";
            FilterParentPath = "";

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
            FilterParentPath = myParentPath;
        }
        // dgridMovieGroup_SelectionChangedから呼び出し
        public GroupFilesInfo ClearAndExecute(int myFilterKind, MovieGroup myGroupData)
        {
            Clear();

            if (myGroupData.Kind == 1)
                SetLabel(myGroupData.Explanation);
            else if (myGroupData.Kind == 3)
                SetSiteContents(myGroupData.Label, myGroupData.Name);
            else if (myGroupData.Kind == 4)
                SetActress(myGroupData.Name);

            GroupFilesInfo FilesInfo = Execute();

            return FilesInfo;
        }
        public GroupFilesInfo Execute()
        {
            string[] manyActress = null;
            string[] sepa = { "／" };

            GroupFilesInfo FilesInfo = new GroupFilesInfo();

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
            if (FilterParentPath.Length > 0)
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
                    if (data.Label.ToUpper() == FilterLabel.ToUpper())
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
                    if (FilterSiteName == data.SiteName)
                        matchCount++;
                }
                if (FilterParentPath.Length > 0)
                {
                    //if (data.Label.Length > 0 && FilterParentPath.IndexOf(data.Label) >= 0)
                    if (data.Label.Length > 0 && FilterParentPath.Equals(data.Label))
                            matchCount++;
                }

                if (TargetMatchCount <= matchCount)
                {
                    FilesInfo.Size += data.Size;
                    FilesInfo.FileCount++;
                    if (data.Rating <= 0)
                        FilesInfo.Unrated++;

                    return true;
                }

                return false;
            };

            return FilesInfo;
        }

        public List<MovieContents> GetMatchData(string myTag)
        {
            List<MovieContents> matchData = new List<MovieContents>();

            //string[] arrTagActress = myTag.Split(',');
            foreach (MovieContents data in listMovieContens)
            {
                if (data.Tag.IndexOf(myTag) >= 0)
                    matchData.Add(data);
                //if (arrTagActress != null && arrTagActress.Count() > 0)
            }

            return matchData;
        }

        public void Delete(MovieContents myData)
        {
            listMovieContens.Remove(myData);
        }
    }
}
