using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WpfMovieManager2.common;
using WpfMovieManager2.data;
using WpfMovieManager2.service;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2.collection
{
    class FavCollection
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public List<FavData> listData;
        public ICollectionView ColViewListData;

        FavService service;

        string FilterSearchText = "";
        string FilterLabel = "";
        string FilterTypeName = "";

        public FavCollection()
        {
            service = new FavService();

            listData = service.GetList(new MySqlDbConnection());
            ColViewListData = CollectionViewSource.GetDefaultView(listData);
        }

        public void SetSort(string mySortItem, ListSortDirection sortOrder)
        {
            if (ColViewListData != null && ColViewListData.CanSort == true)
            {
                ColViewListData.SortDescriptions.Clear();
                ColViewListData.SortDescriptions.Add(new SortDescription(mySortItem, sortOrder));
            }
        }

        public void Delete(FavData myFav)
        {
            service.Delete(myFav, new MySqlDbConnection());

            listData.Remove(myFav);
        }

        public void Add(FavData myFav)
        {
            FavData newData = service.Export(myFav, new MySqlDbConnection());

            listData.Add(newData);
        }

        public string[] GetMatch(string myActress)
        {
            List<string> listMatch = new List<string>();

            foreach(FavData data in listData)
            {
                if (data.Label.IndexOf(myActress) >= 0)
                    listMatch.AddRange(Actress.AppendMatch(data.Label, listMatch));

                if (data.Name.IndexOf(myActress) >= 0)
                    listMatch.AddRange(Actress.AppendMatch(data.Name, listMatch));
            }

            if (!listMatch.Exists(x => x == myActress))
                listMatch.Add(myActress);

            return listMatch.ToArray();
        }

        public void Update(FavData myFav)
        {
            service.Update(myFav, new MySqlDbConnection());
        }

        public void SetSearchText(string myText)
        {
            FilterSearchText = myText;
        }

        public void SetType(string myTypeName)
        {
            FilterTypeName = myTypeName;
        }

        public void Refresh()
        {
            ColViewListData.Refresh();
        }

        public void Execute()
        {
            int TargetMatchCount = 0;
            if (FilterSearchText.Length > 0)
                TargetMatchCount++;
            if (FilterLabel.Length > 0)
                TargetMatchCount++;
            if (FilterTypeName.Length > 0)
                TargetMatchCount++;

            ColViewListData.Filter = delegate (object o)
            {
                FavData data = o as FavData;
                int MatchCount = 0;

                if (FilterSearchText.Length > 0)
                {
                    if (data.Label.ToUpper().IndexOf(FilterSearchText.ToUpper()) >= 0)
                        MatchCount++;
                }
                if (FilterLabel.Length > 0)
                {
                    if (FilterLabel == data.Label)
                        MatchCount++;
                }

                if (FilterTypeName.Length > 0 && data.Type == FilterTypeName)
                    MatchCount++;

                if (TargetMatchCount <= MatchCount)
                    return true;
                else
                    return false;
            };
        }
    }
}
