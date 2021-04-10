using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WpfMovieManager2.data;

namespace WpfMovieManager2Mysql
{
    class CommonMethod
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }
        public sealed class NaturalStringComparer : IComparer
        {
            public int Compare(object a, object b)
            {
                var lhs = (MovieGroupData)a;
                var rhs = (MovieGroupData)b; //APPLY ALGORITHM LOGIC HERE
                return SafeNativeMethods.StrCmpLogicalW(lhs.Path, rhs.Path);
            }
        }

        public static Dictionary<string, string> ToggleButtonType = new Dictionary<string, string>()
        {
            { "D", "file" }, // Directory
            { "S", "site" }, // Site
            { "A", "actress" },  // Actress
            { "K", "keyword" }  // Favorite Custom
        };

        public static long GetLong(string myStrLong)
        {
            long data = 0;
            data = Convert.ToInt64(myStrLong);

            return data;
        }
        public static DateTime GetDateTime(string myStrLDateTime)
        {
            if (myStrLDateTime.Length <= 0)
                return new DateTime(1900, 1, 1);

            DateTime dt;
            dt = Convert.ToDateTime(myStrLDateTime);

            return dt;
        }
        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }
        public static List<T> FindVisualChild<T>(UIElement myPanel, string myType) where T : UIElement
        {
            UIElement parent = myPanel;
            UIElement child;
            T childT = null;
            int idx = 0;
            int max = VisualTreeHelper.GetChildrenCount(parent);

            List<T> list = new List<T>();
            while (idx < max)
            {
                child = VisualTreeHelper.GetChild(parent, idx) as UIElement;

                string type = child.GetType().ToString();

                if (type.IndexOf(myType) >= 0)
                {
                    childT = child as T;
                    list.Add(childT);
                }
                //Debug.Print(type);
                idx++;
            }
            return list;
        }
        /// <summary>
        /// ステータスバーへ表示するための評価情報を生成する
        /// </summary>
        /// <param name="myListMovieFiles"></param>
        /// <returns></returns>
        public static string GetStatusBarEvaluate(string myBaseDir, List<MovieContents> myListMovieFiles)
        {
            long FileSize = 0;
            int FileEvaluated = 0, FileUnEvaluate = 0;
            foreach (MovieContents data in myListMovieFiles)
            {
                // グリッド内のサムネイル情報を設定する（緑の丸画像）
                string ThumbnailFilename = System.IO.Path.Combine(myBaseDir, data.Name + "_th.jpg");

                if (Directory.Exists(myBaseDir))
                {
                    if (System.IO.File.Exists(ThumbnailFilename))
                    {
                        data.IsExistsThumbnail = true;
                    }
                }
                FileSize += data.Size;

                if (data.Rating > 0)
                    FileEvaluated++;
                else
                    FileUnEvaluate++;
            }

            return "全ファイル [" + myListMovieFiles.Count() + "] （評価済 [" + FileEvaluated + "] ／未評価 [" + FileUnEvaluate + "]）";
        }
        public static string GetDisplaySize(long mySize)
        {
            double DoubleSize = 0;
            string Unit = "", SizeStr = "";
            double Long1024 = 1024;
            // M（メガ）
            if (mySize < Long1024 * Long1024)
            {
                DoubleSize = mySize / Long1024;
                Unit = "K";
            }
            else if (mySize < Long1024 * Long1024 * Long1024)
            {
                DoubleSize = mySize / Long1024 / Long1024;
                Unit = "M";
            }
            else if (mySize < Long1024 * Long1024 * Long1024 * Long1024)
            {
                DoubleSize = mySize / Long1024 / Long1024 / Long1024;
                Unit = "G";
            }
            else if (mySize < Long1024 * Long1024 * Long1024 * Long1024 * Long1024)
            {
                DoubleSize = mySize / Long1024 / Long1024 / Long1024 / Long1024;
                Unit = "T";
            }

            if (DoubleSize < 10)
                SizeStr = String.Format("{0:###,###,###,###.00#}{1}", DoubleSize, Unit);
            else if (DoubleSize < 100)
                SizeStr = String.Format("{0:###,###,###,###.0##}{1}", DoubleSize, Unit);
            else
                SizeStr = String.Format("{0:###,###,###,###}{1}", DoubleSize, Unit);

            return SizeStr;
        }
    }

}
