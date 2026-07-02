using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class GeminiResponse
    {
        public List<GeminiCandidate> candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent content { get; set; }

        public string finishReason { get; set; }

        public int index { get; set; }

        public SafetyRating[] safetyRatings { get; set; }
    }

    public class SafetyRating
    {
        public string category { get; set; }

        public string probability { get; set; }
    }
}