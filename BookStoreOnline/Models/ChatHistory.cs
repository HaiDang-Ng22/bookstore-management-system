using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class ChatHistory
    {
        public List<ChatMessage> Messages { get; set; }

        public ChatHistory()
        {
            Messages = new List<ChatMessage>();
        }
    }
}