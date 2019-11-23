using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2Mysql
{
    class MovieContentsParent
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public double TotalLength = 0;
        public int FileCount = 0;

        public static List<MovieContents> GetDbViewContents(MySqlDbConnection myDbCon)
        {
            List<MovieContents> listMContents = new List<MovieContents>();

            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            string queryString
                        = "SELECT id "
                        + "    , store_label, name, product_number, extension "
                        + "    , tag, publish_date, file_date, file_count "
                        + "    , size, rating, comment, remark "
                        + "    , file_status "
                        + "    , created_at, updated_at, type, path "
                        + "  FROM v_contents "
                        + "    ORDER BY updated_at DESC LIMIT 20000 ";

            MySqlDataReader reader = null;
            try
            {
                reader = myDbCon.GetExecuteReader(queryString);

                do
                {

                    if (reader.IsClosed)
                    {
                        _logger.Debug("av.contents reader.IsClosed");
                        throw new Exception("av.contentsの取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        MovieContents data = new MovieContents();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.StoreLabel = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Name = MySqlDbExportCommon.GetDbString(reader, 2);
                        data.ProductNumber = MySqlDbExportCommon.GetDbString(reader, 3);
                        data.Extension = MySqlDbExportCommon.GetDbString(reader, 4);
                        data.Tag = MySqlDbExportCommon.GetDbString(reader, 5);
                        data.SellDate = MySqlDbExportCommon.GetDbDateTime(reader, 6);
                        data.FileDate = MySqlDbExportCommon.GetDbDateTime(reader, 7);
                        data.FileCount = MySqlDbExportCommon.GetDbInt(reader, 8);
                        data.Size = MySqlDbExportCommon.GetLong(reader, 9);
                        data.Rating = MySqlDbExportCommon.GetDbInt(reader, 10);
                        data.Comment = MySqlDbExportCommon.GetDbString(reader, 11);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader, 12);
                        data.FileStatus = MySqlDbExportCommon.GetDbString(reader, 13);
                        data.CreatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 14);
                        data.UpdatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 15);
                        data.Type = MySqlDbExportCommon.GetDbString(reader, 16);
                        data.Path = MySqlDbExportCommon.GetDbString(reader, 17);

                        listMContents.Add(data);
                    }
                } while (reader.NextResult());
            }
            catch(Exception ex)
            {
                Debug.Write(ex);
            }
            finally
            {
                if (reader != null ) reader.Close();
            }

            myDbCon.closeConnection();

            return listMContents;
        }

        public string GetFileLength()
        {
            double SizeTera = TotalLength / 1024 / 1024 / 1024;
            string SizeStr = String.Format("{0:###,###,###,###}", SizeTera);

            return SizeStr;
        }
    }
}
