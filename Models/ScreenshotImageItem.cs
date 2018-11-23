using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jdezscreenshotservice.Models
{
    public class ScreenshotImageItem
    {
        public IFormFile Image { get; set; }
    }
}
