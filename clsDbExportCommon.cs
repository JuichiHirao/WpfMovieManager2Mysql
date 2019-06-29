using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;

namespace wpfMovieManager2Mysql
{
    class DbExportCommon
    {
        public static string GetDbString(SqlDataReader myReader, int myColumnNo)
        {
            string myData = "";
            try
            {
                if (!myReader.IsDBNull(myColumnNo))
                    myData = myReader.GetString(myColumnNo);
            }
            catch (Exception)
            {
                myData = "";
            }
            return myData;
        }
        public static int GetDbInt(SqlDataReader myReader, int myColumnNo)
        {
            int myData = 0;
            try
            {
                if (!myReader.IsDBNull(myColumnNo))
                    myData = myReader.GetInt32(myColumnNo);
            }
            catch (Exception)
            {
                myData = 0;
            }
            return myData;
        }
        public static DateTime GetDbDateTime(SqlDataReader myReader, int myColumnNo)
        {
            DateTime myData = new DateTime(1900, 1, 1);
            string data = "";
            string datatype = "";

            try
            {
                if (!myReader.IsDBNull(myColumnNo))
                {
                    datatype = myReader.GetDataTypeName(myColumnNo);

                    if (datatype.Equals("datetime") || datatype.Equals("date"))
                    {
                        myData = myReader.GetDateTime(myColumnNo);
                    }
                    else if (datatype.Equals("varchar"))
                    {
                        data = myReader.GetString(myColumnNo);
                        //Debug.Print("varchar data [" + data + "]");

                        if (data.Length <= 0)
                            myData = new DateTime(1900, 1, 1);
                        else if (data.IndexOf("x") >= 0)
                            Debug.Print("varchar mask data [" + data + "]");
                        else
                            myData = Convert.ToDateTime(data);
                    }
                }
                else
                    myData = new DateTime(1900, 1, 1);
            }
            catch (Exception)
            {
                myData = new DateTime(1900, 1, 1);
            }
            return myData;
        }
        public static long GetLong(SqlDataReader myReader, int myColumnNo)
        {
            Decimal myDecimal = new Decimal();
            long myData = 0;

            try
            {
                myDecimal = myReader.GetDecimal(myColumnNo);
                myData = Convert.ToInt64(myDecimal);
            }
            catch (Exception)
            {
                myData = 0;
            }
            return myData;
        }
    }
}
