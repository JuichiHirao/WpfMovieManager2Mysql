using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMovieManager2.data
{
    class FavData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public FavData()
        {
            Label = "";
            Name = "";
            Comment = "";
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

        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Type { get; set; }

        private string _Comment;
        public string Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                _Comment = value;
                NotifyPropertyChanged("Comment");
            }
        }
        public string Remark { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
