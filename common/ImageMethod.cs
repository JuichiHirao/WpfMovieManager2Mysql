using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace wpfMovieManager2Mysql.common
{

    class Image
    {
        MovieContents data;

        int numberSheets;
        DirectoryInfo targetDirectory;
        MovieGroup targetGroup;

        FileInfo imagePackageFileInfo;
        public List<FileInfo> listImageFileInfo;
        private string[] arrImagePathname = null;
        private int positionList;
        private int pages;
        public string DisplayPage;

        public Image(MovieContents myData, MovieGroup myGroup)
        {
            data = myData;
            listImageFileInfo = new List<FileInfo>();
            targetGroup = myGroup;

            Settting();
        }
        public Image(MovieContents myData, int myNumberSheets, MovieGroup myGroup)
        {
            data = myData;
            numberSheets = myNumberSheets;
            listImageFileInfo = new List<FileInfo>();
            targetGroup = myGroup;

            DisplayPage = "";
            Settting();
        }
        public void Settting()
        {
            // File
            if (data.Kind == MovieContents.KIND_FILE
                || data.Kind == MovieContents.KIND_CONTENTS)
            {
                FileInfo fileinfo = null;
                if (data.Kind == MovieContents.KIND_CONTENTS)
                {
                    string path = data.GetExistPath(targetGroup);

                    if (path != null)
                        data.Label = path;
                }
                if (File.Exists(Path.Combine(data.Label, data.Name + ".jpg")))
                    fileinfo = new FileInfo(Path.Combine(data.Label, data.Name + ".jpg"));

                if (fileinfo != null && fileinfo.Exists)
                {
                    imagePackageFileInfo = fileinfo;
                    targetDirectory = fileinfo.Directory;
                }

                // KoreanPornoなどのフォルダ名でPhotoが存在する場合
                if (Directory.Exists(Path.Combine(data.Label, data.Name)))
                {
                    DirectoryInfo imageDirInfo = new DirectoryInfo(Path.Combine(data.Label, data.Name));

                    if (imageDirInfo != null)
                    {
                        arrImagePathname = Directory.GetFiles(imageDirInfo.FullName, "*jpg", SearchOption.AllDirectories);

                        if (arrImagePathname.Length <= 0)
                            return;

                        Array.Sort(arrImagePathname);

                        if (arrImagePathname.Length >= 1)
                        {
                            pages = arrImagePathname.Length / 4;
                            SetDisplaySiteImagesPath(imageDirInfo.FullName);
                        }
                    }
                }
            }
            // Site
            else if (data.Kind == MovieContents.KIND_SITE)
            {
                if (Directory.Exists(Path.Combine(targetGroup.Explanation, data.Name)))
                    targetDirectory = new DirectoryInfo(Path.Combine(targetGroup.Explanation, data.Name));

                if (targetDirectory == null)
                    return;

                arrImagePathname = Directory.GetFiles(targetDirectory.FullName, "*jpg", SearchOption.TopDirectoryOnly);

                if (arrImagePathname.Length <= 0)
                    return;

                Array.Sort(arrImagePathname);

                if (arrImagePathname.Length >= 1)
                {
                    pages = arrImagePathname.Length / 4;
                    SetDisplaySiteImagesPath(targetDirectory.FullName);
                }
            }

            return;
        }
        public bool IsThumbnail()
        {
            if (data.Kind == MovieContents.KIND_FILE)
            {
                FileInfo fileinfo = null;
                if (File.Exists(Path.Combine(data.Label, data.Name + "_th.jpg")))
                    fileinfo = new FileInfo(Path.Combine(data.Label, data.Name + "_th.jpg"));

                if (fileinfo != null && fileinfo.Exists)
                {
                    listImageFileInfo.Clear();
                    listImageFileInfo.Add(fileinfo);
                    return true;
                }
            }

            // KIND_FILE以外はサムネイル画像は無い
            return false;
        }
        public FileInfo GetDefaultPackageFileInfo()
        {
            if (data.Kind == MovieContents.KIND_FILE)
                return imagePackageFileInfo;

            if (data.Kind == MovieContents.KIND_SITE)
            {
                //SetDisplaySiteImagesPath(targetDirectory.FullName);
            }

            return null;
        }
        private List<FileInfo> GetContentsImages()
        {
            string searchPath = "";

            if (data.Kind == MovieContents.KIND_FILE)
            {
                searchPath = System.IO.Path.Combine(searchPath, data.Name);
            }
            else if (data.Kind == MovieContents.KIND_SITE)
            {
                searchPath = targetDirectory.FullName;
            }

            if (!Directory.Exists(searchPath))
                return listImageFileInfo;

            listImageFileInfo = new List<FileInfo>();

            if (arrImagePathname.Length >= 4)
            {
                listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[1]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[2]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[3]));
            }
            else
                listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));

            positionList = 0;
            SetDisplayPage(positionList);

            return listImageFileInfo;
        }

        private void SetDisplaySiteImagesPath(string mySitePath)
        {
            if (!Directory.Exists(mySitePath))
                return;

            listImageFileInfo = new List<FileInfo>();

            if (arrImagePathname.Length >= 4)
            {
                listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[1]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[2]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[3]));
            }
            else
                listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));

            positionList = 0;
            SetDisplayPage(positionList);

            return;
        }

        private void SetDisplayPage(int myPosi)
        {
            if (pages <= 0)
            {
                DisplayPage = "";
                return;
            }
            int pageNow = 1;
            if (myPosi > 0)
                pageNow = (myPosi / 4) + 1;

            if (listImageFileInfo.Count > 0)
                DisplayPage = pageNow + "/" + pages;
        }

        public void Next()
        {
            if (arrImagePathname == null || arrImagePathname.Length <= 0)
                return;

            int posi = positionList + 4;

            if (arrImagePathname.Length >= posi + 1)
            {
                listImageFileInfo = new List<FileInfo>();
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi]));
                positionList = posi;
            }
            if (arrImagePathname.Length >= posi + 2)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 1]));
            if (arrImagePathname.Length >= posi + 3)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 2]));
            if (arrImagePathname.Length >= posi + 4)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 3]));

            SetDisplayPage(posi);
        }

        public void Before()
        {
            if (arrImagePathname == null || arrImagePathname.Length <= 0)
                return;

            int posi = positionList - 4;

            if (posi < 0)
                posi = 0;

            if (arrImagePathname.Length >= posi)
            {
                listImageFileInfo = new List<FileInfo>();
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi]));
                positionList = posi;
            }
            if (arrImagePathname.Length >= posi + 2)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 1]));
            if (arrImagePathname.Length >= posi + 3)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 2]));
            if (arrImagePathname.Length >= posi + 4)
                listImageFileInfo.Add(new FileInfo(arrImagePathname[posi + 3]));

            SetDisplayPage(posi);
        }
    }
    class ImageMethod
    {
        public static BitmapImage GetImageStream(string myImagePathname)
        {
            if (!System.IO.File.Exists(myImagePathname))
                return null;

            BitmapImage bitmap = new BitmapImage();
            var stream = System.IO.File.OpenRead(myImagePathname);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            stream.Close();
            stream.Dispose();
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            return bitmap;
        }
    }
}
