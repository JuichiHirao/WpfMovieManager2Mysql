﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using WpfMovieManager2.data;

namespace WpfMovieManager2Mysql.detail
{
    public class FileDetail : detail.BaseDetail
    {
        public long Size = 0;
        public DateTime FileDate = new DateTime(1900, 1, 1);
        public int FileCount = 0;
        public string Extension = "";
        public string ProductNumber = "";

        public FileDetail(MovieContents myMovieContents, MovieGroupData myGroup)
        {
            ExistPath = myMovieContents.Path;
            ContentsName = myMovieContents.Name;
            ProductNumber = myMovieContents.ProductNumber;

            if (ExistPath != null)
                DataSet(ExistPath, ContentsName + "*");
        }

        public override void DataSet(string myPath, string myPattern)
        {
            string[] fileList;
            
            try
            {
                fileList = Directory.GetFiles(myPath, myPattern, SearchOption.AllDirectories);
            }
            catch(DirectoryNotFoundException ex)
            {
                fileList = Directory.GetFiles(myPath, "*" + ProductNumber + "*", SearchOption.AllDirectories);
            }

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
                    bool resultExist = File.Exists(file);
                    FileInfo fileInfo = new FileInfo(file);
                    MovieCount++;
                    FileCount++;
                    if (fileInfo.Exists)
                    {
                        Size += fileInfo.Length;
                        FileDate = fileInfo.LastWriteTime;
                        Extension = fileInfo.Extension.Substring(1);
                    }
                    else
                    {

                    }
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
