using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wpfMovieManager2Mysql.detail
{
    class FileCopyDetail
    {
        public const int STATUS_REPLACE = 1;
        public const int STATUS_ADD = 2;

        public List<common.FileContents> listFileContents;

        public int Status = 0;
        private int MovieCount = 0;
        private MovieContents TargetMovieContents;

        public FileInfo SourceFile;
        public string NewFilename = "";

        // StatusがREPLACEの場合に設定される各プロパティ
        public common.FileContents ReplaceFile = null;
        public bool IsOverride = false;             // 動画の拡張子が同じ場合は上書きするのでtrueになる

        // StatusがADDの場合に設定
        public bool IsAddSuffix = false;            // 1つ動画存在する状態で追加する場合に既存に_1を付加する

        public FileCopyDetail(detail.FileDetail myFileDetail, MovieContents myMovieContents)
        {
            listFileContents = myFileDetail.listFileInfo;
            MovieCount = myFileDetail.MovieCount;
            TargetMovieContents = myMovieContents;
        }

        public void SetStatus(int myStatus)
        {
            if (myStatus == STATUS_REPLACE)
                Status = STATUS_REPLACE;
            else if (myStatus == STATUS_ADD)
                Status = STATUS_ADD;
            else
                throw new Exception("未知のステータスを設定しようとしました");
        }
        public void SetReplace(common.FileContents myReplaceSourceFile, string myTargetFile)
        {
            if (!File.Exists(myTargetFile))
                throw new Exception("コピー対象のファイル[" + myTargetFile + "]が存在しません");

            if (!File.Exists(myReplaceSourceFile.FileInfo.FullName))
                throw new Exception("置換されるファイル[" + myReplaceSourceFile.FileInfo.Name + "]が存在しません");

            ReplaceFile = myReplaceSourceFile;
            SourceFile = new FileInfo(myTargetFile);

            SetStatus(STATUS_REPLACE);

            if (myReplaceSourceFile.FileInfo.Extension == SourceFile.Extension)
            {
                IsOverride = true;
                NewFilename = myReplaceSourceFile.FileInfo.FullName;
            }
            else
            {
                NewFilename = myReplaceSourceFile.FileInfo.FullName.Replace(myReplaceSourceFile.FileInfo.Extension, SourceFile.Extension);
            }
        }
        public void SetAdd(string myTargetFile)
        {
            if (!File.Exists(myTargetFile))
                throw new Exception("コピー対象のファイル[" + myTargetFile + "]が存在しません");

            SourceFile = new FileInfo(myTargetFile);
            SetStatus(STATUS_ADD);

            if (MovieCount <= 0)
                NewFilename = Path.Combine(TargetMovieContents.Label, TargetMovieContents.Name + SourceFile.Extension);
            else
            {
                int cnt = MovieCount + 1;
                NewFilename = Path.Combine(TargetMovieContents.Label, TargetMovieContents.Name + "_" + cnt + SourceFile.Extension);

                if (MovieCount == 1)
                    IsAddSuffix = true;
            }
        }
        public void DeleteExecute(common.FileContents myDeleteTarget)
        {
            if (MovieCount <= 0)
                throw new Exception("削除できるファイルが存在しません");

            Regex regexMov = new Regex(MovieContents.REGEX_MOVIE_EXTENTION, RegexOptions.IgnoreCase);

            if (!regexMov.IsMatch(myDeleteTarget.FileInfo.Name))
            {
                if (myDeleteTarget.FileInfo.Exists)
                    File.Delete(myDeleteTarget.FileInfo.FullName);

                return;
            }

            if (MovieCount == 2)
            {
                foreach(common.FileContents file in listFileContents)
                {
                    if (regexMov.IsMatch(file.FileInfo.Name))
                    {
                        if (myDeleteTarget.FileInfo.Name != file.FileInfo.Name)
                        {
                            Regex regex = new Regex(".*(_[0-9]{1,})");
                            if (regex.IsMatch(file.DisplayName))
                            {
                                string m = regex.Match(file.DisplayName).Groups[1].Value;
                                string dest = Path.Combine(file.FileInfo.DirectoryName, file.FileInfo.Name.Replace(m + ".", "."));
                                File.Move(file.FileInfo.FullName, dest);
                            }
                        }
                        else
                            File.Delete(myDeleteTarget.FileInfo.FullName);
                    }
                }
            }
            else
                File.Delete(myDeleteTarget.FileInfo.FullName);
        }
        public void Execute(BackgroundWorker myWorker, DoWorkEventArgs myEvent)
        {
            Debug.Print("FileCopyDetail.Execute");
            string backupPathname = "";
            if (IsOverride)
            {
                backupPathname = ReplaceFile.FileInfo.FullName + ".bak";
                File.Move(ReplaceFile.FileInfo.FullName, backupPathname);
            }

            Debug.Print("  backupPathname " + backupPathname);

            string readFile = SourceFile.FullName;
            string writeFile = NewFilename;

            // DBとファイルで不整合がある場合で既存ファイルがある場合は削除
            if (File.Exists(writeFile))
                File.Delete(writeFile);

            Debug.Print("  readFile " + readFile);
            Debug.Print("  writeFile " + writeFile);

            using (FileStream source = new FileStream(readFile, FileMode.Open, FileAccess.Read))
            using (FileStream destination = new FileStream(writeFile, FileMode.CreateNew, FileAccess.Write))
            {
                // ストリームのコピー
                int size = 1048576;
                byte[] buffer = new byte[size];
                int numBytes;

                double srclen = source.Length;
                double wlen = 0;
                while ((numBytes = source.Read(buffer, 0, size)) > 0)
                {
                    if ((myWorker.CancellationPending == true))
                    {
                        myEvent.Cancel = true;
                        break;
                    }
                    destination.Write(buffer, 0, numBytes);
                    wlen = wlen + numBytes;
                    double per = wlen / srclen;
                    int per2 = Convert.ToInt32(per * 100);
                    myWorker.ReportProgress(per2);
                }
            }
            if (myEvent.Cancel)
            {
                File.Delete(writeFile);

                if (IsOverride)
                    File.Move(backupPathname, ReplaceFile.FileInfo.FullName);
            }
            else
            {
                if (IsOverride)
                {
                    Debug.Print("  FileDelete IsOverride " + backupPathname);
                    File.Delete(backupPathname);
                }
                else if (Status == STATUS_REPLACE)
                {
                    Debug.Print("  FileDelete STATUS_REPLACE " + ReplaceFile.FileInfo.FullName);
                    File.Delete(ReplaceFile.FileInfo.FullName);
                }

                if (IsAddSuffix)
                {
                    string src = Path.Combine(TargetMovieContents.Label, TargetMovieContents.Name + SourceFile.Extension);
                    string dest = Path.Combine(TargetMovieContents.Label, TargetMovieContents.Name + "_1" + SourceFile.Extension);
                    File.Move(src, dest);
                }
            }

            Debug.Print("  listFileContents.Count " + listFileContents.Count);
            if (ReplaceFile != null)
                Debug.Print("  ReplaceFile.FullName " + ReplaceFile.FileInfo.FullName);
        }
    }
}
