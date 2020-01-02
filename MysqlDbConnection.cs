using Codeplex.Data;
using Microsoft.CSharp.RuntimeBinder;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NLog;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMovieManager2Mysql
{
    public class MySqlDbConnection
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        string settings;
        private MySqlConnection dbcon = null;
        private MySqlTransaction dbtrans = null;

        private MySqlParameter[] parameters;

        public MySqlDbConnection()
        {
            string target = "mysql";

            if (!File.Exists("credential.json"))
            {
                throw new Exception("credential.jsonがDebug or Release配下に存在しません");
            }
            string json = File.ReadAllText("credential.json");
            var obj = DynamicJson.Parse(json);
            string database = "", datasource = "", user = "", password = "", port = "";
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
            try
            {
                var targetObj = obj[target];

                port = targetObj.port;
            }
            catch (RuntimeBinderException ex)
            {
                _logger.Warn("portの設定がされていないので、初期3306を使用します");
                port = "3306";
            }

            String connectionInfo = "Database=" + database + "; Data Source=" + datasource + "; port=" + port + "; User Id=" + user + "; Password=" + password + "; ConnectionTimeout=600; DefaultCommandTimeout=600";
            dbcon = new MySqlConnection(connectionInfo);
        }
        ~MySqlDbConnection()
        {
            try
            {
                dbcon.Close();
            }
            catch (InvalidOperationException)
            {
                // 何もしない 2005/11/28の対応を参照
            }
        }

        public string getDateStringSql(string myMySqlCommand)
        {
            MySqlCommand myCommand;
            MySqlDataReader myReader;

            string resultStr = "";

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
            }
            else
            {
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
                myCommand.Connection = this.getMySqlConnection();
                myCommand.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }
            myReader = myCommand.ExecuteReader();

            if (myReader.Read())
                resultStr = myReader.GetDateTime(0).ToString("yyyy/MM/dd HH:mm:dd");

            myReader.Close();

            return resultStr;
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
        public MySqlConnection getMySqlConnection()
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
        public MySqlTransaction GetTransaction()
        {
            return dbtrans;
        }
        public void BeginTransaction(string myTransaction)
        {
            if (dbcon.State != ConnectionState.Open)
                dbcon.Open();

            dbtrans = dbcon.BeginTransaction();
        }
        public void RollbackTransaction()
        {
            try
            {
                dbtrans.Rollback();
            }
            catch (MySqlException)
            {
                throw;
            }
        }
        public void CommitTransaction()
        {
            try
            {
                dbtrans.Commit();
            }
            catch (MySqlException)
            {
                throw;
            }
        }

        /// <summary>
        /// 指定されたＳＱＬ文を実行する
        /// </summary>
        public MySqlDataReader GetExecuteReader(string myMySqlCommand)
        {
            MySqlCommand dbcmd = new MySqlCommand("set net_write_timeout=60; set net_read_timeout=60", dbcon);

            dbcmd.CommandText = myMySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getMySqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            var reader = dbcmd.ExecuteReader();
            parameters = null;

            return reader;
        }

        /// <summary>
        /// 指定されたMySql文を実行する
        /// </summary>
        public int execMySqlCommand(string myMySqlCommand)
        {
            MySqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = myMySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getMySqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            int count = dbcmd.ExecuteNonQuery();

            parameters = null;

            if (dbtrans == null)
                dbcon.Close();

            return count;
        }
        public void SetParameter(MySqlParameter[] myParams)
        {
            if (myParams == null)
                parameters = null;
            else
                parameters = myParams;
        }
        /// <summary>
        /// 指定されたSQL文を実行する
        /// </summary>
        public int execSqlCommand(string mySqlCommand)
        {
            MySqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = mySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getMySqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            int count = dbcmd.ExecuteNonQuery();

            parameters = null;

            if (dbtrans == null)
                dbcon.Close();

            return count;
        }
        public int getCountSql(string myMySqlCommand)
        {
            MySqlCommand myCommand;
            MySqlDataReader myReader;

            int Count = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
            }
            else
            {
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
                myCommand.Connection = this.getMySqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new MySqlCommand( myMySqlCommand, dbcon );
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
                    throw new NullReferenceException("MySql ERROR");
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
        public string getStringSql(string myMySqlCommand)
        {
            MySqlCommand myCommand;
            MySqlDataReader myReader;

            string myString = "";

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
            }
            else
            {
                myCommand = new MySqlCommand(myMySqlCommand, this.getMySqlConnection());
                myCommand.Connection = this.getMySqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new MySqlCommand( myMySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            myReader.Read();

            myString = MySqlDbExportCommon.GetDbString(myReader, 0);

            myReader.Close();

            return myString;
        }

        public int getIntSql(string mySqlCommand)
        {
            MySqlCommand myCommand;
            MySqlDataReader myReader;

            int myInteger = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new MySqlCommand(mySqlCommand, this.getMySqlConnection());
            }
            else
            {
                myCommand = new MySqlCommand(mySqlCommand, this.getMySqlConnection());
                myCommand.Connection = this.getMySqlConnection();
                myCommand.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }
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
            MySqlCommand myCommand;
            MySqlDataReader myReader;

            long lngDataRowCount = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new MySqlCommand(mySqlCommand, this.getMySqlConnection());
            }
            else
            {
                myCommand = new MySqlCommand(mySqlCommand, this.getMySqlConnection());
                myCommand.Connection = this.getMySqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new MySqlCommand( myMySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            myReader.Read();

            lngDataRowCount = myReader.GetInt32(0);

            myReader.Close();

            return lngDataRowCount;
        }
    }
}
