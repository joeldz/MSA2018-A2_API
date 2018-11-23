using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jdezscreenshotservice.Models
{
    public class ScreenshotImageItem
    {
        public string Series { get; set; }
        public string Episode { get; set; }
        public string Timestamp { get; set; }
        public string Subtitle { get; set; }
        public IFormFile Image { get; set; }
    }
}
