using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPF_ChatUI.MVVM.Model
{
    internal class ContactModel
    {
        public string Username { get; set; }
        public string ImageSource { get; set; }
        public string UserId { get; set; }
        public string UserPath { get; set; }
        public ObservableCollection<MessageModel> Messages { get; set; }
        public string LastMessage => Messages.Last().Message;
    }
}
