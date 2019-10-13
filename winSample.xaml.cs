using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfMovieManager2Mysql.common;

namespace WpfMovieManager2Mysql
{
    /// <summary>
    /// Interaction logic for winSample.xaml
    /// </summary>
    public partial class winSample : Window
    {
        public winSample()
        {
            InitializeComponent();

            FileInfo file = new FileInfo("C:\\SHARE\\SNIS-515big.jpg");

            abc.Source = ImageMethod.GetImageStream(file.FullName);
        }
        private void stretchNone(object sender, RoutedEventArgs e)
        {
            vb1.Stretch = System.Windows.Media.Stretch.None;
            txt1.Text = "Stretch is now set to None.";
        }

        private void stretchFill(object sender, RoutedEventArgs e)
        {
            vb1.Stretch = System.Windows.Media.Stretch.Fill;
            txt1.Text = "Stretch is now set to Fill.";
        }

        private void stretchUni(object sender, RoutedEventArgs e)
        {
            vb1.Stretch = System.Windows.Media.Stretch.Uniform;
            txt1.Text = "Stretch is now set to Uniform.";
        }

        private void stretchUniFill(object sender, RoutedEventArgs e)
        {
            vb1.Stretch = System.Windows.Media.Stretch.UniformToFill;
            txt1.Text = "Stretch is now set to UniformToFill.";
        }

        private void sdUpOnly(object sender, RoutedEventArgs e)
        {
            vb1.StretchDirection = System.Windows.Controls.StretchDirection.UpOnly;
            txt2.Text = "StretchDirection is now UpOnly.";
        }

        private void sdDownOnly(object sender, RoutedEventArgs e)
        {
            vb1.StretchDirection = System.Windows.Controls.StretchDirection.DownOnly;
            txt2.Text = "StretchDirection is now DownOnly.";
        }

        private void sdBoth(object sender, RoutedEventArgs e)
        {
            vb1.StretchDirection = System.Windows.Controls.StretchDirection.Both;
            txt2.Text = "StretchDirection is now Both.";
        }

    }
}
