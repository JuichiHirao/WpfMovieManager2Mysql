using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WpfMovieManager2Mysql;
using WpfMovieManager2.service;
using NLog;
using WpfMovieManager2.data;

namespace WpfMovieManager2.collection
{
    class StoreCollection
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public List<MovieGroupData> listData;
        public ICollectionView ColViewListData;

        StoreService service;

        string FilterSearchText = "";
        string FilterLabel = "";
        string FilterTypeName = "";

        public StoreCollection()
        {
            service = new StoreService();

            listData = service.GetList(new MySqlDbConnection());
            ColViewListData = CollectionViewSource.GetDefaultView(listData);

            if (ColViewListData != null && ColViewListData.CanSort == true)
            {
                ColViewListData.SortDescriptions.Clear();
                ColViewListData.SortDescriptions.Add(new SortDescription("UpdatedAt", ListSortDirection.Descending));
            }
        }

        public MovieGroupData GetMatchLabel(string myLabel)
        {
            var result = from store in listData where store.Label == myLabel
                        orderby store
                        select store;

            if (result.Count() == 1)
            {
                foreach (MovieGroupData data in result)
                    return data;
            }

            return null;
        }

        public void SetSort(string mySortItem, ListSortDirection sortOrder)
        {
            if (ColViewListData != null && ColViewListData.CanSort == true)
            {
                ColViewListData.SortDescriptions.Clear();
                ColViewListData.SortDescriptions.Add(new SortDescription(mySortItem, sortOrder));
            }
        }

        public void Delete(MovieGroupData myGroup)
        {
            service.Delete(myGroup, new MySqlDbConnection());

            listData.Remove(myGroup);
        }

        public void Add(MovieGroupData myGroup)
        {
            MovieGroupData newData = service.Export(myGroup, new MySqlDbConnection());

            listData.Add(newData);
        }

        public void Update(MovieGroupData myGroup)
        {
            service.Update(myGroup, new MySqlDbConnection());
        }

        public void SetSearchText(string myText)
        {
            FilterSearchText = myText;
        }

        public void SetType(string myTypeName)
        {
            FilterTypeName = myTypeName;
        }

        public void SetSiteName(string mySiteName)
        {
            if (mySiteName != null && mySiteName.Length > 0)
            {
                FilterLabel = mySiteName;
                FilterTypeName = "site";
            }
            else
                FilterLabel = mySiteName;
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
                MovieGroupData data = o as MovieGroupData;
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
            /*
            if (ColViewListData != null && ColViewListData.CanSort == true)
            {
                if (sortItem != null && sortItem.Length > 0)
                {
                    if (myMode == EXECUTE_MODE_NATURAL_COMPARE)
                    {
                        ColViewListMovieGroup.SortDescriptions.Clear();
                        ColViewListMovieGroup.CustomSort = new NaturalStringComparer();
                    }
                    else
                    {
                        ColViewListMovieGroup.SortDescriptions.Clear();
                        ColViewListMovieGroup.SortDescriptions.Add(new SortDescription(sortItem, sortOrder));
                    }
                }
            }
             */
        }

    }
}
