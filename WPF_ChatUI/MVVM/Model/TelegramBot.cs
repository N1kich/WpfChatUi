using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_ChatUI.Core;
using System.Windows.Media;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using System.Threading;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace WPF_ChatUI.MVVM.Model
{
    internal class TelegramBot: ObservableObject
    {
        //relative path to save files and etc
        const string relativePath = @"DownloadFiles\";

        //bot token: insert your bot token here
        const string botToken = "5201175628:AAHf_MxWSWsg8Qbz3e0zOGzfV2It8DtralU";

        public string FullPath => Path.GetFullPath(relativePath);

        public bool isMenuEnabled;

        public ImageSource ImageSourceAvatar { 
            get { return _imageSourceAvatar; }
            set { _imageSourceAvatar = value; OnPropertyChanged(); }
             }

        public void GetImageSource()
        {
            Uri imageUri = new Uri(botModel.ImageSource, UriKind.Relative);
            BitmapImage imgBitmap = new BitmapImage(imageUri);
            _imageSourceAvatar = imgBitmap;
        }

        ImageSource _imageSourceAvatar;

        public ContactModel botModel;

        public TelegramBotClient telegramBotUser;

        

        public TelegramBot()
        {
            telegramBotUser = new TelegramBotClient(botToken);
            isMenuEnabled = false;
            botModel = new ContactModel() { Messages = new ObservableCollection<MessageModel>()};
        }

    }
}
