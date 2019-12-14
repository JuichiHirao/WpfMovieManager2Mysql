using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using WpfMovieManager2.data;

namespace WpfMovieManager2Mysql.common
{

    class Image
    {
        MovieContents data;

        DirectoryInfo targetDirectory;
        MovieGroupData targetGroup;

        public FileInfo PackageFileInfo;
        public List<FileInfo> listImageFileInfo;
        public string[] arrImagePathname = null;
        private int positionList;
        private int pages;
        public string DisplayPage;
        public bool IsThumbnail = false;

        public Image(MovieContents myData, MovieGroupData myGroup)
        {
            data = myData;
            listImageFileInfo = new List<FileInfo>();
            targetGroup = myGroup;

            Settting();
        }

        public void Settting()
        {
            string packagePathname = Path.Combine(data.Path, data.Name + ".jpg");

            if (File.Exists(packagePathname))
            {
                // File
                FileInfo fileinfo = new FileInfo(packagePathname);
                PackageFileInfo = fileinfo;
                targetDirectory = fileinfo.Directory;

                arrImagePathname = Directory.GetFiles(data.Path, data.Name + "_*.jpg");
                if (arrImagePathname.Length > 0)
                {
                    listImageFileInfo.Clear();
                    foreach (string file in arrImagePathname)
                    {
                        fileinfo = new FileInfo(file);

                        if (fileinfo.Name == data.Name + ".jpg")
                            continue;

                        listImageFileInfo.Add(fileinfo);
                    }

                }
                if (listImageFileInfo.Count > 0)
                    IsThumbnail = true;
            }
            else
            {
                // Site
                string patname = Path.Combine(data.Path, data.Name);
                string[] tempImagePathname = null;
                if (Directory.Exists(patname))
                {
                    targetDirectory = new DirectoryInfo(patname);
                    tempImagePathname = Directory.GetFiles(targetDirectory.FullName, "*jpg", SearchOption.TopDirectoryOnly);

                    if (tempImagePathname.Length <= 0)
                        return;

                    Array.Sort(tempImagePathname);

                    arrImagePathname = tempImagePathname;
                    if (arrImagePathname.Length >= 1)
                    {
                        pages = arrImagePathname.Length / 4;
                        SetDisplayImagesPath();
                    }
                }
                else
                {
                    // KoreanPorno
                    tempImagePathname = Directory.GetFiles(data.Path, data.Name + "*.jpg");

                    if (tempImagePathname.Length > 0)
                    {
                        Array.Sort(tempImagePathname);

                        FileInfo fileinfo = new FileInfo(tempImagePathname[0]);
                        PackageFileInfo = fileinfo;
                        targetDirectory = fileinfo.Directory;

                        arrImagePathname = tempImagePathname;
                        if (arrImagePathname.Length >= 1)
                        {
                            pages = arrImagePathname.Length / 4;
                            SetDisplayImagesPath();
                        }

                        IsThumbnail = true;
                    }
                }
            }

            return;
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
            {
                if (arrImagePathname.Length >= 1)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));
                if (arrImagePathname.Length >= 2)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[1]));
                if (arrImagePathname.Length >= 3)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[2]));
            }

            positionList = 0;
            SetDisplayPage(positionList);

            return listImageFileInfo;
        }

        private void SetDisplayImagesPath()
        {
            listImageFileInfo = new List<FileInfo>();

            if (arrImagePathname.Length >= 4)
            {
                listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[1]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[2]));
                listImageFileInfo.Add(new FileInfo(arrImagePathname[3]));
            }
            else
            {
                if (arrImagePathname.Length >= 1)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[0]));
                if (arrImagePathname.Length >= 2)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[1]));
                if (arrImagePathname.Length >= 3)
                    listImageFileInfo.Add(new FileInfo(arrImagePathname[2]));
            }

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
            // "J:\\DVDRIP-17\\KOREAN_PORN13\\ka19112401 KOREAN AMATEUR 2019112401\\여친.3gp" でエラー
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
