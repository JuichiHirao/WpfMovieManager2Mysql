using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMovieManager2Mysql.common
{
    public class FileContents
    {
        /// <summary>
        /// MOVIE_FILEの場合にファイル名が長いのでTARGETに変えて表示
        /// </summary>
        /// <param name="myPathName"></param>
        /// <param name="myBasePathOrPattern"></param>
        public FileContents(string myPathName, string myBasePathOrPattern)
        {
            FileInfo = new FileInfo(myPathName);

            string temp;
            if (myBasePathOrPattern.IndexOf("*") > 0)
            {
                // FileDetail用
                temp = myBasePathOrPattern.Replace("*", "");
                DisplayName = FileInfo.Name.Replace(temp, "TARGET");
            }
            else
            {
                // SiteDetail用
                temp = FileInfo.DirectoryName.Replace(myBasePathOrPattern, "");
                if (temp.Length > 0)
                {
                    DisplayName = Path.Combine(temp.Replace("\\", ""), FileInfo.Name);
                }
                else
                    DisplayName = FileInfo.Name;
            }
        }
        public FileInfo FileInfo { get; set; }
        public string DisplayName { get; set; }
    }
}
