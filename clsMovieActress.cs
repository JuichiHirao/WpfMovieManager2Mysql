using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMovieManager2Mysql
{
    class MovieActresses
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        /*
        public static void DbExport(MovieGroup myMovieGroup, DbConnection myDbCon)
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
        }
         */
        public static List<MovieActress> GetDbData(DbConnection myDbCon)
        {
            List<MovieActress> listMovieActress = new List<MovieActress>();

            if (myDbCon == null)
                myDbCon = new DbConnection();

            string queryString
                        = "SELECT "
                        + "    ID, NAME, REMARK, ACTIVITY_DATE "
                        + "  FROM MOVIE_ACTRESS "
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
                        MovieActress data = new MovieActress();

                        data.Id = DbExportCommon.GetDbInt(reader, 0);
                        data.Name = DbExportCommon.GetDbString(reader, 1);
                        data.Remark = DbExportCommon.GetDbString(reader, 2);
                        data.ActivityDate = DbExportCommon.GetDbDateTime(reader, 5);

                        listMovieActress.Add(data);
                    }
                } while (reader.NextResult());
            }
            finally
            {
                reader.Close();
            }

            reader.Close();

            myDbCon.closeConnection();

            return listMovieActress;
        }
    }
    class MovieActress
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
