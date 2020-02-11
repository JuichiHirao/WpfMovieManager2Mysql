using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMovieManager2.data;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2.service
{
    class StoreService
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public List<MovieGroupData> GetList(MySqlDbConnection myDbCon)
        {
            List<MovieGroupData> listData = new List<MovieGroupData>();

            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            string queryString
                        = "SELECT id"
                        + "    , label, name1, name2, type"
                        + "    , path, remark "
                        + "    , created_at, updated_at "
                        + "  FROM store "
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
                        MovieGroupData data = new MovieGroupData();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader,0);
                        data.Label = MySqlDbExportCommon.GetDbString(reader,1);
                        data.Name1 = MySqlDbExportCommon.GetDbString(reader,2);
                        data.Name2 = MySqlDbExportCommon.GetDbString(reader,3);
                        data.Type = MySqlDbExportCommon.GetDbString(reader,4);
                        data.Path = MySqlDbExportCommon.GetDbString(reader, 5);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader,6);
                        data.CreatedAt = MySqlDbExportCommon.GetDbDateTime(reader,7);
                        data.UpdatedAt = MySqlDbExportCommon.GetDbDateTime(reader,8);

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

        public void Delete(MovieGroupData myData, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();

            string querySting = "DELETE FROM store WHERE ID = @pId ";

            MySqlParameter[] sqlparams = new MySqlParameter[1];

            sqlparams[0] = new MySqlParameter("@pId", MySqlDbType.Int64);
            sqlparams[0].Value = myData.Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(querySting);

            myDbCon.closeConnection();
        }

        public MovieGroupData Export(MovieGroupData myMovieGroup, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();
            string querySting = "INSERT INTO store(label, name1, name2, type, path, remark) VALUES ( @pLabel, @pName1, @pName2, @pType, @pPath, @pRemark ) ";

            List<MySqlParameter> sqlparamList = new List<MySqlParameter>();

            MySqlParameter param = new MySqlParameter();

            param = new MySqlParameter("@pLabel", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Label;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName1", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Name1;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName2", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Name2;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pType", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Type;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pPath", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Path;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pRemark", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Remark;
            sqlparamList.Add(param);

            myDbCon.SetParameter(sqlparamList.ToArray());
            myDbCon.execSqlCommand(querySting);

            string queryString
                        = "SELECT "
                        + "    id, label, name1, name2, type, path, remark, created_at, updated_at "
                        + "  FROM store WHERE ID IN (SELECT MAX(id) FROM store) "
                        + ""
                        + "";

            MySqlDataReader reader = null;
            MovieGroupData data = null;
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
                        data = new MovieGroupData();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.Label = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Name1 = MySqlDbExportCommon.GetDbString(reader, 2);
                        data.Name2 = MySqlDbExportCommon.GetDbString(reader, 3);
                        data.Type = MySqlDbExportCommon.GetDbString(reader, 4);
                        data.Path = MySqlDbExportCommon.GetDbString(reader, 5);
                        data.Remark = MySqlDbExportCommon.GetDbString(reader, 6);
                        data.CreatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 7);
                        data.UpdatedAt = MySqlDbExportCommon.GetDbDateTime(reader, 8);
                    }
                } while (reader.NextResult());
            }
            finally
            {
                reader.Close();
            }

            return data;
        }

        public void UpdateNow(MovieGroupData myMovieGroup, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();
            string querySting = "UPDATE store SET updated_at = now() WHERE id = @pId";

            List<MySqlParameter> sqlparamList = new List<MySqlParameter>();

            MySqlParameter param = new MySqlParameter("@pId", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Id;
            sqlparamList.Add(param);

            myDbCon.SetParameter(sqlparamList.ToArray());
            myDbCon.execSqlCommand(querySting);

            return;
        }

        public void Update(MovieGroupData myMovieGroup, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new MySqlDbConnection();

            myDbCon.openConnection();
            string querySting = "UPDATE store " +
                "SET label = @pLabel" +
                ", name1 = @pName1" +
                ", name2 = @pName2" +
                ", type = @pType " +
                ", path = @pPath" +
                ", remark = @pRemark " +
                "WHERE id = @pId ";

            List<MySqlParameter> sqlparamList = new List<MySqlParameter>();

            MySqlParameter param = new MySqlParameter();
            param = new MySqlParameter("@pLabel", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Label;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName1", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Name1;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pName2", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Name2;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pType", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Type;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pPath", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Path;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pRemark", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Remark;
            sqlparamList.Add(param);

            param = new MySqlParameter("@pId", MySqlDbType.VarChar);
            param.Value = myMovieGroup.Id;
            sqlparamList.Add(param);

            myDbCon.SetParameter(sqlparamList.ToArray());
            myDbCon.execSqlCommand(querySting);

            return;
        }

    }
}
