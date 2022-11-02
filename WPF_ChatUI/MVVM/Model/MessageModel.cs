using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_ChatUI.MVVM.Model
{
    class MessageModel
    {
        public string Username { get; set; }
        public string UserNameColor { get; set; }
        public string ImageSource { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }

        public bool? IsFirstMessage { get; set; }
    }
}
