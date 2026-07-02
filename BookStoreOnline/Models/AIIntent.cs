using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class AIIntent
    {
        public bool IsProductSearch { get; set; }

        public bool IsOrderLookup { get; set; }

        public bool IsGreeting { get; set; }

        public bool IsSupport { get; set; }

        public bool IsGeneralQuestion { get; set; }
    }
}