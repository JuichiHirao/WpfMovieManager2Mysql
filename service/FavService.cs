using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NLog;
using WpfMovieManager2.data;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2.service
{
    class FavService
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public List<FavData> GetList(MySqlDbConnection myDbCon)
        {
            List<FavData> listData = new List<FavData>();

            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            string queryString
                        = "SELECT id"
                        + "    , label, name, type, comment "
                        + "    , remark "
                        + "    , created_at, updated_at "
                        + "  FROM fav "
                        + "";

            MySqlDataReader reader = null;
            try
            {
                reader = myDbCon.GetExecuteReader(queryString);

                do
                {

                    if (reader.IsClosed)
                    {
                        //_logger.Debug("reader.IsClosed");
                        throw new Exception("av.storeの取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        FavData data = new FavData();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.Label = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Name = MySqlDbExportCommon.GetDbString(reader, 2);
                        data.Type = MySqlDbExportCommon.GetDbString(reader, 3);
                        data.Comment = MySqlDbExportCommon.GetDbString(reader, 4);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader, 5);
                        data.CreatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 6);
                        data.UpdatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 7);

                        listData.Add(data);
                    }
                } while (reader.NextResult());
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            finally
            {
                reader.Close();
            }

            reader.Close();

            myDbCon.closeConnection();

            return listData;
        }

        public void Delete(FavData myFav, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();

            string querySting = "DELETE FROM fav WHERE ID = @pId ";

            MySqlParameter[] sqlparams = new MySqlParameter[1];

            sqlparams[0] = new MySqlParameter("@pId", MySqlDbType.Int64);
            sqlparams[0].Value = myFav.Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);

            myDbCon.closeConnection();
        }

        public FavData Export(FavData myFav, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();
            string querySting = "INSERT INTO av.fav(label, name, type, comment, remark) VALUES ( @pLabel, @pName, @pType, @pComment, @pRemark ) ";

            List<MySqlParameter> sqlparamList = new List<MySqlParameter>();

            MySqlParameter param = new MySqlParameter();

            param = new MySqlParameter("@pLabel", MySqlDbType.VarChar);
            param.Value = myFav.Label;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName", MySqlDbType.VarChar);
            param.Value = myFav.Name;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pType", MySqlDbType.VarChar);
            param.Value = myFav.Type;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pComment", MySqlDbType.VarChar);
            param.Value = myFav.Comment;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pRemark", MySqlDbType.VarChar);
            param.Value = myFav.Remark;
            sqlparamList.Add(param);

            myDbCon.SetParameter(sqlparamList.ToArray());
            myDbCon.execSqlCommand(querySting);

            string queryString
                        = "SELECT "
                        + "    id, label, name, type, comment, remark, created_at, updated_at "
                        + "  FROM av.fav WHERE ID IN (SELECT MAX(id) FROM fav) "
                        + ""
                        + "";

            MySqlDataReader reader = null;
            FavData data = null;
            try
            {
                reader = myDbCon.GetExecuteReader(queryString);

                do
                {

                    if (reader.IsClosed)
                    {
                        _logger.Debug("reader.IsClosed");
                        throw new Exception("Favの登録後の取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        data = new FavData();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.Label = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Name = MySqlDbExportCommon.GetDbString(reader, 2);
                        data.Type = MySqlDbExportCommon.GetDbString(reader, 3);
                        data.Comment = MySqlDbExportCommon.GetDbString(reader, 4);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader, 5);
                        data.CreatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 6);
                        data.UpdatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 7);
                    }
                } while (reader.NextResult());
            }
            finally
            {
                reader.Close();
            }

            return data;
        }

        public void Update(FavData myFav, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();
            string querySting = "UPDATE fav " +
                "SET label = @pLabel" +
                ", name = @pName " +
                ", type = @pType " +
                ", comment = @pComment " +
                ", remark = @pRemark " +
                "WHERE id = @pId ";

            List<MySqlParameter> sqlparamList = new List<MySqlParameter>();

            MySqlParameter param = new MySqlParameter();
            param = new MySqlParameter("@pLabel", MySqlDbType.VarChar);
            param.Value = myFav.Label;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName", MySqlDbType.VarChar);
            param.Value = myFav.Name;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pType", MySqlDbType.VarChar);
            param.Value = myFav.Type;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pComment", MySqlDbType.VarChar);
            param.Value = myFav.Comment;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pRemark", MySqlDbType.VarChar);
            param.Value = myFav.Remark;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pId", MySqlDbType.VarChar);
            param.Value = myFav.Id;
            sqlparamList.Add(param);

            myDbCon.SetParameter(sqlparamList.ToArray());
            myDbCon.execSqlCommand(querySting);

            return;
        }


    }
}
