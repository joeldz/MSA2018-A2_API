using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jdezscreenshotservice.Models
{
    public class ScreenshotItem
    {
        public int Id { get; set; }
        public string Series { get; set; }
        public string Episode { get; set; }
        public string Timestamp { get; set; }
        public string Subtitle { get; set; }
        public string Url { get; set; }
        public string Uploaded { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }
}
