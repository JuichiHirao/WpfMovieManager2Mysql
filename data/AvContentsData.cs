using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMovieManager2.data
{
    class AvContentsData
    {
        public int Id { get; set; }
        public string StoreLabel { get; set; }
        public string Name { get; set; }
        public string ProductNumber { get; set; }
        public string Extension { get; set; }
        public string Tag { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime FileDate { get; set; }
        public int FileCount { get; set; }
        public long Size { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Remark { get; set; }
        public string FileStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
    }
}
