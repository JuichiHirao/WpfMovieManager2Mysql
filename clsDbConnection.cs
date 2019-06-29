using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using System.Net;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Xml.Linq;
using System.IO;
using NLog;
using Codeplex.Data;
using Microsoft.CSharp.RuntimeBinder;

namespace wpfMovieManager2Mysql
{
    public class DbConnection
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        string settings;
        private SqlConnection dbcon = null;
        private SqlTransaction dbtrans = null;

        private SqlParameter[] parameters;

        public DbConnection()
        {
            string target = "mssql";
            if (!File.Exists("credential.json"))
            {
                throw new Exception("credential.jsonがDebug or Release配下に存在しません");
            }
            string json = File.ReadAllText("credential.json");
            var obj = DynamicJson.Parse(json);
            string database = "", datasource = "", user = "", password = "";

            try
            {
                var targetObj = obj[target];

                database = targetObj.database;
                datasource = targetObj.datasource;
                user = targetObj.user;
                password = targetObj.password;
            }
            catch (RuntimeBinderException ex)
            {
                throw new Exception("credential.jsonに存在しない[" + target + "]が指定されました or " + ex.Message);
            }

            String connectionInfo = "Data Source=tcp:" + datasource + ";Initial Catalog=" + database + ";Persist Security Info=True;User ID=" + user + ";Password=" + password;

            try
            {
                dbcon = new SqlConnection(connectionInfo);
            }
            catch(Exception ex)
            {
                _logger.Error(ex);
            }
        }
        ~DbConnection()
        {
            try
            {
                if (dbcon != null)
                    dbcon.Close();
            }
            catch (InvalidOperationException)
            {
                // 何もしない 2005/11/28の対応を参照
            }
        }
        public string LoadPlace()
        {
            if (!File.Exists("UserConfig.xml"))
                return "";

            XElement root = XElement.Load("UserConfig.xml");

            string place = root.Value;

            if (place == null)
                place = "";

            return place;
/*
            List<MoneySzeInputData> listSzeInputData = new List<MoneySzeInputData>();

            foreach (XContainer xcon in listAll)
            {
                MoneySzeInputData inputdata = new MoneySzeInputData();

                try
                {
                    inputdata.Date = Convert.ToDateTime(xcon.Element("年月日").Value);
                    inputdata.DebitCode = xcon.Element("借方コード").Value;
                    inputdata.DebitName = xcon.Element("借方名").Value;
                    inputdata.CreditCode = xcon.Element("貸方コード").Value;
                    inputdata.CreditName = xcon.Element("貸方名").Value;
                    inputdata.Amount = Convert.ToInt64(xcon.Element("金額").Value);
                    inputdata.Remark = xcon.Element("摘要").Value;
                }
                catch (NullReferenceException nullex)
                {
                    Debug.Write(nullex);
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listSzeInputData.Add(inputdata);
            }
 */
        }
        public void SavePlace(string myPlace)
        {
            XElement root = new XElement("PLACE");

            root.Add(myPlace);

            root.Save("UserConfig.xml");
        }
        public void openConnection()
        {
            if (dbcon.State != ConnectionState.Open)
                dbcon.Open();
        }
        public void closeConnection()
        {
            if (dbcon.State != ConnectionState.Closed)
                dbcon.Close();
        }
        public SqlConnection getSqlConnection()
        {
            return dbcon;
        }
        public bool isTransaction()
        {
            if (dbtrans == null)
                return false;
            else
                return true;
        }
        public SqlTransaction GetTransaction()
        {
            return dbtrans;
        }
        public void BeginTransaction(string myTransaction)
        {
            if (dbcon.State != ConnectionState.Open)
                dbcon.Open();

            dbtrans = dbcon.BeginTransaction(myTransaction);
        }
        public void RollbackTransaction()
        {
            try
            {
                dbtrans.Rollback();
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }
        }
        public void CommitTransaction()
        {
            try
            {
                dbtrans.Commit();
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }
        }

        /// <summary>
        /// 指定されたＳＱＬ文を実行する
        /// </summary>
        public SqlDataReader GetExecuteReader(string mySqlCommand)
        {
            SqlDataReader reader;
            SqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = mySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getSqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            reader = dbcmd.ExecuteReader();
            parameters = null;

            return reader;
        }

        /// <summary>
        /// 指定されたＳＱＬ文を実行する
        /// </summary>
        public int execSqlCommand(string mySqlCommand)
        {
            SqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = mySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getSqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            int rowCnt = dbcmd.ExecuteNonQuery();

            parameters = null;

            if (dbtrans == null)
                dbcon.Close();

            return rowCnt;
        }
        public void SetParameter(SqlParameter[] myParams)
        {
            if (myParams == null)
                parameters = null;
            else
                parameters = myParams;
        }
        public int getCountSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            int Count = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );
            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }

            myReader = myCommand.ExecuteReader();

            if (myReader.Read())
            {
                if (myReader.IsDBNull(0))
                {
                    parameters = null;
                    myReader.Close();
                    throw new NullReferenceException("SQL ERROR");
                }

                Count = myReader.GetInt32(0);
            }
            else
            {
                parameters = null;
                myReader.Close();
                return -1;
            }

            myReader.Close();
            parameters = null;

            return Count;
        }
        public string getStringSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            string myString = "";

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            myReader.Read();

            myString = myReader.GetSqlString(0).ToString();

            myReader.Close();

            return myString;
        }

        public int getIntSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            int myInteger = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            if (myReader.Read())
                myInteger = myReader.GetInt32(0);
            else
                myInteger = 0;

            myReader.Close();

            return myInteger;
        }
        /// <summary>
        /// 指定されたＳＱＬ文を実行する
        /// </summary>
        public long getSqlCommandRow(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            long lngDataRowCount = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            myReader.Read();

            lngDataRowCount = myReader.GetInt32(0);

            myReader.Close();

            return lngDataRowCount;
        }
        public long getAmountSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            long lngAmount = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            myReader.Read();

            SqlMoney mySqlMoney = myReader.GetSqlMoney(0);
            if (mySqlMoney.IsNull == true)
            {
                myReader.Close();
                return 0;
            }
            lngAmount = mySqlMoney.ToInt64();

            myReader.Close();

            return lngAmount;
        }
    }
}
