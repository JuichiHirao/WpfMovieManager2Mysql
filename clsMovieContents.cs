using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using WpfMovieManager2Mysql;
using MySql.Data.MySqlClient;

namespace WpfMovieManager2Mysql
{
    public class MovieContents : INotifyPropertyChanged
    {
        public const int KIND_FILE = 1;
        public const int KIND_SITE = 2;
        public const int KIND_CONTENTS = 3;
        public const int KIND_SITECHK_UNREGISTERED = 11;
        public const int KIND_SITECHK_NOTEXIST = 12;

        public static string REGEX_MOVIE_EXTENTION = @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*mkv$|.*rm$|.*rmvb$|.*m4v$|.*3gp$|.*mov$";
        public static string REGEX_IMAGE_EXTENTION = @".*\.jpg$|.*\.png$|.*\.gif$";
        //  @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*jpg$|.*jpesg$|.*iso$|.*mkv$";

        public static string TABLE_KIND_MOVIE_CONTENTS = "MOVIE_CONTENTS_RENEW";
        public static string TABLE_KIND_MOVIE_FILESCONTENTS = "MOVIE_FILES";
        public static string TABLE_KIND_MOVIE_SITECONTENTS = "MOVIE_SITECONTENTS";
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public MovieContents()
        {
            Id = -1;
            Name = "";
            Label = "";
            Size = 0;
            //FileDate = FileDate;
            Extension = "";
            Rating = 0;
            Comment = "";
        }

        public void ParseMedia()
        {
            string[] files = Directory.GetFiles(Path, @Name + "*");

            string pathname = System.IO.Path.Combine(Path, Name);

            // ListFile, PackageImage, ImageList, MovieList
            ImageList = new List<FileInfo>();
            MovieList = new List<FileInfo>();
            PackageImage = null;
            ThumbnailImage = null;

            Regex reImage = new Regex(REGEX_IMAGE_EXTENTION);
            Regex reMovie = new Regex(REGEX_MOVIE_EXTENTION);

            foreach(string file in files)
            {
                if (reImage.IsMatch(file))
                {
                    FileInfo fileinfo = new FileInfo(file);
                    if (fileinfo.Name.Replace(fileinfo.Extension, "") == Name)
                        PackageImage = fileinfo;
                    else if (fileinfo.Name.Replace(fileinfo.Extension, "") == Name + "_th")
                        ThumbnailImage = fileinfo;
                    else
                        ImageList.Add(new FileInfo(file));
                }

                if (reMovie.IsMatch(file))
                    MovieList.Add(new FileInfo(file));
            }
            if (Directory.Exists(pathname))
            {
                string listFilename = System.IO.Path.Combine(pathname, "list");

                if (File.Exists(listFilename))
                {
                    ListFile = new FileInfo(listFilename);
                }
                files = Directory.GetFiles(pathname, "*");

                foreach (string file in files)
                {
                    if (reImage.IsMatch(file))
                    {
                        if (PackageImage == null)
                            PackageImage = new FileInfo(file);
                        else
                            ImageList.Add(new FileInfo(file));
                    }

                    if (reMovie.IsMatch(file))
                        MovieList.Add(new FileInfo(file));
                }
            }

            this.ImagePosition = 0;
            this.BackImage();

            return;
        }

        public FileInfo ListFile { get; set; }

        public FileInfo PackageImage { get; set; }

        public FileInfo ThumbnailImage { get; set; }

        public List<FileInfo> ImageList { get; set; }

        int ImagePosition = 0;

        public FileInfo[] CurrentImages = null;

        public void BackImage()
        {
            if (ImageList.Count <= 0)
            {
                if (PackageImage != null && ThumbnailImage == null)
                {
                    CurrentImages = new FileInfo[1];
                    CurrentImages[0] = PackageImage;
                }

                return;
            }

            int posi = ImagePosition - 4;

            if (posi < 0)
                posi = 0;

            int size;
            if (posi + 4 < ImageList.Count)
                size = 4;
            else
                size = ImageList.Count - ImagePosition;

            CurrentImages = new FileInfo[size];

            if (size >= 1)
                CurrentImages[0] = ImageList[posi];
            if (size >= 2)
                CurrentImages[1] = ImageList[posi+1];
            if (size >= 3)
                CurrentImages[2] = ImageList[posi+2];
            if (size >= 4)
                CurrentImages[3] = ImageList[posi+3];

            ImagePosition = posi;
        }

        public void NextImage()
        {
            if (ImageList.Count <= 0)
                return;

            int posi = ImagePosition + 4;

            if (posi >= ImageList.Count)
                posi = ImagePosition;

            int size;
            if (posi + 4 < ImageList.Count)
                size = 4;
            else
                size = ImageList.Count - ImagePosition;

            CurrentImages = new FileInfo[size];

            if (size >= 1)
                CurrentImages[0] = ImageList[posi];
            if (size >= 2)
                CurrentImages[1] = ImageList[posi+1];
            if (size >= 3)
                CurrentImages[2] = ImageList[posi+2];
            if (size >= 4)
                CurrentImages[3] = ImageList[posi+3];

            ImagePosition = posi;
        }

        public List<FileInfo> MovieList { get; set; }

        public string Type { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string ChildTableName { get; set; }

        public string DispKind { get; set; }
        private int _Kind;
        public int Kind
        {
            get
            {
                return _Kind;
            }
            set
            {
                _Kind = value;
                if (_Kind == 1)
                    DispKind = "①";
                if (_Kind == 2)
                    DispKind = "②";
                if (_Kind == 3)
                    DispKind = "③";
            }
        }

        public int Id { get; set; }

        public string StoreLabel { get; set; }

        private string _Name;
        public string Name
        {
            get
            {
                return _Name;

            }
            set
            {
                _Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string _Path;
        public string Path
        {
            get
            {
                // フォルダの～の文字コードがmysqlに格納されているUTF-8ではないので、DirectoryNotFoundExceptionが発生する
                return _Path.Replace("〜", "～");
            }
            set
            {
                _Path = value;
            }
        }

        public long Size { get; set; }

        public DateTime FileDate { get; set; }
        public string DispFileDate { get; set; }

        // MovieSiteContentsのみで使用するプロパティ
        public DateTime MovieNewDate { get; set; }

        public DateTime SellDate { get; set; }
        public string DispSellDate { get; set; }

        public void Parse()
        {
            string WorkStr = Regex.Replace(Name.Substring(1), ".* \\[", "");
            string WorkStr2 = Regex.Replace(WorkStr, "\\].*", "");
            WorkStr = Regex.Replace(WorkStr2, " [0-9]*.*", "");
            ProductNumber = WorkStr.ToUpper();

            string DateStr = Regex.Replace(WorkStr2, ".* ", "");

            if (DateStr.Length != 8)
                return;

            string format = "yyyyMMdd"; // "yyyyMMddHHmmss";
            try
            {
                SellDate = DateTime.ParseExact(DateStr, format, null);
            }
            catch (Exception)
            {
                return;
            }
        }

        private int _Rating;
        public int Rating
        {
            get
            {
                return _Rating;
            }
            set
            {
                _Rating = value;
                NotifyPropertyChanged("Rating");
            }
        }

        public string Label { get; set; }

        private string _Comment;
        public string Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                _Comment = value;
                NotifyPropertyChanged("Comment");
            }
        }

        public string Remark { get; set; }

        public string FileStatus { get; set; }

        public string ProductNumber { get; set; }   // KIND_2_MOVIE_SITECONTENTS

        private int _FileCount = 0;
        public int FileCount
        {
            get
            {
                return _FileCount;
            }
            set
            {
                if (value == 1 || value == 0)
                {
                    DispFileCount = "";
                    _FileCount = value;
                }
                else
                {
                    DispFileCount = Convert.ToString(value);
                    _FileCount = value;
                }
                NotifyPropertyChanged("FileCount");
            }
        }
        public string DispFileCount { get; set; }

        public string MovieCount { get; set; }

        public string PhotoCount { get; set; }

        private string _Extension;
        public string Extension
        {
            get
            {
                return _Extension;
            }
            set
            {
                if (value == null)
                    _Extension = value;
                else
                    _Extension = value.ToUpper();
            }
        }

        public string Tag { get; set; }

        private bool _IsExistsThumbnail;

        public bool IsExistsThumbnail
        {
            get
            {
                return _IsExistsThumbnail;
            }
            set
            {
                _IsExistsThumbnail = value;
                ImageUri = GetImageUri(_IsExistsThumbnail);
            }
        }

        private string _ImageUri;

        public string ImageUri
        {
            get
            {
                return _ImageUri;
            }
            set
            {
                _ImageUri = value;
            }
        }

        public string ParentPath { get; set; }

        private string GetImageUri(bool myExistsThumbnail)
        {
            string WorkImageUri = "";

            DirectoryInfo dirinfo = new DirectoryInfo(Environment.CurrentDirectory);

            if (IsExistsThumbnail)        // サムネイル画像あり
                WorkImageUri = System.IO.Path.Combine(dirinfo.FullName, "32.png");
            else
                WorkImageUri = System.IO.Path.Combine(dirinfo.FullName, "00.png");

            return WorkImageUri;
        }

        public void RefrectData(MovieContents myData)
        {
            if (myData == null)
                return;

            if (myData.Name != null && myData.Name.Length > 0 && Name != myData.Name.Trim())
                Name = myData.Name;
            if (myData.Tag != null && myData.Tag.Length > 0 && Tag != myData.Tag.Trim())
                Tag = myData.Tag;
            if (myData.Label != null && myData.Label.Length > 0 && Label != myData.Label.Trim())
                Label = myData.Label;
            if (myData.SellDate.Year != 1900)
                SellDate = myData.SellDate;
            if (myData.FileDate.Year != 1900)
                FileDate = myData.FileDate;
            if (myData.ProductNumber != null && myData.ProductNumber.Length > 0 && ProductNumber != myData.ProductNumber.Trim())
                ProductNumber = myData.ProductNumber;
            if (myData.Extension != null && myData.Extension.Length > 0 && Extension != myData.Extension.ToUpper().Trim())
                Extension = myData.Extension.ToUpper().Trim();

            return;
        }

        public void RefrectFileInfoAndDbUpdate(detail.FileDetail myFileDetail, MySqlDbConnection myDbCon)
        {
            Size = myFileDetail.Size;
            FileDate = myFileDetail.FileDate;
            Extension = myFileDetail.Extension;
            FileCount = myFileDetail.FileCount;

            DbUpdate(myDbCon);

            return;
        }

        public void DbUpdate(MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE contents ";
            sqlCommand += "SET name = @pName ";
            sqlCommand += "  , store_label = @pStoreLabel ";
            sqlCommand += "  , tag = @pTag ";
            sqlCommand += "  , extension = @pExtension ";
            sqlCommand += "  , product_number = @pProductNumber ";
            sqlCommand += "  , publish_date = @pSellDate ";
            sqlCommand += "  , file_date = @pFileDate ";
            sqlCommand += "WHERE ID = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            List<MySqlParameter> listSqlParam = new List<MySqlParameter>();

            MySqlParameter sqlparam = new MySqlParameter("@pName", MySqlDbType.VarChar);
            sqlparam.Value = Name;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pStoreLabel", MySqlDbType.VarChar);
            sqlparam.Value = StoreLabel;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pTag", MySqlDbType.VarChar);
            if (Tag == null || Tag.Length <= 0)
                sqlparam.Value = DBNull.Value;
            else
                sqlparam.Value = Tag;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pExtension", MySqlDbType.VarChar);
            sqlparam.Value = Extension;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pProductNumber", MySqlDbType.VarChar);
            sqlparam.Value = ProductNumber;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pSellDate", MySqlDbType.Date);
            sqlparam.Value = SellDate;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pFileDate", MySqlDbType.Date);
            sqlparam.Value = FileDate;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@pId", MySqlDbType.Int32);
            sqlparam.Value = Id;
            listSqlParam.Add(sqlparam);

            myDbCon.SetParameter(listSqlParam.ToArray());
            myDbCon.execSqlCommand(sqlCommand);

            return;
        }

        public void DbUpdateTag(string myTag, MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE " + GetTableName() + " ";
            sqlCommand += "SET TAG = @pTag ";
            sqlCommand += "WHERE ID = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[2];

            sqlparams[0] = new MySqlParameter("@pTag", SqlDbType.VarChar);
            sqlparams[0].Value = myTag;

            sqlparams[1] = new MySqlParameter("@pId", SqlDbType.Int);
            sqlparams[1].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);

            this.Tag = myTag;
        }

        public void DbUpdateComment(string myComment, MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE contents ";
            sqlCommand += "SET comment = @pComment ";
            sqlCommand += "WHERE id = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[2];

            sqlparams[0] = new MySqlParameter("@pComment", MySqlDbType.VarChar);
            sqlparams[0].Value = myComment;

            sqlparams[1] = new MySqlParameter("@pId", MySqlDbType.Int32);
            sqlparams[1].Value = Id;

            myDbCon.SetParameter(sqlparams);

            int cnt = myDbCon.execSqlCommand(sqlCommand);

            if (cnt <= 0)
                throw new Exception("Comment更新行が0件でした Id [" + Id + "]");

        }

        public void DbUpdateName(string myName, MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE contents ";
            sqlCommand += "SET name = @pName ";
            sqlCommand += "WHERE id = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[2];

            sqlparams[0] = new MySqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = myName;

            sqlparams[1] = new MySqlParameter("@pId", SqlDbType.Int);
            sqlparams[1].Value = Id;

            myDbCon.SetParameter(sqlparams);

            int cnt = myDbCon.execSqlCommand(sqlCommand);

            if (cnt <= 0)
                throw new Exception("Name更新行が0件でした Id [" + Id + "]");

        }

        private string GetTableName()
        {
            if (Kind == MovieContents.KIND_FILE)
                return MovieContents.TABLE_KIND_MOVIE_FILESCONTENTS;
            else if (Kind == MovieContents.KIND_SITE
                || Kind == MovieContents.KIND_SITECHK_UNREGISTERED
                || Kind == MovieContents.KIND_SITECHK_NOTEXIST)
                return MovieContents.TABLE_KIND_MOVIE_SITECONTENTS;
            else
                return MovieContents.TABLE_KIND_MOVIE_CONTENTS;
        }
        public void DbUpdateRating(int myRating, MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE contents ";
            sqlCommand += "SET rating = @pRating ";
            sqlCommand += "WHERE id = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[2];

            sqlparams[0] = new MySqlParameter("@pRating", MySqlDbType.Int32);
            sqlparams[0].Value = myRating;

            sqlparams[1] = new MySqlParameter("@pId", MySqlDbType.Int32);
            sqlparams[1].Value = Id;

            myDbCon.SetParameter(sqlparams);
            int cnt = myDbCon.execSqlCommand(sqlCommand);

            if (cnt <= 0)
                throw new Exception("更新行が0件でした Id [" + Id + "]");

            Rating = myRating;
        }

        public void DbDelete(MySqlDbConnection myDbCon)
        {
            string sqlCommand = "DELETE FROM contents ";
            sqlCommand += "WHERE id = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[1];

            sqlparams[0] = new MySqlParameter("@pId", MySqlDbType.Int32);
            sqlparams[0].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }

        public void DbExportSiteContents(MySqlDbConnection myDbCon)
        {
            string sqlCommand = "INSERT INTO " + GetTableName();
            sqlCommand += "( SITE_NAME, NAME, PARENT_PATH, MOVIE_NEWDATE, MOVIE_COUNT, PHOTO_COUNT, EXTENSION ) ";
            sqlCommand += "VALUES( @pSiteName, @pName, @pParentPath, @pMovieNewDate, @pMovieCount, @pPhotoCount, @pExtension )";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[7];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new MySqlParameter("@pSiteName", MySqlDbType.VarChar);
            sqlparams[0].Value = "";

            sqlparams[1] = new MySqlParameter("@pName", MySqlDbType.VarChar);
            sqlparams[1].Value = Name;

            sqlparams[2] = new MySqlParameter("@pParentPath", MySqlDbType.VarChar);
            sqlparams[2].Value = ParentPath;

            sqlparams[3] = new MySqlParameter("@pMovieNewDate", MySqlDbType.DateTime);
            if (MovieNewDate.Year >= 2000)
                sqlparams[3].Value = MovieNewDate;
            else
                sqlparams[3].Value = Convert.DBNull;

            sqlparams[4] = new MySqlParameter("@pMovieCount", MySqlDbType.VarChar);
            sqlparams[4].Value = MovieCount;

            sqlparams[5] = new MySqlParameter("@pPhotoCount", MySqlDbType.VarChar);
            sqlparams[5].Value = PhotoCount;

            sqlparams[6] = new MySqlParameter("@pExtension", MySqlDbType.VarChar);
            sqlparams[6].Value = Extension;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }
    }
}
