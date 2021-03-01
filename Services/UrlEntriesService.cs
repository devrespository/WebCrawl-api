using System;
using System.Collections.Generic;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Interfaces;
using WebApi.Models.UrlEntries;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApi.Services
{
    public class UrlEntriesService: IUrlEntriesService
    {

        private WebCrawlerDbContext _webCrawlerDbContext;
        readonly HashSet<Uri> _hashUrlSet = new HashSet<Uri>();
        Uri _mainUri;
        private List<UrlEntries> _urlEntries = new List<UrlEntries>();

        private string _expression;
        private Uri _url;
        private string _domainName;

        public UrlEntriesService(WebCrawlerDbContext context)
        {
            _webCrawlerDbContext = context;
        }

        public IEnumerable<UrlEntries> GetAllEntries()
        {
            return _webCrawlerDbContext.UrlEntries;
        }

        public void Search(SearchModel urlSearch)
        {
            AddUrls(GetUrlData(urlSearch));
        }

        public void AddUrls(List<UrlEntries> urlEntriesList)
        {
            if (urlEntriesList == null)
            {
                return;
            }

            _webCrawlerDbContext.UrlEntries.AddRangeAsync(urlEntriesList);
            _webCrawlerDbContext.SaveChanges();
        }
        public void AddUrl(UrlEntries urlEntries)
        {
            if (urlEntries == null)
            {
                return;
            }

            _webCrawlerDbContext.UrlEntries.Add(urlEntries);
            _webCrawlerDbContext.SaveChanges();
        }


        private List<UrlEntries> GetUrlData(SearchModel urlSearch)
        {
            _expression = urlSearch.Expression;
            _url = GetUri(urlSearch.Url);
            _domainName = GetDomain(_url.Host);
            _mainUri = _url;
            SearchAllTheUrls(_url);
            if (_urlEntries.Count > 0)
            {
                return _urlEntries;
            }

            return null;

        }

        private Uri GetUri(string url)
        {
            return new UriBuilder(url).Uri;
        }

        /// <summary>
        /// Searches all the urls for the given.
        /// </summary>
        /// <param name="urlAddress">The URL address.</param>
        private void SearchAllTheUrls(Uri urlAddress)
        {

            if (!_hashUrlSet.Contains(urlAddress) && urlAddress.Host.Contains(_domainName))
            {
                var htmlData = GetUrlData(urlAddress);

                if (htmlData != null)
                {
                    var hostUri = urlAddress;

                    var htmlDataUrlList = Extract(htmlData);

                    var expressionCount = Regex.Matches(htmlData, _expression, RegexOptions.IgnoreCase).Count;

                    if (_hashUrlSet.Add(urlAddress))
                    {

                        var urlEntries = new UrlEntries
                        {
                            Url = urlAddress,
                            Date = DateTimeOffset.Now,
                            EnteredExpression = _expression,
                            NumberOfHits = expressionCount
                        };
                        //Performance issue
                        //_urlEntries.Add(urlEntries);

                        AddUrl(urlEntries);
                    }

                    var newSubUrls = GetUniqueUrls(htmlDataUrlList, hostUri);
                    foreach (var subHashUrl in newSubUrls)
                    {
                        SearchAllTheUrls(subHashUrl);

                    }
                }
            }
        }

        /// <summary>
        /// Gets the unique urls contained in the given Page link.
        /// </summary>
        /// <param name="urlList">The list.</param>
        /// <param name="myUri">My URI.</param>
        /// <returns></returns>
        private HashSet<Uri> GetUniqueUrls(List<string> urlList, Uri myUri)
        {
            var hashUrlSets = new HashSet<Uri>();
            foreach (var url in urlList)
            {
                if (url == null)
                {
                    continue;
                }

                var currentUrl = url.Trim();
                if (!currentUrl.Trim().Any(x => char.IsLetter(x)))
                {
                    continue;
                }

                if (!currentUrl.StartsWith("//"))
                {
                    if (currentUrl.StartsWith("/"))
                    {
                        currentUrl = myUri.Scheme + "://" + myUri.Host + currentUrl;
                    }
                }
                if (currentUrl.StartsWith("//"))
                {
                    if (!currentUrl.Contains(_domainName))
                    {
                        continue;
                    }
                    else
                    {
                        currentUrl = myUri.Scheme + ":" + currentUrl;
                    }
                }

                //Exclude all StyleSheet, image and JS files.
                if (currentUrl.ToLower().Contains(".css")
                    || currentUrl.ToLower().Contains(".js")
                    || currentUrl.ToLower().Contains(".ico")
                    || currentUrl.ToLower().Contains(".png")
                    || currentUrl.ToLower().Contains(".jpeg")
                    || currentUrl.ToLower().Contains(".svg")
                    || currentUrl.ToLower().Contains(".jpg"))
                {
                    continue;
                }

                if (currentUrl.EndsWith("/"))
                {
                    currentUrl = currentUrl.Remove(currentUrl.Length - 1);
                }


                if (Uri.IsWellFormedUriString(currentUrl, UriKind.RelativeOrAbsolute))
                {
                    try
                    {
                        hashUrlSets.Add(GetUri(currentUrl));
                    }
                    catch
                    {
                        //ignored
                    }
                }

            }

            return hashUrlSets;
        }

        /// <summary>
        /// Gets the HTML data of the URL provided.
        /// </summary>
        /// <param name="urlAddress">The URL address.</param>
        /// <returns></returns>
        private string GetUrlData(Uri urlAddress)
        {
            string htmlData = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (string.IsNullOrWhiteSpace(response.CharacterSet))
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));


                    htmlData = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();
                }
                return htmlData;
            }
            catch
            {
                // ignored
            }

            return htmlData;
        }

        /// <summary>
        /// Extracts all src and href links from a HTML string.
        /// </summary>
        /// <param name="html">The html source</param>
        /// <returns>A list of links - these will be all links including javascript ones.</returns>
        private static List<string> Extract(string html)
        {
            var urlList = new List<string>();

            var regex = new Regex("(?:href)=[\"|']?(.*?)[\"|'|>]+", RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (regex.IsMatch(html))
            {
                foreach (Match match in regex.Matches(html))
                {
                    urlList.Add(match.Groups[1].Value.Trim());
                }
            }

            return urlList;
        }

        /// <summary>
        /// Function to get the domain.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string GetDomain(string url)
        {
            string[] urlParts = url.Split('.');
            if (urlParts.Length >= 2)
            {
                return urlParts[urlParts.Length - 2];
            }
            else
            {
                return url;
            }

        }
    }
}
