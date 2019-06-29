using NLog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace wpfMovieManager2Mysql
{
    class MovieContentsParent
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public double TotalLength = 0;
        public int FileCount = 0;

        public static List<MovieContents> GetDbViewContents(DbConnection myDbCon)
        {
            List<MovieContents> listMContents = new List<MovieContents>();

            if (myDbCon == null)
                myDbCon = new DbConnection();

            string queryString
                        = "SELECT KIND "
                        + "    , ID, NAME, SIZE "
                        + "    , FILE_DATE, MOVIE_NEWDATE, SELL_DATE "
                        + "    , RATING, LABEL, COMMENT, REMARK "
                        + "    , SITE_NAME "
                        + "    , PRODUCT_NUMBER "
                        + "    , FILE_COUNT, MOVIE_COUNT, PHOTO_COUNT "
                        + "    , EXTENSION, CREATE_DATE, UPDATE_DATE "
                        + "    , TAG "
                        + "  FROM V_MOVIE_CONTENTS "
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
                        _logger.Debug("V_MOVIE_CONTENTS reader.IsClosed");
                        throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        MovieContents data = new MovieContents();

                        data.Kind = DbExportCommon.GetDbInt(reader, 0);
                        data.Id = DbExportCommon.GetDbInt(reader, 1);
                        data.Name = DbExportCommon.GetDbString(reader, 2);
                        data.Size = DbExportCommon.GetLong(reader, 3);
                        data.FileDate = DbExportCommon.GetDbDateTime(reader, 4);
                        data.MovieNewDate = DbExportCommon.GetDbDateTime(reader, 5);
                        data.SellDate = DbExportCommon.GetDbDateTime(reader, 6);
                        data.Rating = DbExportCommon.GetDbInt(reader, 7);
                        data.Label = DbExportCommon.GetDbString(reader, 8);
                        data.Comment = DbExportCommon.GetDbString(reader, 9);
                        data.Remark = DbExportCommon.GetDbString(reader, 10);
                        data.SiteName = DbExportCommon.GetDbString(reader, 11);
                        data.ProductNumber = DbExportCommon.GetDbString(reader, 12);
                        data.FileCount = DbExportCommon.GetDbInt(reader, 13);
                        data.MovieCount = DbExportCommon.GetDbString(reader, 14);
                        data.PhotoCount = DbExportCommon.GetDbString(reader, 15);
                        data.Extension = DbExportCommon.GetDbString(reader, 16);
                        data.CreateDate = DbExportCommon.GetDbDateTime(reader, 17);
                        data.UpdateDate = DbExportCommon.GetDbDateTime(reader, 18);
                        data.Tag = DbExportCommon.GetDbString(reader, 19);
                        //data.ChildTableName = MovieContents.TABLE_KIND_MOVIE_FILESCONTENTS;

                        listMContents.Add(data);
                    }
                } while (reader.NextResult());
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
