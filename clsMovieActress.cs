using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2Mysql
{
    class MovieActresses
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static List<MovieActress> GetDbData(MySqlDbConnection myDbCon)
        {
            List<MovieActress> listMovieActress = new List<MovieActress>();

            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            string queryString
                        = "SELECT "
                        + "    ID, NAME, REMARK, ACTIVITY_DATE "
                        + "  FROM MOVIE_ACTRESS "
                        + "";

            MySqlDataReader reader = null;
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

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.Name = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader, 2);
                        data.ActivityDate = MySqlDbExportCommon.GetDbDateTime(reader, 5);

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
