using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace ChatRoom.Client.Converters
{
    public class NickNameToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var nickName = value as string;
            if (nickName == "Self")
            {
                return HorizontalAlignment.Right;
            }
            else if(nickName == "System")
            {
                return HorizontalAlignment.Center;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}