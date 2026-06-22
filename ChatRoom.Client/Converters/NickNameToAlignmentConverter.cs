using ChatRoom.Client.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace ChatRoom.Client.Converters
{
    public class NickNameToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var senderType = (MessageInfoModel.MessageInfoSenderType)value;
            if (senderType == MessageInfoModel.MessageInfoSenderType.Self)
            {
                return HorizontalAlignment.Right;
            }
            else if(senderType == MessageInfoModel.MessageInfoSenderType.System)
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