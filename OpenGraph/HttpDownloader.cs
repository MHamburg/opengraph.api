using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenGraph
{
    /// <summary>
    /// Http Downloader
    /// </summary>
    /// <remarks>
    /// http://stackoverflow.com/a/2700707
    /// </remarks>
    public class HttpDownloader
    {
        #region Private Fields

        private readonly string referer;
        private readonly string userAgent;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpDownloader"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="referer">The referer.</param>
        /// <param name="userAgent">The user agent.</param>
        public HttpDownloader(Uri url, string referer, string userAgent)
        {
            Encoding = Encoding.GetEncoding("ISO-8859-1");
            Url = url;
            this.userAgent = userAgent;
            this.referer = referer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpDownloader"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="referer">The referer.</param>
        /// <param name="userAgent">The user agent.</param>
        public HttpDownloader(string url, string referer, string userAgent) : this(new Uri(url), referer, userAgent)
        {
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public WebHeaderCollection Headers { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public Uri Url { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <returns>The content of the page</returns>
        public string GetPage()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (!string.IsNullOrEmpty(referer))
            {
                request.Referer = referer;
            }
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }

            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Headers = response.Headers;
                Url = response.ResponseUri;
                return ProcessContent(response);
            }
        }

        /// <summary>
        /// Gets the page asynchronosly
        /// </summary>
        /// <returns>
        /// The content of the page
        /// </returns>
        public async Task<string> GetPageAsync()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (!string.IsNullOrEmpty(referer))
            {
                request.Referer = referer;
            }
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }

            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            using (HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync()))
            {
                Headers = response.Headers;
                Url = response.ResponseUri;
                return ProcessContent(response);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Checks the meta character set and re encode.
        /// </summary>
        /// <param name="memStream">The memory stream.</param>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        private string CheckMetaCharSetAndReEncode(Stream memStream, string html)
        {
            Match m = new Regex(@"<meta\s+.*?charset\s*=\s*?""?(?<charset>[A-Za-z0-9_-]+?)""", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(html);
            ////Match m = new Regex(@"<meta\s+.*?charset\s*=\s*(?<charset>[A-Za-z0-9_-]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(html);
            if (m.Success)
            {
                string charset = m.Groups["charset"].Value.ToLower();
                if ((charset == "unicode") || (charset == "utf-16"))
                {
                    charset = "utf-8";
                }

                try
                {
                    Encoding metaEncoding = Encoding.GetEncoding(charset);
                    if (!Encoding.Equals(metaEncoding))
                    {
                        memStream.Position = 0L;
                        StreamReader recodeReader = new StreamReader(memStream, metaEncoding);
                        html = recodeReader.ReadToEnd().Trim();
                        recodeReader.Close();
                    }
                }
                // ReSharper disable once UncatchableException
                catch (ArgumentException)
                {
                }
            }

            return html;
        }

        /// <summary>
        /// Processes the content.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private string ProcessContent(HttpWebResponse response)
        {
            SetEncodingFromHeader(response);

            //Set memory stream
            MemoryStream memStream = new MemoryStream();

            using (Stream s = response.GetResponseStream())
            {
                if (s == null)
                {
                    throw new InvalidOperationException("Response stream came back as null");
                }

                //Check for compression
                if (response.ContentEncoding?.ToLower().Contains("gzip") ?? false)
                    ReadBytes(ref memStream, new GZipStream(s, CompressionMode.Decompress));
                else if (response.ContentEncoding?.ToLower().Contains("deflate") ?? false)
                    ReadBytes(ref memStream, new DeflateStream(s, CompressionMode.Decompress));
                else
                    ReadBytes(ref memStream, s);

                //Get the data we need
                string html;
                memStream.Position = 0;
                using (StreamReader r = new StreamReader(memStream, Encoding))
                {
                    html = r.ReadToEnd().Trim();
                    html = CheckMetaCharSetAndReEncode(memStream, html);
                }

                //Dispose of this item
                memStream.Dispose();

                //Return html
                return html;
            }
        }

        /// <summary>
        /// Reads the bytes.
        /// </summary>
        /// <param name="streamto">The streamto.</param>
        /// <param name="readfrom">The readfrom.</param>
        private void ReadBytes(ref MemoryStream streamto, Stream readfrom)
        {
            int bytesRead;
            byte[] buffer = new byte[0x1000];
            for (bytesRead = readfrom.Read(buffer, 0, buffer.Length); bytesRead > 0; bytesRead = readfrom.Read(buffer, 0, buffer.Length))
                streamto.Write(buffer, 0, bytesRead);
            readfrom.Close();
        }

        /// <summary>
        /// Sets the encoding from header.
        /// </summary>
        /// <param name="response">The response.</param>
        private void SetEncodingFromHeader(HttpWebResponse response)
        {
            string charset = null;
            if (string.IsNullOrEmpty(response.CharacterSet))
            {
                Match m = Regex.Match(response.ContentType, @";\s*charset\s*=\s*(?<charset>.*)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    charset = m.Groups["charset"].Value.Trim('\'', '"');
                }
            }
            else
            {
                charset = response.CharacterSet;
            }
            if (!string.IsNullOrEmpty(charset))
            {
                try
                {
                    Encoding = Encoding.GetEncoding(charset);
                }
                // ReSharper disable once UncatchableException
                catch (ArgumentException)
                {
                }
            }
        }

        #endregion Private Methods
    }
}