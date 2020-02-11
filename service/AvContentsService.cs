using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMovieManager2.common;
using WpfMovieManager2.data;
using WpfMovieManager2Mysql;

namespace WpfMovieManager2.service
{
    class AvContentsService
    {
        public string[] GetFavoriteActresses(string myActress, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                // string myDatabase, string myDataSource, string myPort, string myUser, string myPassword
                myDbCon = new MySqlDbConnection(0);

            List<string> actressList = new List<string>();
            string queryString = "SELECT label, name FROM av.fav WHERE label = @StoreLabel or name like @LikeName ";

            string labels = "";

            List<MySqlParameter> listSqlParam = new List<MySqlParameter>();
            MySqlDataReader reader = null;

            MySqlParameter sqlparam = new MySqlParameter("@StoreLabel", MySqlDbType.VarChar);
            sqlparam.Value = myActress;
            listSqlParam.Add(sqlparam);

            sqlparam = new MySqlParameter("@LikeName", MySqlDbType.VarChar);
            sqlparam.Value = "%" + myActress + "%";
            listSqlParam.Add(sqlparam);

            myDbCon.SetParameter(listSqlParam.ToArray());
            reader = myDbCon.GetExecuteReader(queryString);

            string label = "", name = "";
            do
            {
                if (reader.IsClosed)
                {
                    //_logger.Debug("av.contents reader.IsClosed");
                    throw new Exception("av.contentsの取得でreaderがクローズされています");
                }

                while (reader.Read())
                {
                    label = MySqlDbExportCommon.GetDbString(reader, 0);
                    name = MySqlDbExportCommon.GetDbString(reader, 1);
                    if (label.IndexOf(myActress) >= 0)
                        actressList.AddRange(Actress.AppendMatch(label, actressList));

                    if (name.IndexOf(myActress) >= 0)
                        actressList.AddRange(Actress.AppendMatch(name, actressList));
                }

            } while (reader.NextResult());

            //Debug.Print("totalsize " + total);

            myDbCon.closeConnection();

            return actressList.ToArray();
        }

        public long GetStoreLabelTotalSize(string myStoreLabel, MySqlDbConnection myDbCon)
        {
            if (myDbCon == null)
                // string myDatabase, string myDataSource, string myPort, string myUser, string myPassword
                myDbCon = new MySqlDbConnection(0);

            string queryString = "SELECT sum(size) FROM av.v_contents WHERE store_label = @StoreLabel ";

            long total = 0;
            try
            {
                List<MySqlParameter> listSqlParam = new List<MySqlParameter>();

                MySqlParameter sqlparam = new MySqlParameter("@StoreLabel", MySqlDbType.VarChar);
                sqlparam.Value = myStoreLabel;
                listSqlParam.Add(sqlparam);

                myDbCon.SetParameter(listSqlParam.ToArray());
                total = myDbCon.getLongSql(queryString);
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            //Debug.Print("totalsize " + total);

            myDbCon.closeConnection();

            return total;
        }

        public List<AvContentsData> GetActressList(string myTag, MySqlDbConnection myDbCon)
        {
            List<AvContentsData> avContentsList = new List<AvContentsData>();
            string queryString = "SELECT id, tag, rating FROM av.contents WHERE tag like @Tag ";

            MySqlDataReader reader = null;
            string[] arrTag = myTag.Split(',');

            try
            {
                foreach (string tag in arrTag)
                {
                    List<MySqlParameter> listSqlParam = new List<MySqlParameter>();

                    MySqlParameter sqlparam = new MySqlParameter("@Tag", MySqlDbType.VarChar);
                    sqlparam.Value = String.Format("{0}", tag);
                    listSqlParam.Add(sqlparam);
                    myDbCon.SetParameter(listSqlParam.ToArray());

                    reader = myDbCon.GetExecuteReader(queryString);

                    do
                    {
                        myDbCon.SetParameter(listSqlParam.ToArray());

                        if (reader.IsClosed)
                        {
                            //_logger.Debug("av.contents reader.IsClosed");
                            throw new Exception("av.contentsの取得でreaderがクローズされています");
                        }

                        while (reader.Read())
                        {
                            AvContentsData data = new AvContentsData();

                            data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                            data.Tag = MySqlDbExportCommon.GetDbString(reader, 1);
                            data.Rating = MySqlDbExportCommon.GetDbInt(reader, 2);

                            avContentsList.Add(data);
                        }
                    } while (reader.NextResult());
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            myDbCon.closeConnection();

            return avContentsList;
        }

        public List<AvContentsData> GetActressLikeFilenameList(string myActress, MySqlDbConnection myDbCon, List<AvContentsData> myExistList)
        {
            List<AvContentsData> avContentsList = new List<AvContentsData>();
            string queryString = "SELECT id, tag, rating FROM av.v_contents WHERE name like @Tag ";

            MySqlDataReader reader = null;
            try
            {
                List<MySqlParameter> listSqlParam = new List<MySqlParameter>();

                MySqlParameter sqlparam = new MySqlParameter("@Tag", MySqlDbType.VarChar);
                sqlparam.Value = String.Format("{0}", myActress);
                listSqlParam.Add(sqlparam);
                myDbCon.SetParameter(listSqlParam.ToArray());

                reader = myDbCon.GetExecuteReader(queryString);

                do
                {
                    myDbCon.SetParameter(listSqlParam.ToArray());

                    if (reader.IsClosed)
                    {
                        //_logger.Debug("av.contents reader.IsClosed");
                        throw new Exception("av.contentsの取得でreaderがクローズされています");
                    }

                    while (reader.Read())
                    {
                        AvContentsData data = new AvContentsData();

                        data.Id = MySqlDbExportCommon.GetDbInt(reader, 0);
                        data.Tag = MySqlDbExportCommon.GetDbString(reader, 1);
                        data.Rating = MySqlDbExportCommon.GetDbInt(reader, 2);

                        if (myExistList == null)
                            avContentsList.Add(data);
                        else if (!myExistList.Exists(x => x.Id == data.Id))
                            avContentsList.Add(data);
                    }
                } while (reader.NextResult());
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            myDbCon.closeConnection();

            return avContentsList;
        }
    }
}
