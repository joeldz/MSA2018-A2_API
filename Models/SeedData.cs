using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jdezscreenshotservice.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new jdezscreenshotserviceContext(
                serviceProvider.GetRequiredService<DbContextOptions<jdezscreenshotserviceContext>>()))
            {
                // Look for any movies.
                if (context.ScreenshotItem.Count() > 0)
                {
                    return;   // DB has been seeded
                }

                context.ScreenshotItem.AddRange(
                    new ScreenshotItem
                    {
                        Id = 1,
                        Series = "Suzumiya Haruhi no Yuuutsu",
                        Episode = "4",
                        Timestamp = "00:11:27",
                        Subtitle = "I'm fine.",
                        Url = "https://i.imgur.com/T2xSjb7.jpg",
                        Uploaded = "11/10/2018 10:09:52 PM",
                        Width = "960",
                        Height = "557"
                    }


                );
                context.SaveChanges();
            }
        }
    }
}
