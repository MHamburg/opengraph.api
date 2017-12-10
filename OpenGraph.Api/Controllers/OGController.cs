using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core;
using Jil;
using Microsoft.AspNetCore.Mvc;

namespace OpenGraph.Api.Controllers
{
    [Route("scan")]
    //http://blogs.microsoft.co.il/sasha/2017/02/27/profiling-a-net-core-application-on-linux/
    //https://github.com/dotnet/coreclr/issues/14991
    //https://github.com/dotnet/coreclr/issues/13489#issuecomment-343416765
    //https://github.com/aspnet/aspnet-docker/issues/300
    public class OGController : Controller
    {
        #region Private Fields
        
        ///<summary>
        /// Caching
        /// </summary>
        private readonly ICacheManager<string> _cached;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OGController"/> class.
        /// </summary>
        /// <param name="cacheManager">The cache manager.</param>
        public OGController(ICacheManager<string> cacheManager) =>
            _cached = cacheManager;

        #endregion Public Constructors

        #region Public Methods

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string url)
        {
            //Notify
            Console.WriteLine($"Processing: {url}");
            Uri trueurl = new Uri(url);

            try
            {
                //Check cache
                string key = $"OpenGraph:{MD5(url)}";
                string toreturn = _cached.Get(key);
                if (toreturn != null)
                {
                    Console.WriteLine($"Returning cached item: {url}");
                    return Ok(toreturn);
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //Check if we need true url
                if (url.ToLower().Contains("yahoo"))
                {
                    using (var client = new HttpClient())
                    {
                        var dontknow = await client.GetStringAsync(url);
                        var secondurl = dontknow
                            .Split('=')
                            .Select(x => x.Replace("'", ""))
                            .Last(x => x.StartsWith("http"));

                        //Check if we have a hit
                        if (Uri.TryCreate(secondurl, UriKind.RelativeOrAbsolute, out Uri newuri) && !secondurl.Contains("</"))
                            trueurl = newuri;
                    }
                }

                //Get any redirects
                trueurl = new Uri(await RedirectPath(trueurl.ToString()));

                //Return opengraph data
                toreturn = JSON.Serialize(await OpenGraph.ParseUrlAsync(trueurl,
                    userAgent: "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20100101 Firefox/10.0"));
                _cached.Add(key, toreturn);

                //Set return notify
                sw.Stop();
                Console.WriteLine($"Returning non-cached item: {url} ({sw.Elapsed})");

                //return result
                return Ok(toreturn);
            }
            catch (Exception exc)
            {
                string message = $"Error processing: {trueurl}! {exc.Message}";
                Console.WriteLine(message);
                return StatusCode(500, message);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// MD5 Hash a string
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        private string MD5(string s)
        {
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(s)))
                    builder.Append(b.ToString("x2").ToLower());

                return builder.ToString();
            }
        }

        /// <summary>
        /// Redirects the path.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private async Task<string> RedirectPath(string url)
        {
            try
            {
                List<string> redirects = new List<string>();
                string location = string.Copy(url);
                while (!string.IsNullOrWhiteSpace(location))
                {
                    redirects.Add(location);
                    HttpWebRequest request = WebRequest.CreateHttp(location);
                    request.AllowAutoRedirect = false;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync()))
                        {
                            location = response.GetResponseHeader("Location");
                        }
                    }
                    catch (WebException e)
                    {
                        location = e.Response.Headers.Get("location");
                    }
                }

                return redirects.Last();
            }
            catch
            {
                return url;
            }
        }

        #endregion Private Methods
    }
}