using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMovieManager2Mysql.common
{
    public class Player
    {
        public List<PlayerInfo> listPlayer;

        public Player()
        {
            listPlayer = new List<PlayerInfo>();

            listPlayer.Add(new PlayerInfo("WMP", "wmplayer.exe"));
            listPlayer.Add(new PlayerInfo("GOM", "GOM.exe"));
        }
        public List<PlayerInfo> GetPlayers()
        {
            return listPlayer;
        }
        public void Execute(MovieContents myMovieContents, string myPlayerName, MovieGroup myGroup)
        {
            string executePathname = "";

            // 複数ファイルのためPlayerに対応したリストを作成する
            if (myMovieContents.ExistMovie != null && myMovieContents.ExistMovie.Length > 1)
            {
                string[] arrTargetExt = null;
                arrTargetExt = new string[1];
                arrTargetExt[0] = myMovieContents.Name + "*" + myMovieContents.Extension;

                // プレイリストは一時ディレクトリに書き込むのでパスを取得
                //string tempPath = Path.GetTempPath();

                if (myPlayerName.Equals("GOM"))
                    executePathname = PlayList.MakeAsxFile(myMovieContents.Label, arrTargetExt, Path.GetTempPath(), myMovieContents.Name);
                else if (myPlayerName.Equals("WMP"))
                    executePathname = PlayList.MakeWplFile(myMovieContents.Label, arrTargetExt, Path.GetTempPath(), myMovieContents.Name);
            }
            else if (myMovieContents.ExistMovie != null && myMovieContents.ExistMovie.Length == 1)
            {
                executePathname = myMovieContents.ExistMovie[0];
            }
            else
            {
                if (String.IsNullOrEmpty(myMovieContents.ExistList))
                    return;

                SiteDetail ColViewSiteDetail = new SiteDetail(myMovieContents.Path);

                if (!String.IsNullOrEmpty(myMovieContents.ExistList))
                {
                    List<FileInfo> files = new List<FileInfo>();
                    StreamReader sreader = new StreamReader(myMovieContents.ExistList);
                    string line = sreader.ReadLine();
                    while (line != null)
                    {
                        string movieFilename = Path.Combine(myMovieContents.Path, line);
                        FileInfo fileinfo = new FileInfo(movieFilename);
                        if (fileinfo.Exists)
                            files.Add(fileinfo);

                        line = sreader.ReadLine();
                    }
                    if (myPlayerName.Equals("GOM"))
                        executePathname = PlayList.MakeAsxFile(myMovieContents.Label, files, Path.GetTempPath(), myMovieContents.Name);
                    else if (myPlayerName.Equals("WMP"))
                        executePathname = PlayList.MakeWplFile(myMovieContents.Label, files, Path.GetTempPath(), myMovieContents.Name);
                    Process.Start(@executePathname);
                    return;
                }
                else if (ColViewSiteDetail.ListCount >= 1)
                {
                    List<common.FileContents> list = ColViewSiteDetail.GetList();

                    // Playerリストが存在する場合はPlayerの選択を無視して再生実行
                    if (list.Count >= 1)
                    {
                        executePathname = list[0].FileInfo.FullName;
                        Process.Start(@executePathname);
                        return;
                    }
                }
            }

            var targets = from player in listPlayer
                                  where player.Name.ToUpper() == myPlayerName.ToUpper()
                              select player;

            foreach(PlayerInfo info in targets)
            {
                // 起動するファイル名の前後を""でくくる  例) test.mp4 --> "test.mp4"
                Process.Start(info.ExecuteName, "\"" + @executePathname + "\"");
                break;
            }
        }
    }

    public class PlayerInfo
    {
        public PlayerInfo(string myName, string myExecuteName)
        {
            Name = myName;
            ExecuteName = myExecuteName;
        }
        public string Name;
        public string ExecuteName;
    }

    class PlayList
    {
        public static string MakeWplFile(string myBaseDir, string[] myFilePattern, string myListOutputDir, string myListFilename)
        {
            string WplFilename = System.IO.Path.Combine(myListOutputDir, "_" + myListFilename + ".wpl");

            StreamWriter utf16Writer = new StreamWriter(WplFilename, false, Encoding.GetEncoding("UTF-16"));

            // 先頭部分を出力
            utf16Writer.WriteLine("<?wpl version=\"1.0\"?>");
            utf16Writer.WriteLine("<smil>");
            utf16Writer.WriteLine("\t<body>");
            utf16Writer.WriteLine("\t\t<seq>");

            try
            {
                for (int IndexArr = 0; IndexArr < myFilePattern.Length; IndexArr++)
                {
                    // リスト作成対象の動画ファイル情報を取得
                    string[] filesMovie = System.IO.Directory.GetFiles(myBaseDir, myFilePattern[IndexArr], System.IO.SearchOption.TopDirectoryOnly);

                    // リスト作成対象の動画ファイルが存在しない場合は処理しない
                    if (filesMovie.Length <= 0)
                        continue;

                    // 取得した動画ファイルの出力を行う
                    for (int IndexMovieArr = 0; IndexMovieArr < filesMovie.Length; IndexMovieArr++)
                    {
                        // ファイル名のみを取得
                        FileInfo file = new FileInfo(filesMovie[IndexMovieArr]);

                        // １動画分のリスト生成
                        utf16Writer.WriteLine("\t\t\t<media src=\"" + myBaseDir + "\\" + file.Name + "\" />");
                    }
                }
                utf16Writer.WriteLine("\t\t</seq>");
                utf16Writer.WriteLine("\t</body>");
                utf16Writer.WriteLine("</smil>");
            }
            finally
            {
                utf16Writer.Close();
            }

            return WplFilename;
        }

        public static string MakeWplFile(string myBaseDir, List<FileInfo> myListFiles, string myListOutputDir, string myListFilename)
        {
            string WplFilename = System.IO.Path.Combine(myListOutputDir, "_" + myListFilename + ".wpl");

            StreamWriter utf16Writer = new StreamWriter(WplFilename, false, Encoding.GetEncoding("UTF-16"));

            // 先頭部分を出力
            utf16Writer.WriteLine("<?wpl version=\"1.0\"?>");
            utf16Writer.WriteLine("<smil>");
            utf16Writer.WriteLine("\t<body>");
            utf16Writer.WriteLine("\t\t<seq>");

            try
            {
                foreach (FileInfo file in myListFiles)
                    utf16Writer.WriteLine("\t\t\t<media src=\"" + file.FullName + "\" />");

                utf16Writer.WriteLine("\t\t</seq>");
                utf16Writer.WriteLine("\t</body>");
                utf16Writer.WriteLine("</smil>");
            }
            finally
            {
                utf16Writer.Close();
            }

            return WplFilename;
        }

        public static string MakeAsxFile(string myBaseDir, string[] myFilePattern, string myListOutputDir, string myListFilename)
        {
            string PlayListFilename = System.IO.Path.Combine(myListOutputDir, "_" + myListFilename + ".asx");

            StreamWriter utf16Writer = new StreamWriter(PlayListFilename, false, Encoding.GetEncoding("UTF-16"));

            try
            {
                // 先頭部分を出力
                utf16Writer.WriteLine("<asx version = \"3.0\" >");

                for (int IndexArr = 0; IndexArr < myFilePattern.Length; IndexArr++)
                {
                    // リスト作成対象の動画ファイル情報を取得
                    string[] filesMovie = System.IO.Directory.GetFiles(myBaseDir, myFilePattern[IndexArr], System.IO.SearchOption.TopDirectoryOnly);

                    // リスト作成対象の動画ファイルが存在しない場合は処理しない
                    if (filesMovie.Length <= 0)
                        continue;

                    // 取得した動画ファイルの出力を行う
                    for (int IndexMovieArr = 0; IndexMovieArr < filesMovie.Length; IndexMovieArr++)
                    {
                        // ファイル名のみを取得
                        FileInfo file = new FileInfo(filesMovie[IndexMovieArr]);

                        utf16Writer.WriteLine("<entry>");
                        utf16Writer.WriteLine("<title>" + file.Name + "</title>");

                        // １動画分のリスト生成
                        utf16Writer.WriteLine("\t\t\t<ref href=\"" + file.FullName + "\" />");

                        utf16Writer.WriteLine("</entry>");
                    }
                }

                utf16Writer.WriteLine("</asx>");
            }
            catch (Exception e)
            {
                throw new Exception(PlayListFilename + e.Message);
            }
            finally
            {
                utf16Writer.Close();
            }

            return PlayListFilename;
        }
        public static string MakeAsxFile(string myBaseDir, List<FileInfo> myListFiles, string myListOutputDir, string myListFilename)
        {
            string PlayListFilename = System.IO.Path.Combine(myListOutputDir, "_" + myListFilename + ".asx");

            StreamWriter utf16Writer = new StreamWriter(PlayListFilename, false, Encoding.GetEncoding("UTF-16"));

            try
            {
                // 先頭部分を出力
                utf16Writer.WriteLine("<asx version = \"3.0\" >");

                foreach(FileInfo file in myListFiles)
                {
                    utf16Writer.WriteLine("<entry>");
                    utf16Writer.WriteLine("<title>" + file.Name + "</title>");

                    // １動画分のリスト生成
                    utf16Writer.WriteLine("\t\t\t<ref href=\"" + file.FullName + "\" />");

                    utf16Writer.WriteLine("</entry>");
                }

                utf16Writer.WriteLine("</asx>");
            }
            catch (Exception e)
            {
                throw new Exception(PlayListFilename + e.Message);
            }
            finally
            {
                utf16Writer.Close();
            }

            return PlayListFilename;
        }
    }
}
