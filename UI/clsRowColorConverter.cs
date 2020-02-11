using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace WpfMovieManager2Mysql
{
    public class IntCollection : ObservableCollection<int> { }

    class RowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value.ToString();
            //Debug.Print("strValue" + strValue);
            if (strValue.Equals("file"))
                return new LinearGradientBrush(Colors.Cornsilk, Colors.Cornsilk, 45);
            if (strValue.Equals("site"))
                return new LinearGradientBrush(Colors.Honeydew, Colors.Honeydew, 45);
            /*
            if (strValue.Equals("3"))
                return new LinearGradientBrush(Colors.White, Colors.White, 45);
            if (strValue.Equals("11"))
                return new LinearGradientBrush(Colors.OrangeRed, Colors.White, 45);
            if (strValue.Equals("12"))
                return new LinearGradientBrush(Colors.PaleVioletRed, Colors.White, 45);
             */

            return new LinearGradientBrush(Colors.Pink, Colors.White, 45);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class RatingRowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rating = System.Convert.ToInt32(value);
            
            //Debug.Print("strValue" + strValue);
            if (rating == 0)
                return new LinearGradientBrush(Colors.White, Colors.White, 45);
            if (rating == 4 || rating == 3 || rating == 2 || rating == 1)
                return new LinearGradientBrush(Colors.White, Colors.Gainsboro, 45);
            if (rating == 5)
                return new LinearGradientBrush(Colors.White, Colors.Linen, 45);
            if (rating == 6)
                return new LinearGradientBrush(Colors.White, Colors.Honeydew, 45);
            if (rating == 7)
                return new LinearGradientBrush(Colors.White, Colors.PaleGreen, 45);
            if (rating == 8)
                return new LinearGradientBrush(Colors.White, Colors.LawnGreen, 45);
            if (rating == 9)
                return new LinearGradientBrush(Colors.White, Colors.SpringGreen, 45);
            if (rating == 10)
                return new LinearGradientBrush(Colors.White, Colors.Coral, 45);

            return new LinearGradientBrush(Colors.White, Colors.White, 45);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class SiteStoreRowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool BoolValue = System.Convert.ToBoolean(value);

            if (BoolValue)
                return new LinearGradientBrush(Colors.Pink, Colors.Pink, 45);

            return new LinearGradientBrush(Colors.White, Colors.White, 45);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
