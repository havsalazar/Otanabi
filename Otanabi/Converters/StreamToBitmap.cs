using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Otanabi.Helpers;
using Windows.Storage.Streams;

namespace Otanabi.Converters;

public sealed class StreamToBitmap : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        IRandomAccessStream strm;
        if (value is IRandomAccessStream rand)
            strm = rand.CloneStream();
        else if (value is IRandomAccessStreamReference randRef)
            strm = randRef.OpenReadAsync().AsTask().GetAwaiter().GetResult();
        else
            throw new ArgumentException(
                $"The provided value must be of type {typeof(IRandomAccessStream)}.",
                nameof(value)
            );
        var img = new BitmapImage();
        void OnImageOpened(object sender, RoutedEventArgs e)
        {
            strm.Dispose();
            img.ImageOpened -= OnImageOpened;
        }
        img.ImageOpened += OnImageOpened;
        _ = img.SetSourceAsync(strm);
        return img;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
