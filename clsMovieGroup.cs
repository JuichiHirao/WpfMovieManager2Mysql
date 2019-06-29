using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Data;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

namespace wpfMovieManager2Mysql
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
            var lhs = (MovieGroup)a;
            var rhs = (MovieGroup)b; //APPLY ALGORITHM LOGIC HERE
            return SafeNativeMethods.StrCmpLogicalW(lhs.Explanation, rhs.Explanation);
        }
    }

    public class GroupFilesInfo
    {
        public int FileCount { get; set; }
        public long Size { get; set; }
        public int Unrated { get; set; }
    }

    class MovieGroupFilterAndSorts
    {
        public const int EXECUTE_MODE_NORMAL = 0;
        public const int EXECUTE_MODE_NATURAL_COMPARE = 1;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public void DbExport()
        {
            TemporaryTools tools = new TemporaryTools();
            tools.DbUpdateDir(listMovieGroup);
        }

        DbConnection dbcon;
        List<MovieGroup> listMovieGroup;
        //public ICollectionView ColViewListMovieGroup;
        public ListCollectionView ColViewListMovieGroup;

        public List<string> listSiteName;

        public int FilterKind = 0;
        public string FilterLabel = "";

        string sortItem;
        ListSortDirection sortOrder;

        string FilterSearchText = "";

        public MovieGroupFilterAndSorts(DbConnection myDbCon)
        {
            dbcon = myDbCon;

            DataSet();
        }

        public MovieGroupFilterAndSorts()
        {
            dbcon = new DbConnection();

            DataSet();
        }

        public MovieGroup GetMatchDataByContents(MovieContents myContents)
        {
            bool IsSite = (myContents.Kind == MovieContents.KIND_SITE);
            bool IsFile = (myContents.Kind == MovieContents.KIND_FILE);
            //bool IsContentsSite = (myContents.Kind == MovieContents.KIND_CONTENTS && myContents.SiteName != null && myContents.SiteName.Length > 0);
            //bool IsContentsFile = (myContents.Kind == MovieContents.KIND_CONTENTS && (myContents.SiteName == null || myContents.SiteName.Length <= 0));

            Regex regex = null;
            if (IsSite)
            {
                regex = new Regex(".*" + Regex.Escape(myContents.Label) + "$", RegexOptions.IgnoreCase);
            }

            foreach (MovieGroup data in listMovieGroup)
            {
                if (IsSite)// || IsContentsSite)
                {
                    if (data.Kind != MovieGroup.KIND_SITE)
                        continue;
                }
                else if (IsFile)
                {
                    if (data.Kind != MovieGroup.KIND_DIR)
                        continue;
                }

                if (IsSite)
                {
                    if (data.Label.ToUpper() == myContents.SiteName.ToUpper() && regex.IsMatch(data.Explanation))
                        return data;

                    continue;
                }
                else if (IsFile)
                {
                    if (data.Explanation.Length > 0 && data.Explanation.ToUpper() == myContents.Label.ToUpper())
                        return data;

                    continue;
                }
                else //if (IsContentsSite)
                {
                    if (data.Label.ToUpper() == myContents.SiteName.ToUpper())
                    {
                        if (Directory.Exists(Path.Combine(data.Explanation, myContents.Name)))
                            return data;
                    }

                    if (File.Exists(Path.Combine(data.Explanation, myContents.Name)))
                        return data;

                    continue;
                }
            }

            return null;
        }

        private void DataSet()
        {
            listMovieGroup = MovieGroups.GetDbData(dbcon);

            ColViewListMovieGroup = (ListCollectionView)CollectionViewSource.GetDefaultView(listMovieGroup);

            ColViewListMovieGroup.SortDescriptions.Clear();
            ColViewListMovieGroup.SortDescriptions.Add(new SortDescription("UpdateDate", ListSortDirection.Descending));

            listSiteName = GetDistinctSiteName();
        }
        public void Add(MovieGroup myData)
        {
            listMovieGroup.Add(myData);
        }
        public void DbDelete(MovieGroup myData, DbConnection myDbCon)
        {
            ColViewListMovieGroup.Remove(myData);
            MovieGroups.DbDelete(myData, myDbCon);
        }
        public void Clear()
        {
            FilterLabel = "";
            FilterKind = 0;
            ColViewListMovieGroup.SortDescriptions.Clear();
        }
        private List<string> GetDistinctSiteName()
        {
            var names = (from g in listMovieGroup
                            orderby g.Label
                         where g.Kind == 3
                            select g.Label).Distinct();

            List<string> siteNames = new List<string>();

            foreach(string name in names)
            {
                siteNames.Add(name);
            }

            return siteNames;
        }

        public void SetSearchText(string myText)
        {
            FilterSearchText = myText;
        }

        public void SetSort(string mySortItem, ListSortDirection mySortOrder)
        {
            if (mySortItem == null)
            {
                sortItem = "CreateDate";
                sortOrder = ListSortDirection.Descending;
                return;
            }
            sortItem = mySortItem;
            sortOrder = mySortOrder;
        }
        public void SetFilterKind(int myKind)
        {
            // 0は全て対象
            FilterKind = myKind;
        }
        public void SetSiteName(string mySiteName)
        {
            if (mySiteName != null && mySiteName.Length > 0)
            {
                FilterLabel = mySiteName;
                FilterKind = 3;
            }
            else
                FilterLabel = mySiteName;
        }

        public void Refresh()
        {
            ColViewListMovieGroup.Refresh();
        }

        public void Execute(int mode)
        {
            int TargetMatchCount = 0;
            if (FilterSearchText.Length > 0)
                TargetMatchCount++;
            if (FilterLabel.Length > 0)
                TargetMatchCount++;
            if (FilterKind > 0)
                TargetMatchCount++;

            ColViewListMovieGroup.Filter = delegate (object o)
            {
                MovieGroup data = o as MovieGroup;
                int MatchCount = 0;

                if (FilterSearchText.Length > 0)
                {
                    if (data.Name.ToUpper().IndexOf(FilterSearchText.ToUpper()) >= 0)
                        MatchCount++;
                }
                if (FilterLabel.Length > 0)
                {
                    if (FilterLabel == data.Label)
                        MatchCount++;
                }

                if (FilterKind > 0 && data.Kind == FilterKind)
                    MatchCount++;

                if (TargetMatchCount <= MatchCount)
                    return true;
                else
                    return false;
            };

            if (ColViewListMovieGroup != null && ColViewListMovieGroup.CanSort == true)
            {
                if (sortItem != null && sortItem.Length > 0)
                {
                    if (mode == EXECUTE_MODE_NATURAL_COMPARE)
                    {
                        ColViewListMovieGroup.SortDescriptions.Clear();
                        ColViewListMovieGroup.CustomSort = new NaturalStringComparer();
                    }
                    else
                    {
                        ColViewListMovieGroup.SortDescriptions.Clear();
                        ColViewListMovieGroup.SortDescriptions.Add(new SortDescription(sortItem, sortOrder));
                    }
                }
            }
        }
    }

    class MovieGroups
    {
        public static Dictionary<string, int> KindByButton = new Dictionary<string, int>()
        {
            { "D", 1 }, // Directory
            { "",  2 }, // ???
            { "S", 3 }, // Site
            { "A", 4 }  // Actress
        };

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static MovieGroup DbExport(MovieGroup myMovieGroup, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            myDbCon.openConnection();
            string querySting = "INSERT INTO MOVIE_GROUP( NAME, LABEL, EXPLANATION, KIND ) VALUES ( @pName, @pLabel, @pExplanation, @pKind ) ";

            SqlParameter[] sqlparams = new SqlParameter[4];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = myMovieGroup.Name;

            sqlparams[1] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[1].Value = myMovieGroup.Label;

            sqlparams[2] = new SqlParameter("@pExplanation", SqlDbType.VarChar);
            sqlparams[2].Value = myMovieGroup.Explanation;

            sqlparams[3] = new SqlParameter("@pKind", SqlDbType.Int);
            sqlparams[3].Value = myMovieGroup.Kind;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);

            string queryString
                        = "SELECT "
                        + "    ID, NAME, LABEL, EXPLANATION, KIND, CREATE_DATE, UPDATE_DATE "
                        + "  FROM MOVIE_GROUP WHERE ID IN (SELECT MAX(ID) FROM MOVIE_GROUP) "
                        + ""
                        + "";

            SqlDataReader reader = null;
            MovieGroup data = null;
            try
            {
                reader = myDbCon.GetExecuteReader(queryString);

                do
                {

                    if (reader.IsClosed)
                    {
                        _logger.Debug("reader.IsClosed");
                        throw new Exception("MOVIE_SITESTOREの取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        data = new MovieGroup();

                        data.Id = DbExportCommon.GetDbInt(reader, 0);
                        data.Name = DbExportCommon.GetDbString(reader, 1);
                        data.Label = DbExportCommon.GetDbString(reader, 2);
                        data.Explanation = DbExportCommon.GetDbString(reader, 3);
                        data.Kind = DbExportCommon.GetDbInt(reader, 4);
                        data.CreateDate = DbExportCommon.GetDbDateTime(reader, 5);
                        data.UpdateDate = DbExportCommon.GetDbDateTime(reader, 6);
                    }
                } while (reader.NextResult());
            }
            finally
            {
                reader.Close();
            }

            return data;
        }
        public static List<MovieGroup> GetDbData(DbConnection myDbCon)
        {
            List<MovieGroup> listMovieGroup = new List<MovieGroup>();

            if (myDbCon == null)
                myDbCon = new DbConnection();

            string queryString
                        = "SELECT "
                        + "    ID, NAME, LABEL, EXPLANATION, KIND, CREATE_DATE, UPDATE_DATE "
                        + "  FROM MOVIE_GROUP "
                        + ""
                        + "";

            SqlDataReader reader = null;
            try
            {
                reader = myDbCon.GetExecuteReader(queryString);

                do
                {

                    if (reader.IsClosed)
                    {
                        _logger.Debug("reader.IsClosed");
                        throw new Exception("MOVIE_SITESTOREの取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        MovieGroup data = new MovieGroup();

                        data.Id = DbExportCommon.GetDbInt(reader, 0);
                        data.Name = DbExportCommon.GetDbString(reader, 1);
                        data.Label = DbExportCommon.GetDbString(reader, 2);
                        data.Explanation = DbExportCommon.GetDbString(reader, 3);
                        data.Kind = DbExportCommon.GetDbInt(reader, 4);
                        data.CreateDate = DbExportCommon.GetDbDateTime(reader, 5);
                        data.UpdateDate = DbExportCommon.GetDbDateTime(reader, 6);

                        listMovieGroup.Add(data);
                    }
                } while (reader.NextResult());
            }
            finally
            {
                reader.Close();
            }

            reader.Close();

            myDbCon.closeConnection();

            return listMovieGroup;
        }

        public static void DbDelete(MovieGroup myData, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            myDbCon.openConnection();

            string querySting = "DELETE FROM MOVIE_GROUP WHERE ID = @pId ";

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[0].Value = myData.Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);

            myDbCon.closeConnection();
        }

    }

    public class MovieGroup
    {
        public const int KIND_DIR = 1;
        public const int KIND_SITE = 3;
        public const int KIND_ACTRESS = 4;

        public MovieGroup()
        {
            Name = "";
            Label = "";
            Explanation = "";
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Explanation {get; set; }
        public int Kind { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        public void DbExport(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            myDbCon.openConnection();

            string querySting = "INSERT INTO MOVIE_GROUP (NAME, LABEL, EXPLANATION, KIND) VALUES( @pName, @pLabel, @pExplanation, @pKind)";

            SqlParameter[] sqlparams = new SqlParameter[4];

            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = Name;

            sqlparams[1] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[1].Value = Label;

            sqlparams[2] = new SqlParameter("@pExplanation", SqlDbType.VarChar);
            sqlparams[2].Value = Explanation;

            sqlparams[3] = new SqlParameter("@pKind", SqlDbType.Int);
            sqlparams[3].Value = Kind;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);
        }
        public void DbUpdate(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            myDbCon.openConnection();

            string querySting = "UPDATE MOVIE_GROUP SET NAME = @pName, LABEL = @pLabel, EXPLANATION = @pExplanation, KIND = @pKind WHERE ID = @pId ";
            //                  + " NAME ", LABEL, EXPLANATION, KIND ) VALUES ( @pName, @pLabel, @pExplanation, @pKind ) ";

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = Name;

            sqlparams[1] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[1].Value = Label;

            sqlparams[2] = new SqlParameter("@pExplanation", SqlDbType.VarChar);
            sqlparams[2].Value = Explanation;

            sqlparams[3] = new SqlParameter("@pKind", SqlDbType.Int);
            sqlparams[3].Value = Kind;

            sqlparams[4] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[4].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);
        }
    }
}
