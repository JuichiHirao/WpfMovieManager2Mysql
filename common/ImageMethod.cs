using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using WpfMovieManager2.data;

namespace WpfMovieManager2Mysql.common
{
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
