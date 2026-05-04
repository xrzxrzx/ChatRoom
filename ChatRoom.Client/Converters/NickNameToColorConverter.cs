using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using Microsoft.UI.Xaml;

namespace ChatRoom.Client.Converters
{
    public class NickNameToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var nickName = value as string;
            if (nickName == "System")
            {
                return new SolidColorBrush(Colors.SlateBlue);
            }
            else if(nickName == "Self")
            {
                return new SolidColorBrush(new Windows.UI.Color() { A = 255, R = 0, G = 62, B = 102 });
            }
            return new SolidColorBrush(new Windows.UI.Color() { A = 255, R = 59, G = 59, B = 59 });
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}