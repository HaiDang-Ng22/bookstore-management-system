using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class ChatMessage
    {
        public string Role { get; set; }

        public string Message { get; set; }

        public DateTime Time { get; set; }
    }
}