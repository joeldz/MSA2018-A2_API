using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace jdezscreenshotservice.Models
{
    public class jdezscreenshotserviceContext : DbContext
    {
        public jdezscreenshotserviceContext (DbContextOptions<jdezscreenshotserviceContext> options)
            : base(options)
        {
        }

        public DbSet<jdezscreenshotservice.Models.ScreenshotItem> ScreenshotItem { get; set; }
    }
}
