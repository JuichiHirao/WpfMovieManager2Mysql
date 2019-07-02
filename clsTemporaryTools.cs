using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using WpfMovieManager2Mysql;
using MySql.Data.MySqlClient;

namespace wpfMovieManager2Mysql
{
    class TemporaryTools
    {
        MySqlDbConnection dbcon = new MySqlDbConnection();

        public void DbExportGroupFromSiteStore()
        {
            /*
            List<MovieSiteStore> listSiteStore = MovieSiteStoreParent.GetDbData(dbcon);

            foreach (MovieSiteStore data in listSiteStore)
            {
                DirectoryInfo dir = new DirectoryInfo(data.Path);

                if (data.Name.Equals("DVDRip"))
                    continue;

                string fullPath = data.Path;
                string childPath = "";
                Regex regex = new Regex(data.Name + "\\\\(?<child_path>.*)", RegexOptions.IgnoreCase);
                if (regex.IsMatch(fullPath))
                {
                    Match match = regex.Match(fullPath);
                    childPath = Convert.ToString(match.Groups["child_path"].Value);
                }

                string namePath = "";
                if (childPath.Length <= 0)
                    namePath = data.Path;
                else
                    namePath = childPath;

                MovieGroup group = new MovieGroup();
                group.Name = namePath;
                group.Kind = 3;
                group.Label = data.Name;
                group.Explanation = data.Path;
                group.DbExport(dbcon);
            }
             */
        }

        public void DbUpdateDir(List<MovieGroup> myListGroup)
        {
            foreach(MovieGroup data in myListGroup)
            {
                if (data.Explanation.IndexOf("DIR情報") >= 0)
                {
                    data.Explanation = data.Explanation.Replace("DIR情報【", "").Replace("】", "");
                    data.DbUpdate(dbcon);
                }
            }
        }

        public void ExportMovieGroupFromMovieFilesTagOnly(List<MovieGroup> myListGroup)
        {
            List<string> listTag = GetOnlyTagList();

            foreach (string data in listTag)
            {
                string[] csvSplit = data.Split(',');

                if (csvSplit.Length > 1)
                {
                    foreach (string field in csvSplit)
                    {
                        var checkdata = from groupInfo in myListGroup
                                        where groupInfo.Name == field.Trim()
                                        select groupInfo;

                        if (checkdata.Count() <= 0)
                        {
                            Debug.Print("TAGのみ " + field);
                            MovieGroup ginfo = new MovieGroup();
                            ginfo.Name = field;
                            ginfo.Explanation = "TAGのみ";
                            ginfo.Kind = 4;

                            MovieGroups.DbExport(ginfo, dbcon);
                        }
                    }
                }
                else
                {
                    var checkdata = from groupInfo in myListGroup
                                    where groupInfo.Name == data.Trim()
                                    select groupInfo;

                    if (checkdata.Count() <= 0)
                    {
                        Debug.Print("TAGのみ " + data);
                        MovieGroup ginfo = new MovieGroup();
                        ginfo.Name = data;
                        ginfo.Explanation = "TAGのみ";
                        ginfo.Kind = 4;

                        MovieGroups.DbExport(ginfo, dbcon);
                    }
                }
            }
        }

        public List<string> GetOnlyTagList()
        {
            string queryString = "SELECT DISTINCT TAG"
                                + "  FROM"
                                + "  ("
                                + "     SELECT TAG "
                                + "       FROM MOVIE_FILES GROUP BY TAG"
                                + "     UNION "
                                + "     SELECT TAG"
                                + "       FROM MOVIE_SITECONTENTS GROUP BY TAG"
                                + "  ) AS TAGLIST "
                                + "  ORDER BY TAG ";

            MySqlCommand command = new MySqlCommand(queryString, dbcon.getMySqlConnection());

            dbcon.openConnection();

            MySqlDataReader reader = command.ExecuteReader();

            List<string> listTag = new List<string>();
            do
            {
                while (reader.Read())
                {
                    string tagName = MySqlDbExportCommon.GetDbString(reader, 0);

                    if (tagName.Length > 0)
                        listTag.Add(tagName);
                }
            } while (reader.NextResult());
            reader.Close();

            dbcon.closeConnection();

            return listTag;
        }

    }
}
