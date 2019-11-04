using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMovieManager2.data
{
    public class MovieGroupData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public MovieGroupData()
        {
            Label = "";
            Name1 = "";
            Name2 = "";
            Path = "";
            Remark = "";
        }
        public int Id { get; set; }

        private string _Label;
        public string Label
        {
            get
            {
                return _Label;
            }
            set
            {
                _Label = value;
                NotifyPropertyChanged("Label");
            }
        }
        public string Name1{ get; set; }
        public string Name2 { get; set; }
        public string Type { get; set; }

        private string _Path;
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                _Path = value;
                NotifyPropertyChanged("Path");
            }
        }
        public string Remark { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
