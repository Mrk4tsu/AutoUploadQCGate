using System;
using System.Globalization;
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
            if (value is string judgement)
            {
                return judgement?.ToLower() == "pass" ?
                    Application.Current.FindResource("JudgementPass") :
                    Application.Current.FindResource("JudgementFail");
            }
            return Application.Current.FindResource("JudgementFail");
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
            if (value is string judgement)
            {
                return judgement?.ToLower() == "pass" ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            return new SolidColorBrush(Color.FromRgb(244, 67, 54));
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
            if (value is string status)
            {
                switch (status?.ToLower())
                {
                    case "completed":
                    case "success":
                        return Application.Current.FindResource("StatusSuccess");
                    case "failed":
                    case "error":
                        return Application.Current.FindResource("StatusError");
                    case "processing":
                    case "uploading":
                        return Application.Current.FindResource("StatusProcessing");
                    default:
                        return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}