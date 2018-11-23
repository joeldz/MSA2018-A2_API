using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using jdezscreenshotservice.Models;
using jdezscreenshotservice.Helpers;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using RestSharp;
using Newtonsoft.Json;
using System.Net.Http;
using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
using static jdezscreenshotservice.Models.ScreenshotDataItem;

namespace jdezscreenshotservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScreenshotController : ControllerBase
    {
        private readonly jdezscreenshotserviceContext _context;
        private IConfiguration _configuration;

        public ScreenshotController(jdezscreenshotserviceContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Screenshot
        [HttpGet]
        public IEnumerable<ScreenshotItem> GetScreenshotItem()
        {
            return _context.ScreenshotItem;
        }

        // GET: api/Screenshot/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetScreenshotItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var screenshotItem = await _context.ScreenshotItem.FindAsync(id);

            if (screenshotItem == null)
            {
                return NotFound();
            }

            return Ok(screenshotItem);
        }

        // PUT: api/Screenshot/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutScreenshotItem([FromRoute] int id, [FromBody] ScreenshotItem screenshotItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != screenshotItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(screenshotItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScreenshotItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Screenshot
        [HttpPost]
        public async Task<IActionResult> PostScreenshotItem([FromBody] ScreenshotItem screenshotItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.ScreenshotItem.Add(screenshotItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetScreenshotItem", new { id = screenshotItem.Id }, screenshotItem);
        }

        // DELETE: api/Screenshot/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScreenshotItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var screenshotItem = await _context.ScreenshotItem.FindAsync(id);
            if (screenshotItem == null)
            {
                return NotFound();
            }

            _context.ScreenshotItem.Remove(screenshotItem);
            await _context.SaveChangesAsync();

            return Ok(screenshotItem);
        }

        private bool ScreenshotItemExists(int id)
        {
            return _context.ScreenshotItem.Any(e => e.Id == id);
        }

        // GET: api/Screenshot/Subtitle
        [Route("subtitle")]
        [HttpGet]
        public async Task<List<string>> GetTags()
        {
            var subtitles = (from m in _context.ScreenshotItem
                         select m.Subtitle).Distinct();

            var returned = await subtitles.ToListAsync();

            return returned;
        }

        // GET: api/Meme/Tags

        [HttpGet]
        [Route("tag")]
        public async Task<List<ScreenshotItem>> GetTagsItem([FromQuery] string tags)
        {
            var subtitles = from m in _context.ScreenshotItem
                        select m; //get all the subtitles


            if (!String.IsNullOrEmpty(tags)) //make sure user gave a tag to search
            {
                subtitles = subtitles.Where(s => s.Subtitle.ToLower().Contains(tags.ToLower())); // find the entries with the search tag and reassign
            }

            var returned = await subtitles.ToListAsync(); //return the subtitles

            return returned;
        }

        [HttpPost, Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm]ScreenshotImageItem screenshot)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var stream = screenshot.Image.OpenReadStream())
                {
                    var cloudBlock = await UploadToBlob(screenshot.Image.FileName, null, stream);
                    //// Retrieve the filename of the file you have uploaded
                    //var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
                    if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
                    {
                        return BadRequest("An error has occured while uploading your file. Please try again.");
                    }

                    // Get Series, Episode, and Timestamp data from trace.moe API
                    var responseString = await "https://trace.moe/api/search"
                    .PostUrlEncodedAsync(new { image = ("data:image/jpeg;base64," + ConvertImageURLToBase64(cloudBlock.SnapshotQualifiedUri.AbsoluteUri)).ToString()})
                    .ReceiveString();

                    responseString = responseString.Replace("\\", "");
                    
                    var root = JsonConvert.DeserializeObject<RootObject>(responseString);
                    
                    ScreenshotItem screenshotItem = new ScreenshotItem();
                    screenshotItem.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;

                    
                    if (!string.IsNullOrEmpty(screenshot.Subtitle))
                    {
                        screenshotItem.Subtitle = screenshot.Subtitle;
                    }
                    else
                    {
                        // Get Subtitle data from Google Cloud Vision API
                        screenshotItem.Subtitle = GetSubtitle(screenshotItem.Url, "northern-music-223223", "./My-First-Project-d90a39f377c2.json");
                    }


                    if (!string.IsNullOrEmpty(screenshot.Series))
                    {
                        screenshotItem.Series = screenshot.Series;
                    }
                    else
                    {
                        screenshotItem.Series = root.docs[0].title_romaji;
                    }

                    if (!string.IsNullOrEmpty(screenshot.Episode))
                    {
                        screenshotItem.Episode = screenshot.Episode;
                    }
                    else
                    {
                        screenshotItem.Episode = root.docs[0].episode.ToString();
                    }

                    if (!string.IsNullOrEmpty(screenshot.Timestamp))
                    {
                        screenshotItem.Timestamp = screenshot.Timestamp;
                    }
                    else
                    {
                        TimeSpan t = TimeSpan.FromSeconds(root.docs[0].at);
                        string timestamp = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds);

                        screenshotItem.Timestamp = timestamp;
                    }
                    

                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    screenshotItem.Height = image.Height.ToString();
                    screenshotItem.Width = image.Width.ToString();
                    screenshotItem.Uploaded = DateTime.Now.ToString();

                    _context.ScreenshotItem.Add(screenshotItem);
                    await _context.SaveChangesAsync();

                    return Ok($"Screenshot of {screenshotItem.Series} at {screenshotItem.Uploaded} has successfully uploaded");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error has occured. Details: {ex.Message}");
            }
        }

        private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
        {

            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("images");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Generate a new filename for every new blob
                    var fileName = Guid.NewGuid().ToString();
                    fileName += GetFileExtention(filename);

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

                    if (stream != null)
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    else
                    {
                        return new CloudBlockBlob(new Uri(""));
                    }

                    return cloudBlockBlob;
                }
                catch (StorageException ex)
                {
                    return new CloudBlockBlob(new Uri(""));
                }
            }
            else
            {
                return new CloudBlockBlob(new Uri(""));
            }

        }

        private string GetFileExtention(string fileName)
        {
            if (!fileName.Contains("."))
                return ""; //no extension
            else
            {
                var extentionList = fileName.Split('.');
                return "." + extentionList.Last(); //assumes last item is the extension 
            }
        }
	
        private string GetSubtitle(string imageURI, string projectId, string jsonPath)
        {
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "./My-First-Project-d90a39f377c2.json");
            var client = ImageAnnotatorClient.Create();
            // Load image file
            var image = Google.Cloud.Vision.V1.Image.FromUri(imageURI);
            // Perform text detection
            var response = client.DetectText(image);

            string subtitle = "";
            bool tick = false;
            foreach (var annotation in response)
            {
                if (tick == false)
                {
                    tick = true;
                    subtitle = annotation.Description;
                }
            }
            return subtitle;
        }

        private String ConvertImageURLToBase64(String url)
        {
            StringBuilder _sb = new StringBuilder();

            Byte[] _byte = this.GetImage(url);

            _sb.Append(Convert.ToBase64String(_byte, 0, _byte.Length));

            return _sb.ToString();
        }

        private byte[] GetImage(string url)
        {
            Stream stream = null;
            byte[] buf;

            try
            {
                WebProxy myProxy = new WebProxy();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                stream = response.GetResponseStream();

                using (BinaryReader br = new BinaryReader(stream))
                {
                    int len = (int)(response.ContentLength);
                    buf = br.ReadBytes(len);
                    br.Close();
                }

                stream.Close();
                response.Close();
            }
            catch (Exception exp)
            {
                buf = null;
            }

            return (buf);
        }
    }
}