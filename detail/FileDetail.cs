using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpfMovieManager2Mysql.detail
{
    public class FileDetail : detail.BaseDetail
    {
        public long Size = 0;
        public DateTime FileDate = new DateTime(1900, 1, 1);
        public int FileCount = 0;
        public string Extension = "";

        public FileDetail(MovieContents myMovieContents, MovieGroup myGroup)
        {
            ExistPath = myMovieContents.Path;
            ContentsName = myMovieContents.Name;

            if (ExistPath != null)
                DataSet(ExistPath, ContentsName + "*");
        }

        public override void DataSet(string myPath, string myPattern)
        {
            string[] fileList = Directory.GetFiles(myPath, myPattern, SearchOption.AllDirectories);

            ImageCount = 0;
            MovieCount = 0;
            ListCount = 0;
            FileCount = 0;
            Size = 0;

            listFileInfo.Clear();
            foreach (string file in fileList)
            {
                // FileContentsに長いファイル名をTARGETに変える
                listFileInfo.Add(new common.FileContents(file, myPattern));

                if (regexMov.IsMatch(file))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    MovieCount++;
                    Size += fileInfo.Length;
                    FileDate = fileInfo.LastWriteTime;
                    FileCount++;
                    Extension = fileInfo.Extension.Substring(1);
                }
                if (regexJpg.IsMatch(file))
                    ImageCount++;
                if (regexLst.IsMatch(file))
                    ListCount++;

                if (regexJpg.IsMatch(file) && ImageCount == 1)
                    StartImagePathname = file;
            }

            ColViewListFileInfo = CollectionViewSource.GetDefaultView(listFileInfo);

            if (ColViewListFileInfo != null && ColViewListFileInfo.CanSort == true)
            {
                ColViewListFileInfo.SortDescriptions.Clear();
                ColViewListFileInfo.SortDescriptions.Add(new SortDescription("FileInfo.LastWriteTime", ListSortDirection.Ascending));
            }
        }

        public void Refresh()
        {
            if (ExistPath != null)
                DataSet(ExistPath, ContentsName + "*");
        }

        public override void Execute(int myKind)
        {
            ColViewListFileInfo.Filter = delegate (object o)
            {
                return true;
            };
        }
    }
}
