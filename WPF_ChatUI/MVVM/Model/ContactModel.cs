using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;


namespace WPF_ChatUI.MVVM.Model
{
    [Serializable]
    internal class ContactModel
    {
        public string Username { get; set; }
        public string ImageSource { get; set; }
        public string UserId { get; set; }
        public string UserPath { get; set; }
        public ObservableCollection<MessageModel> Messages { get; set; }

        public long ChatId { get; set; }

        public string LastMessage { get; set; }

        public void GetLastMsg()
        {
            if (Messages.Last().Message.Length >= 22)
            {
                LastMessage = Messages.Last().Message.Substring(0, 19) + "...";
            }
            else
            {
                LastMessage = Messages.Last().Message;
            }
            
        }

        
    }
}
