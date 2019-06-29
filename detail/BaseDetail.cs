using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wpfMovieManager2Mysql.detail
{
    abstract public class BaseDetail
    {
        DbConnection dbcon;

        public List<common.FileContents> listFileInfo = new List<common.FileContents>();
        public ICollectionView ColViewListFileInfo;
        public string ExistPath = "";
        public string ContentsName = "";

        public string StartImagePathname = "";

        public int ImageCount = 0;
        public int MovieCount = 0;
        public int ListCount = 0;

        public DateTime MovieNewDate = new DateTime(1900, 1, 1);

        private string _Extention = "";

        public string Extention
        {
            get
            {
                return _Extention;
            }
            set
            {
                if (_Extention.ToUpper().IndexOf(value.ToUpper()) >= 0)
                    return;

                if (_Extention.Length > 0)
                    _Extention += " " + value.ToUpper();
                else
                    _Extention = value.ToUpper();

                return;
            }
        }

        protected Regex regexMov = new Regex(MovieContents.REGEX_MOVIE_EXTENTION, RegexOptions.IgnoreCase);
        protected Regex regexJpg = new Regex(@".*\.jpg$|.*\.jpeg$", RegexOptions.IgnoreCase);
        protected Regex regexLst = new Regex(@".*\.wpl$|.*\.asx$|list", RegexOptions.IgnoreCase);

        public abstract void DataSet(string myPath, string myPattern);

        public abstract void Execute(int myKind);

        public void Delete(common.FileContents myFileContents)
        {
            listFileInfo.Remove(myFileContents);
            ColViewListFileInfo.Refresh();
        }
    }
}
