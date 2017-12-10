﻿using Xunit;

namespace OpenGraph.Tests
{
    public class OpenGraphFacts
    {
        #region Private Fields

        /// <summary>
        /// The invalid missing all meta
        /// </summary>
        private string invalidMissingAllMeta = @"<!DOCTYPE HTML>
                                                <html>
                                                <head>
                                                    <title>some title</title>
                                                </head>
                                                <body>
                                                </body>
                                                </html>";

        /// <summary>
        /// The invalid missing required URLs
        /// </summary>
        private string invalidMissingRequiredUrls = @"<!DOCTYPE HTML>
                                                    <html>
                                                    <head>
                                                        <meta property=""og:type"" content=""product"" />
                                                        <meta property=""og:title"" cOntent=""Product Title"" />
                                                        <meta property=""og:description"" content=""My Description""/>
                                                        <meta property=""og:site_Name"" content=""Fact Site"">
                                                    </head>
                                                    <body>
                                                    </body>
                                                    </html>";

        /// <summary>
        /// The invalid sample content
        /// </summary>
        private string invalidSampleContent = @"<!DOCTYPE HTML>
                                                <html>
                                                <head>
                                                    <meta property=""og:title"" cOntent=""Product Title"" />
                                                    <meta name=""og:image"" content=""http://www.Fact.com/Fact.png""/>
                                                    <meta propErty=""og:uRl"" content=""http://www.Fact.com"" />
                                                    <meta property=""og:description"" content=""My Description""/>
                                                    <meta property=""og:site_Name"" content=""Fact Site"">
                                                    <meta property=""og:mistake"" value=""not included"">
                                                </head>
                                                <body>
                                                </body>
                                                </html>";

        /// <summary>
        /// The valid sample content
        /// </summary>
        private string validSampleContent = @"<!DOCTYPE HTML>
                                            <html>
                                            <head>
                                                <meta property=""og:type"" content=""product"" />
                                                <meta property=""og:title"" cOntent=""Product Title"" />
                                                <meta name=""og:image"" content=""http://www.Fact.com/Fact.png""/>
                                                <meta propErty=""og:uRl"" content=""http://www.Fact.com"" />
                                                <meta property=""og:description"" content=""My Description""/>
                                                <meta property=""og:site_Name"" content=""Fact Site"">
                                            </head>
                                            <body>
                                            </body>
                                            </html>";

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Facts calling <c>MakeOpenGraph</c> method
        /// </summary>
        public void FactMakingOpenGraphMetaTags()
        {
            var title = "some title";
            var type = "website";
            var image = "http://www.go.com/someimg.jpg";
            var url = "http://www.go.com/";
            var description = "some description";
            var siteName = "my site";
            var graph = OpenGraph.MakeGraph(title, type, image, url, description, siteName);

            Assert.Equal(title, graph.Title);
            Assert.Equal(type, graph.Type);
            Assert.Equal(image, graph.Image.ToString());
            Assert.Equal(url, graph.Url.ToString());
            Assert.Equal(description, graph["description"]);
            Assert.Equal(siteName, graph["site_name"]);

            var expected = "<meta property=\"og:title\" content=\"some title\">" +
                "<meta property=\"og:type\" content=\"website\">" +
                "<meta property=\"og:image\" content=\"http://www.go.com/someimg.jpg\">" +
                "<meta property=\"og:url\" content=\"http://www.go.com/\">" +
                "<meta property=\"og:description\" content=\"some description\">" +
                "<meta property=\"og:site_name\" content=\"my site\">";
            Assert.Equal(expected, graph.ToString());
        }

        /// <summary>
        /// Facts the meta charset parses correctly.
        /// </summary>
        [Fact]
        public void FactMetaCharsetParsesCorrectly()
        {
            var expectedTitle = "Réalité virtuelle : 360° de bonheur à améliorer";
            var expectedDescription =
                "Le cinéma à 360° a désormais son festival. Organisé par le Forum des images, le premier Paris Virtual Film Festival a donc vu le jour....";

            var ogs = OpenGraph.ParseUrl("http://www.telerama.fr/cinema/realite-virtuelle-360-de-bonheur-a-ameliorer,144339.php?utm_medium=Social&utm_source=Twitter&utm_campaign=Echobox&utm_term=Autofeed#link_time=1466595239");

            Assert.Equal(expectedTitle, ogs["title"]);
            Assert.Equal(expectedDescription, ogs["description"]);
        }

        /// <summary>
        /// Facts the parsing amazon URL asynchronous Fact.
        /// </summary>
        [Fact]
        public void FactParsingAmazonUrlAsyncFact()
        {
            OpenGraph graph = OpenGraph.ParseUrlAsync("http://www.amazon.com/Spaced-Complete-Simon-Pegg/dp/B0019MFY3Q").Result;

            Assert.Equal("http://www.amazon.com/dp/B0019MFY3Q/ref=tsm_1_fb_lk", graph.Url.ToString());
            Assert.True(graph.Title.StartsWith("Spaced: The Complete Series"));
            Assert.True(graph["description"].Contains("Spaced"));
            Assert.True(graph.Image.ToString().Contains("images-amazon"));
            Assert.Equal("movie", graph.Type);
            Assert.Equal("Amazon.com", graph["site_name"]);
        }

        /// <summary>
        /// Fact parsing a URL
        /// </summary>
        [Fact]
        public void FactParsingAmazonUrlFact()
        {
            OpenGraph graph = OpenGraph.ParseUrl("http://www.amazon.com/Spaced-Complete-Simon-Pegg/dp/B0019MFY3Q");

            Assert.Equal("http://www.amazon.com/dp/B0019MFY3Q/ref=tsm_1_fb_lk", graph.Url.ToString());
            Assert.True(graph.Title.StartsWith("Spaced: The Complete Series"));
            Assert.True(graph["description"].Contains("Spaced"));
            Assert.True(graph.Image.ToString().Contains("images-amazon"));
            Assert.Equal("movie", graph.Type);
            Assert.Equal("Amazon.com", graph["site_name"]);
        }

        /// <summary>
        /// Facts parsing the HTML that is missing URLs
        /// </summary>
        [Fact]
        public void FactParsingHtmlHtmlMissingUrlsFact()
        {
            OpenGraph graph = OpenGraph.ParseHtml(this.invalidMissingRequiredUrls);

            Assert.Equal("product", graph.Type);
            Assert.Equal("Product Title", graph.Title);
            Assert.Null(graph.Image);
            Assert.Null(graph.Url);
            Assert.Equal("My Description", graph["description"]);
            Assert.Equal("Fact Site", graph["site_name"]);
        }

        /// <summary>
        /// Fact that parsing the HTML with invalid graph specification throws an exception
        /// </summary>
        [Fact]
        public void FactParsingHtmlInvalidGraphParsingFact()
        {
            Assert.Throws<InvalidSpecificationException>(() => OpenGraph.ParseHtml(this.invalidSampleContent, true));
        }

        /// <summary>
        /// Fact that parsing the HTML with invalid graph specification throws an exception
        /// </summary>
        [Fact]
        public void FactParsingHtmlInvalidGraphParsingMissingAllMetaFact()
        {
            Assert.Throws<InvalidSpecificationException>(() => OpenGraph.ParseHtml(this.invalidMissingAllMeta, true));
        }

        /// <summary>
        /// Fact that parsing the HTML with invalid graph specification passes when validate specification boolean is off
        /// </summary>
        [Fact]
        public void FactParsingHtmlInvalidGraphParsingWithoutCheckFact()
        {
            OpenGraph graph = OpenGraph.ParseHtml(this.invalidSampleContent);

            Assert.Equal(string.Empty, graph.Type);
            Assert.False(graph.ContainsKey("mistake"));
            Assert.Equal("Product Title", graph.Title);
            Assert.Equal("http://www.fact.com/Fact.png", graph.Image.ToString());
            Assert.Equal("http://www.fact.com/", graph.Url.ToString());
            Assert.Equal("My Description", graph["description"]);
            Assert.Equal("Fact Site", graph["site_name"]);
        }

        /// <summary>
        /// Facts parsing the HTML
        /// </summary>
        [Fact]
        public void FactParsingHtmlValidGraphParsingFact()
        {
            OpenGraph graph = OpenGraph.ParseHtml(this.validSampleContent, true);

            Assert.Equal("product", graph.Type);
            Assert.Equal("Product Title", graph.Title);
            Assert.Equal("http://www.fact.com/Fact.png", graph.Image.ToString());
            Assert.Equal("http://www.fact.com/", graph.Url.ToString());
            Assert.Equal("My Description", graph["description"]);
            Assert.Equal("Fact Site", graph["site_name"]);
        }

        /// <summary>
        /// Facts the parsing URL asynchronous validate encoding is correct.
        /// </summary>
        [Fact]
        public void FactParsingUrlAsyncValidateEncodingIsCorrect()
        {
            var expectedContent =
                "Создайте себе горное настроение с нашим первым фан-китом по игре #SteepGame&amp;#33; -&amp;gt; http://ubi.li/u8w9n";
            var tags = OpenGraph.ParseUrlAsync("https://vk.com/wall-41600377_66756").Result;

            Assert.True(tags["description"] == expectedContent);
        }

        /// <summary>
        /// Facts the parsing URL validate encoding is correct.
        /// </summary>
        [Fact]
        public void FactParsingUrlValidateEncodingIsCorrect()
        {
            var expectedContent =
                "Создайте себе горное настроение с нашим первым фан-китом по игре #SteepGame&amp;#33; -&amp;gt; http://ubi.li/u8w9n";
            var tags = OpenGraph.ParseUrl("https://vk.com/wall-41600377_66756");

            Assert.True(tags["description"] == expectedContent);
        }

        [Fact]
        public void FactUrlDecodingUrlValues()
        {
            var expectedUrl =
                "https://tn.periscope.tv/lXc5gSh6UPaWdc37LtVCb3UdtSfvj2QNutojPK2du5YWrNchfI4wXpwwHKTyfDhmfT2ibsBZV4doQeWlhSvI4A==/chunk_314.jpg?Expires=1781852253&Signature=U5OY3Y2HRb4ETmakQAPwMcv~bqu6KygIxriooa41rk64RcDfjww~qpVgMR-T1iX4S9NxfvXHLMT3pEckBDEOicsNO7oUAo4NieH9GRB2Sv0EA7swxLojD~Zn98ThNWTF5fSzv6SSPjyvctsqBiRmvAN6x7fmMH6l3vzx8ePSCgdEm8-31lUAz7lReBNZQjYSi~C8AwqZVI0Mx6y8lNKklL~m0e6RTGdvr~-KIDewU3wpjSdX7AgpaXXjahk4x-ceUUKcH3T1j--ZjaY7nqPO9fbMZFNPs502A32mrcmaZCzvaD~AuoH~u3y44mJVjzHRrpTxHIBklqHxAgc7dzverg__&Key-Pair-Id=APKAIHCXHHQVRTVSFRWQ";
            var og = OpenGraph.ParseUrl("https://www.periscope.tv/w/1DXxyZZZVykKM");

            Assert.Equal(expectedUrl, og["image"]);
        }

        #endregion Public Methods
    }
}