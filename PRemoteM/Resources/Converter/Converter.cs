﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Shawn.Utils;

namespace PRM.Resources.Converter
{
    public class ConverterIsGreaterThan : IValueConverter
    {
        public int CompareValue { get; set; } = 0;
        // Converter={StaticResource ConverterIsGreaterThan},ConverterParameter=50}

        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int v = (int)value;
            return v > CompareValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterIsLowerThan : IValueConverter
    {
        public int CompareValue { get; set; } = 0;
        // Converter={StaticResource ConverterIsGreaterThan},ConverterParameter=50}

        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int v = (int)value;
            return v < CompareValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class DoubleAdd : IValueConverter
    {
        public double AddValue { get; set; } = 1;
        // Converter={StaticResource ConverterIsGreaterThan},ConverterParameter=50}

        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double v = (double)value;
            return v + AddValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterBool2Visible : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool ss = (bool)value;
            return ss ? "Visible" : "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterBool2VisibleInv : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool ss = (bool)value;
            return !ss ? "Visible" : "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterBoolInverse : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    //public class ConverterDouble2Negate : IValueConverter
    //{
    //    #region IValueConverter 成员
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        double ss = (double)value;
    //        return ss * -1;
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //    #endregion
    //}

    public class ConverterDouble2Half : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double ss = (double)value;
            return ss * 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterTextWidthAndContent2PresentationSize : IMultiValueConverter
    {
        private static Size MeasureText(TextBlock tb, int fontSize)
        {
            var formattedText = new FormattedText(tb.Text, CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                fontSize, Brushes.Black); // always uses MaxFontSize for desiredSize
            return new Size(formattedText.Width, formattedText.Height);
        }

        #region IValueConverter 成员

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var tb = new TextBlock
                {
                    Text = values[0].ToString(),
                    Width = int.Parse(values[1].ToString()),
                    FontFamily = (FontFamily)values[2],
                    FontStyle = (FontStyle)values[3],
                    FontWeight = (FontWeight)values[4],
                    FontStretch = (FontStretch)values[5]
                };
                var size = MeasureText(tb, 20);
                double k = 1.0 * tb.Width / size.Width;
                double fs = (int)(20 * k);
                //if (fs > 16)
                //    fs = 16;
                if (fs < 4)
                    fs = 4;
                return fs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 12;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion IValueConverter 成员
    }

    public class ConverterColorHexString2Brush : IValueConverter
    {
        #region IValueConverter

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string hex = value.ToString();
                var brush = ColorAndBrushHelper.ColorToMediaBrush(hex);
                return brush;
            }
            catch (Exception e)
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "#FFFFFF";
        }

        #endregion IValueConverter
    }

    public class ConverterColorHexString2Color : IValueConverter
    {
        #region IValueConverter

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string hex = value.ToString();
                var brush = ColorAndBrushHelper.HexColorToMediaColor(hex);
                return brush;
            }
            catch (Exception e)
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "#FFFFFF";
        }

        #endregion IValueConverter
    }


    public class ConverterIsTheSame : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var _1st = values.FirstOrDefault();
            return values.All(x => x == _1st);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class ConverterEqual2Visible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return value == parameter ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return parameter;
        }
    }
}