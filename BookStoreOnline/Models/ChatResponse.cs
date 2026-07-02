using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class ChatResponse
    {
        public bool Success { get; set; }

        public string Type { get; set; }

        public string Message { get; set; }

        public List<ProductResult> Products { get; set; }
    }
}