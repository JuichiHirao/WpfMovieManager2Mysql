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

namespace wpfMovieManager2Mysql
{
    public class MovieContents : INotifyPropertyChanged
    {
        public const int KIND_FILE = 1;
        public const int KIND_SITE = 2;
        public const int KIND_CONTENTS = 3;
        public const int KIND_SITECHK_UNREGISTERED = 11;
        public const int KIND_SITECHK_NOTEXIST = 12;

        public static string REGEX_MOVIE_EXTENTION = @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*mkv$|.*rm$|.*rmvb$|.*m4v$|.*3gp$";
        //  @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*jpg$|.*jpeg$|.*iso$|.*mkv$";

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
            //Id = Id;
            Name = "";
            SiteName = "";
            Label = "";
            Size = 0;
            //FileDate = FileDate;
            Extension = "";
            Rating = 0;
            Comment = "";
            ChildTableName = "";
            IsExecuteExistPath = false;
        }

        private bool IsExecuteExistPath { get; set; }

        public string[] _ExistMovie;
        public string[] ExistMovie
        {
            get
            {
                if (!IsExecuteExistPath)
                    throw new Exception("GetExistPathの実行後でないとExistMovieは参照できません");

                return _ExistMovie;
            }
            set
            {
                _ExistMovie = value;
            }
        }

        public string GetExistPath(MovieGroup myGroup)
        {
            if (myGroup == null)
                return null;

            IsExecuteExistPath = true;
            if (Kind == KIND_FILE)
            {
                string fileName = "";
                if (Extension == null || Extension.Length <= 0)
                {
                    // ファイル名の中にスラッシュが入っているとDirectoryNotFoundExceptionが発生する
                    string[] tempExistMovie = null;
                    try
                    {
                        tempExistMovie = Directory.GetFiles(Label, @Name + "*");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        return null;
                    }

                    Regex regexMov = new Regex(MovieContents.REGEX_MOVIE_EXTENTION, RegexOptions.IgnoreCase);

                    int cnt = 0;
                    foreach(string file in tempExistMovie)
                    {
                        if (regexMov.IsMatch(file))
                        {
                            ExistMovie = new string[1];
                            ExistMovie[0] = file;

                            return new FileInfo(file).DirectoryName;
                        }
                    }
                }
                else
                    fileName = Path.Combine(Label, Name) + "." + Extension.ToLower();

                if (File.Exists(fileName))
                {
                    ExistMovie = new string[1];
                    ExistMovie[0] = fileName;
                    return new FileInfo(fileName).DirectoryName;
                }
                else
                {
                    string fullName = Name + "_*." + Extension.ToLower();
                    // ファイル名の中にスラッシュが入っているとDirectoryNotFoundExceptionが発生する
                    try
                    {
                        ExistMovie = Directory.GetFiles(Label, @fullName);
                    }
                    catch(DirectoryNotFoundException ex)
                    {
                        return null;
                    }
                    catch(IOException ex)
                    {
                        return null;
                    }

                    if (ExistMovie != null && ExistMovie.Length > 0)
                        return new FileInfo(ExistMovie[0]).DirectoryName;
                }
            }
            else if (Kind == KIND_SITE)
            {
                string path = Path.Combine(myGroup.Explanation, Name);

                if (Directory.Exists(path))
                    return new DirectoryInfo(path).FullName;
            }
            if (Kind == MovieContents.KIND_CONTENTS)
            {
                string path = Path.Combine(myGroup.Explanation, Name);

                if (Directory.Exists(path))
                    return new DirectoryInfo(path).FullName;

                if (File.Exists(path))
                {
                    ExistMovie = new string[1];
                    ExistMovie[0] = path;
                    return new FileInfo(path).DirectoryName;
                }
            }

            return null;
        }
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

        public string DispLabelOrSiteName { get; set; }

        private string _SiteName;
        public string SiteName
        {
            get
            {
                return _SiteName;
            }
            set
            {
                _SiteName = value;
                if (_SiteName != null && _SiteName.Length > 0)
                    DispLabelOrSiteName = _SiteName;
            }
        }

        private string _Label;
        public string Label
        {
            get
            {
                return _Label;
            }
            set
            {
                _Label = value;
                if (_Label != null && _Label.Length > 0)
                {
                    DispLabelOrSiteName = _Label.ToUpper().Replace("\\\\TWELVE-SRV\\","");
                }
            }
        }

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
            int paramCnt = 0;
            int paramMax = 0;
            string sqlCommand = "UPDATE " + GetTableName() + " ";
            sqlCommand += "SET NAME = @pName ";
            sqlCommand += "  , LABEL = @pLabel ";
            sqlCommand += "  , TAG = @pTag ";
            sqlCommand += "  , EXTENSION = @pExtension ";
            if (this.Kind == MovieContents.KIND_FILE)
            {
                sqlCommand += "  , PRODUCT_NUMBER = @pProductNumber ";
                sqlCommand += "  , SELL_DATE = @pSellDate ";
                sqlCommand += "  , FILE_DATE = @pFileDate ";
                paramMax = 8;
            }
            else
                paramMax = 5;

            sqlCommand += "WHERE ID = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[paramMax];

            paramCnt = 0;
            sqlparams[paramCnt] = new MySqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[paramCnt].Value = Name;
            paramCnt++;

            sqlparams[paramCnt] = new MySqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[paramCnt].Value = Label;
            paramCnt++;

            sqlparams[paramCnt] = new MySqlParameter("@pTag", SqlDbType.VarChar);
            if (Tag == null || Tag.Length <= 0)
                sqlparams[paramCnt].Value = DBNull.Value;
            else
                sqlparams[paramCnt].Value = Tag;
            paramCnt++;

            sqlparams[paramCnt] = new MySqlParameter("@pExtension", SqlDbType.VarChar);
            sqlparams[paramCnt].Value = Extension;
            paramCnt++;

            if (this.Kind == MovieContents.KIND_FILE)
            {
                sqlparams[paramCnt] = new MySqlParameter("@pProductNumber", SqlDbType.VarChar);
                sqlparams[paramCnt].Value = ProductNumber;
                paramCnt++;

                sqlparams[paramCnt] = new MySqlParameter("@pSellDate", SqlDbType.Date);
                sqlparams[paramCnt].Value = SellDate;
                paramCnt++;

                sqlparams[paramCnt] = new MySqlParameter("@pFileDate", SqlDbType.Date);
                sqlparams[paramCnt].Value = FileDate;
                paramCnt++;
            }

            sqlparams[paramCnt] = new MySqlParameter("@pId", SqlDbType.Int);
            sqlparams[paramCnt].Value = Id;

            myDbCon.SetParameter(sqlparams);
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
            string sqlCommand = "UPDATE " + GetTableName() + " ";
            sqlCommand += "SET COMMENT = @pComment ";
            sqlCommand += "WHERE ID = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[2];

            sqlparams[0] = new MySqlParameter("@pComment", SqlDbType.VarChar);
            sqlparams[0].Value = myComment;

            sqlparams[1] = new MySqlParameter("@pId", SqlDbType.Int);
            sqlparams[1].Value = Id;

            myDbCon.SetParameter(sqlparams);

            int cnt = myDbCon.execSqlCommand(sqlCommand);

            if (cnt <= 0)
                throw new Exception("Comment更新行が0件でした " + GetTableName() + " Id " + Id);

        }

        public void DbUpdateName(string myName, MySqlDbConnection myDbCon)
        {
            string sqlCommand = "UPDATE " + GetTableName() + " ";
            sqlCommand += "SET NAME = @pName ";
            sqlCommand += "WHERE ID = @pId ";

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
                throw new Exception("Name更新行が0件でした " + GetTableName() + " Id " + Id);

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
            string sqlCommand = "UPDATE " + GetTableName() + " ";
            sqlCommand += "SET RATING = @pRating ";
            sqlCommand += "WHERE ID = @pId ";

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
                throw new Exception("更新行が0件でした " + GetTableName() + " Id " + Id);

            Rating = myRating;
        }

        public void DbDelete(MySqlDbConnection myDbCon)
        {
            string sqlCommand = "DELETE FROM " + GetTableName() + " ";
            sqlCommand += "WHERE ID = @pId ";

            MySqlCommand command = new MySqlCommand();

            command = new MySqlCommand(sqlCommand, myDbCon.getMySqlConnection());

            MySqlParameter[] sqlparams = new MySqlParameter[1];

            sqlparams[0] = new MySqlParameter("@pId", MySqlDbType.Int32);
            sqlparams[0].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }

        public void ParseToSite(MovieGroup myGroup)
        {
            string dir = Path.Combine(myGroup.Explanation, Label);
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
            sqlparams[0].Value = SiteName;

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
