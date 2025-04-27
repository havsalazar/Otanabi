using Microsoft.UI.Xaml.Data;

namespace Otanabi.Converters
{
    class TruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (parameter == null)
            {
                return value;
            }
            int _MaxLength;
            if (!int.TryParse(parameter.ToString(), out _MaxLength))
            {
                return value;
            }
            var _String = value.ToString();
            if (_String.Length > _MaxLength)
            {
                _String = _String.Substring(0, _MaxLength) + "...";
            }
            return _String;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
