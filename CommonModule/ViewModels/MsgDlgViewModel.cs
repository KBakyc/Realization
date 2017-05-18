using System;
using CommonModule.Commands;
using System.Windows.Media;

namespace CommonModule.ViewModels
{
    public enum MsgType { Message, Text, ImagePath, ImageSource }

    /// <summary>
    /// Модель диалога отображения сообщения.
    /// </summary>
    public class MsgDlgViewModel : BaseDlgViewModel
    {
        /// <summary>
        /// Сообщение
        /// </summary>
        private string message;
        public string Message
        {
            get { return message; }
            set { SetAndNotifyProperty("Message", ref message, value); }
        }

        private MsgType messageType = MsgType.Message;
        public MsgType MessageType
        {
            get { return messageType; }
            set { SetAndNotifyProperty("MessageType", ref messageType, value); }
        }

        private int maxWidth = int.MaxValue;
        public int MaxWidth
        {
            get { return maxWidth; }
            set { SetAndNotifyProperty("MaxWidth", ref maxWidth, value); }
        }

        private ImageSource imageMsg;
        public ImageSource ImageMsg
        {
            get { return imageMsg; }
            set { SetAndNotifyProperty("ImageMsg", ref imageMsg, value); }
        }

    }
}