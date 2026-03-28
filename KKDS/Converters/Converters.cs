using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace KKDS.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class RiskSeviyeRenkConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is int skor)
            {
                if (skor >= 70) return new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
                if (skor >= 40) return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            }
            if (value is string s)
            {
                return s.ToLower() switch
                {
                    "yüksek" or "kritik" => new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45)),
                    "orta" => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
                    _ => new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45))
                };
            }
            return Brushes.Gray;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class EgilimRenkConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is not string s) return Brushes.Gray;
            return s.ToLower() switch
            {
                "artıyor" or "hızlı kötüleşme" => new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45)),
                "azalıyor" or "iyileşme" => new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)),
                _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class EgilimSimgeConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is not string s) return "—";
            return s.ToLower() switch
            {
                "artıyor" or "hızlı kötüleşme" => "▲",
                "azalıyor" or "iyileşme" => "▼",
                _ => "—"
            };
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    public class BarHeightConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is int count)
            {
                double mul = 8.0;
                if (p is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double m))
                    mul = m;
                return Math.Max(4, count * mul);
            }
            return 4.0;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }
}
