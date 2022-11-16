using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_ChatUI.Core;

namespace WPF_ChatUI.MVVM.Model
{
    [Serializable]
    internal class ContactModel : ObservableObject
    {
        public string Username { get; set; }
        public string ImageSource { get; set; }
        public string UserId { get; set; }
        public string UserPath { get; set; }
        public ObservableCollection<MessageModel> Messages { get; set; }

        public long ChatId { get; set; }

        
        public string LastMessage => Messages.Last().Message; 
        


       

    }
}
