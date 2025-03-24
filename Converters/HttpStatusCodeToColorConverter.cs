using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using System.Net;

namespace Lance.Converters
{
    public class HttpStatusCodeToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush SuccessColor = new(Colors.PaleGreen);
        private static readonly SolidColorBrush ClientErrorColor = new(Colors.PaleGoldenrod);
        private static readonly SolidColorBrush ServerErrorColor = new(Colors.PaleVioletRed);
        private static readonly SolidColorBrush FallbackColor = new(Colors.Gray);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is HttpStatusCode statusCode)
            {
                return statusCode switch
                {
                    < HttpStatusCode.BadRequest => SuccessColor,
                    < HttpStatusCode.InternalServerError => ClientErrorColor,
                    _ => ServerErrorColor
                };
            }

            return FallbackColor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
