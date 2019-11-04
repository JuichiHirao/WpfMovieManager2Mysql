using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfMovieManager2Mysql
{
    public class SiteDetail : detail.BaseDetail
    {
        public const string DEFAULT_FILE_PATTERN = "*";
        public const int FILTER_KIND_IMAGE = 1;
        public const int FILTER_KIND_MOVIE = 2;
        public const int FILTER_KIND_LIST = 3;

        public SiteDetail(string myPath)
        {
            ExistPath = myPath;
            DataSet(ExistPath, DEFAULT_FILE_PATTERN);
        }

        public override void DataSet(string myPath, string myPattern)
        {
            this.Refresh();

            ColViewListFileInfo = CollectionViewSource.GetDefaultView(listFileInfo);

            if (ColViewListFileInfo != null && ColViewListFileInfo.CanSort == true)
            {
                ColViewListFileInfo.SortDescriptions.Clear();
                ColViewListFileInfo.SortDescriptions.Add(new SortDescription("FileInfo.LastWriteTime", ListSortDirection.Ascending));
            }
        }

        public void Refresh()
        {
            listFileInfo.Clear();

            string[] fileList = Directory.GetFiles(ExistPath, DEFAULT_FILE_PATTERN, SearchOption.AllDirectories);

            foreach (string file in fileList)
            {
                listFileInfo.Add(new common.FileContents(file, ExistPath));

                if (regexMov.IsMatch(file))
                {
                    FileInfo fileinfo = new FileInfo(file);
                    Extention = fileinfo.Extension.Replace(".", "");
                    MovieCount++;
                    if (MovieNewDate.Year <= 1900 || fileinfo.LastWriteTime < MovieNewDate)
                        MovieNewDate = fileinfo.LastWriteTime;
                }
                if (regexJpg.IsMatch(file))
                    ImageCount++;
                if (regexLst.IsMatch(file))
                    ListCount++;

                if (regexJpg.IsMatch(file) && ImageCount == 1)
                    StartImagePathname = file;
            }
        }

        public List<common.FileContents> GetList()
        {
            List<common.FileContents> listPlayListOnly = new List<common.FileContents>();

            Execute(FILTER_KIND_LIST);

            ColViewListFileInfo.SortDescriptions.Clear();
            ColViewListFileInfo.SortDescriptions.Add(new SortDescription("FileInfo.LastWriteTime", ListSortDirection.Descending));

            foreach (common.FileContents data in ColViewListFileInfo)
            {
                listPlayListOnly.Add(data);
            }

            return listPlayListOnly;
        }

        public override void Execute(int myKind)
        {
            if (myKind == 0)
            {
                ColViewListFileInfo.Filter = null;
                return;
            }

            ColViewListFileInfo.Filter = delegate (object o)
            {
                common.FileContents data = o as common.FileContents;

                if (myKind == FILTER_KIND_IMAGE)
                {
                    if (regexJpg.IsMatch(data.FileInfo.Name))
                        return true;
                }

                if (myKind == FILTER_KIND_MOVIE)
                {
                    if (regexMov.IsMatch(data.FileInfo.Name))
                        return true;
                }

                if (myKind == FILTER_KIND_LIST)
                {
                    if (regexLst.IsMatch(data.FileInfo.Name))
                        return true;
                }
                if (regexLst.IsMatch(data.FileInfo.Name) && myKind == FILTER_KIND_LIST)
                    return true;

                return false;
            };
        }
    }
}
