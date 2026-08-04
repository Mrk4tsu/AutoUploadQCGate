using System;
using System.Globalization;
using AutoUploadQCGate.Models;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoUploadQCGate
{
    // Converter cho Judgement style (Pass/Fail background)
    public class JudgementToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var judgement = value as string;
            if (string.Equals(judgement, "PASS", StringComparison.OrdinalIgnoreCase))
                return Application.Current.FindResource("JudgementPass");
            if (string.Equals(judgement, "FAIL", StringComparison.OrdinalIgnoreCase))
                return Application.Current.FindResource("JudgementFail");

            return Application.Current.FindResource("JudgementEmpty");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter cho Judgement text color
    public class JudgementToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var judgement = value as string;
            if (string.Equals(judgement, "PASS", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(22, 131, 63));
            if (string.Equals(judgement, "FAIL", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(198, 40, 40));

            return new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter cho Status style
    public class StatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = UploadStatusNames.Normalize(value as string);
            switch (status)
            {
                case UploadStatusNames.Success:
                    return Application.Current.FindResource("StatusSuccess");
                case UploadStatusNames.Failed:
                    return Application.Current.FindResource("StatusError");
                case UploadStatusNames.Processing:
                    return Application.Current.FindResource("StatusProcessing");
                default:
                    return Application.Current.FindResource("StatusPending");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = UploadStatusNames.Normalize(value as string);
            switch (status)
            {
                case UploadStatusNames.Success:
                    return new SolidColorBrush(Color.FromRgb(22, 131, 63));
                case UploadStatusNames.Failed:
                    return new SolidColorBrush(Color.FromRgb(198, 40, 40));
                case UploadStatusNames.Processing:
                    return new SolidColorBrush(Color.FromRgb(180, 115, 0));
                default:
                    return new SolidColorBrush(Color.FromRgb(71, 85, 105));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
