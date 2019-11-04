using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMovieManager2Mysql.common;
using System.Diagnostics;

namespace WpfMovieManager2Mysql.contents
{
    class TargetList
    {
        public string Pathname = "";
        public List<string> DisplayTargetFiles;

        public TargetList(string myPath)
        {
            Pathname = myPath;
            string listPathname = Path.Combine(Pathname, "list");

            DisplayTargetFiles = new List<string>();

            if (!File.Exists(listPathname))
            {
                return;
            }

            StreamReader sreader = new StreamReader(listPathname);
            string line = sreader.ReadLine();
            while (line != null)
            {
                string movieFilename = Path.Combine(Pathname, line);
                FileInfo fileinfo = new FileInfo(movieFilename);
                if (fileinfo.Exists)
                    DisplayTargetFiles.Add(line);

                line = sreader.ReadLine();
            }
        }

        public TargetList()
        {
            DisplayTargetFiles = new List<string>();
        }

        public void Add(List<FileContents> myList)
        {
            foreach(FileContents data in myList)
            {
                string movieFilename = Path.Combine(Pathname, data.FileInfo.FullName);
                FileInfo fileinfo = new FileInfo(movieFilename);
                if (data.FileInfo.Exists)
                    DisplayTargetFiles.Add(data.DisplayName);
            }
        }

        public void Delete(string  mySelectedItem)
        {
            string movieFilename = Path.Combine(Pathname, mySelectedItem);
            FileInfo fileinfo = new FileInfo(movieFilename);
            if (fileinfo.Exists)
            {
                DisplayTargetFiles.Remove(mySelectedItem);
            }
        }
        public void Up(List<string> mySelectedItems)
        {
            if (mySelectedItems == null || mySelectedItems.Count <= 0)
                return;

            int posi = DisplayTargetFiles.IndexOf(mySelectedItems[0]);

            if (posi > 0)
            {
                int arrIdx = 0;
                DisplayTargetFiles.RemoveRange(posi, mySelectedItems.Count);
                for (int idx=posi-1; arrIdx < mySelectedItems.Count; idx++)
                {
                    DisplayTargetFiles.Insert(idx, mySelectedItems[arrIdx]);
                    arrIdx++;
                }

            }
        }
        public void Down(List<string> mySelectedItems)
        {
            if (mySelectedItems == null || mySelectedItems.Count <= 0)
                return;

            int posi = DisplayTargetFiles.IndexOf(mySelectedItems[0]);

            if (posi < DisplayTargetFiles.Count)
            {
                int arrIdx = 0;
                DisplayTargetFiles.RemoveRange(posi, mySelectedItems.Count);
                for (int idx = posi+1; arrIdx < mySelectedItems.Count; idx++)
                {
                    DisplayTargetFiles.Insert(idx, mySelectedItems[arrIdx]);
                    arrIdx++;
                }

            }
        }

        public void Export()
        {
            string listFilename = System.IO.Path.Combine(Pathname, "list");

            StreamWriter swriter = new StreamWriter(listFilename, false, Encoding.GetEncoding("UTF-8"));

            try
            {
                foreach (string file in DisplayTargetFiles)
                    swriter.WriteLine(file);
            }
            finally
            {
                swriter.Close();
            }
        }
    }
}
