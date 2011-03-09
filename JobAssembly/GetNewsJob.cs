using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Common.Logging;
using mshtml;
using Quartz;

namespace JobAssembly
{
    public class GetNewsJob : IStatefulJob
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(GetNewsJob));
        private static string urlNews = JobUtils.GetConfigValue("urlNews");
        private static string urlEvents = JobUtils.GetConfigValue("urlEvents");
        private static string urlTourism = JobUtils.GetConfigValue("urlTourism");
        private static string urlHotels = JobUtils.GetConfigValue("urlHotels");
        private static string urlRestaurants = JobUtils.GetConfigValue("urlRestaurants");
        private static string urlWeather = JobUtils.GetConfigValue("urlWeather");

        #region IStatefulJob implementation 

        /// <summary>
        /// Main execution entrypoint
        /// </summary>
        /// <param name="context"></param>
        public void Execute(JobExecutionContext context)
        {
            // Scrape News
            GetNews(urlNews);
            Thread.Sleep(30000);

            // Scrape Events
            GetEvents(urlEvents);
            Thread.Sleep(30000);

            // Scrape Tourism information
            GetTourism(urlTourism);
            Thread.Sleep(30000);

            // Scrape Hotels
            GetHotels(urlHotels);
            Thread.Sleep(30000);

            // Scrape Restaurants
            GetRestaurants(urlRestaurants);
            Thread.Sleep(30000);

            // Scrape Weather information
            GetWeather(urlWeather);
        }

        #endregion

        #region Scraper methods

        /// <summary>
        /// Retrieves and saves News information
        /// </summary>
        /// <param name="url"></param>
        public void GetNews(string newsUrl)
        {
            HTMLDocument document = JobUtils.LoadDocument(newsUrl);

            string rssRoot = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?> <?xml-stylesheet href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"?> <!-- generator=\"ICMS GemWeb v1.1\" --> <rdf:RDF  xmlns=\"http://purl.org/rss/1.0/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"     xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:ev=\"http://www.w3.org/2001/xml-events\">#####1</rdf:RDF>";
            string rssItem = "<item rdf:about=\"#####2\" image=\"#####5\" > <dc:format>text/html</dc:format> <date>#####3</date> <title>#####4</title> <description>#####6</description> </item>";
            string strRssContent = "", strRssDetailsContent = "";

            // Find original content placeholder
            IHTMLElement contentBox = document.getElementById("contentboxsub");

            // Iterate over all <TR>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)contentBox.all)
            {
                string strDate = "", strTitle = "", strDetailID = "", strDetailUrl = "", strRssItem = "";
                if (el.tagName.ToLower() == "tr")
                {
                    IHTMLElement date = JobUtils.FindElement(el, "td span");
                    if (date != null)
                    {
                        strDate = date.innerHTML;
                    }

                    IHTMLElement title = JobUtils.FindElement(el, "span a");
                    if (title != null)
                    {
                        // Process title
                        strTitle = title.innerText;
                        strDetailUrl = title.getAttribute("href").ToString();
                        int infoPos = strDetailUrl.IndexOf("_id=") + 4;
                        strDetailID = strDetailUrl.Substring(infoPos, strDetailUrl.IndexOf("&", infoPos) - infoPos);

                        // Save Details content
                        strDetailUrl = strDetailUrl.Replace("about:", JobUtils.GetConfigValue("sourceWebRoot"));
                        JobUtils.SaveDetailsContent(strDetailUrl, strDetailID + ".data");
                    }

                    if ((title == null) && (date == null)) continue;

                    strRssItem = rssItem.Replace("#####2", strDetailUrl);
                    strRssItem = strRssItem.Replace("#####4", strTitle);
                    strRssItem = strRssItem.Replace("#####5", "");
                    strRssItem = strRssItem.Replace("#####3", strDate);
                    strRssItem = strRssItem.Replace("#####6", "");
                    strRssItem += "\\\n";

                    strRssContent += strRssItem;

                    strRssDetailsContent += (strDetailUrl + "   \n");

                }
            }

            strRssContent = rssRoot.Replace("#####1", strRssContent);

            // Updated processed content
            strRssContent = strRssContent.Replace('"', '\'');
            strRssContent = "RSSData.Content = \"" + strRssContent + "\";";

            JobUtils.SaveData(strRssContent, "news.rss");
        }

        /// <summary>
        /// Retrieves and saves Weather information
        /// </summary>
        /// <param name="urlWeather"></param>
        private void GetWeather(string weatherUrl)
        {
            JobUtils.SaveEventsDetailsContent(weatherUrl, "weather.data");
        }

        /// <summary>
        /// Retrieves and saves Restaurants information
        /// </summary>
        /// <param name="urlRestaurants"></param>
        private void GetRestaurants(string restaurantsUrl)
        {
            HTMLDocument document = JobUtils.LoadDocument(restaurantsUrl);

            string rssRoot = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?> <?xml-stylesheet href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"?> <!-- generator=\"ICMS GemWeb v1.1\" --> <rdf:RDF  xmlns=\"http://purl.org/rss/1.0/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"     xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:ev=\"http://www.w3.org/2001/xml-events\">#####1</rdf:RDF>";
            string rssItem = "<item rdf:about=\"#####2\" image=\"#####5\" > <dc:format>text/html</dc:format> <date>#####3</date> <title>#####4</title> <description>#####6</description> </item>";
            string strRssContent = "", strRssDetailsContent = "";

            // Find original content placeholder
            IHTMLElement contentBox = document.getElementById("contentboxsub");

            // Iterate over all <TR>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)contentBox.all)
            {
                string strTel = "", strTitle = "", strDetailID = "", strDetailUrl = "", strRssItem = "";
                if (el.tagName.ToLower() == "tr")
                {
                    IHTMLElement title = JobUtils.FindElement(el, "span a");
                    if (title != null)
                    {
                        // Process title
                        strTitle = title.innerText;
                        strDetailUrl = title.getAttribute("href").ToString();
                        int infoPos = strDetailUrl.IndexOf("_id=") + 4;
                        int ampPos = strDetailUrl.IndexOf("&", infoPos);
                        if (ampPos < 0) ampPos = strDetailUrl.Length;
                        strDetailID = strDetailUrl.Substring(infoPos, ampPos - infoPos);
                    }

                    IHTMLElement tel = JobUtils.FindElement(el, "td[nowrap] span");
                    if (tel != null)
                    {
                        strTel = tel.innerText;
                    }

                    if ((title == null) || (tel == null)) continue;
                    // Save Details content
                    strDetailUrl = strDetailUrl.Replace("about:blank", JobUtils.GetConfigValue("urlRestaurants"));
                    JobUtils.SaveEventsDetailsContent(strDetailUrl, strDetailID + ".data");

                    strRssItem = rssItem.Replace("#####2", strDetailUrl);
                    strRssItem = strRssItem.Replace("#####4", strTitle);
                    strRssItem = strRssItem.Replace("#####5", "");
                    strRssItem = strRssItem.Replace("#####3", "");
                    strRssItem = strRssItem.Replace("#####6", strTel);
                    strRssItem += "\\\n";

                    strRssContent += strRssItem;

                    strRssDetailsContent += (strDetailUrl + "   \n");
                }
            }

            strRssContent = rssRoot.Replace("#####1", strRssContent);

            // Updated processed content
            strRssContent = strRssContent.Replace('"', '\'');
            strRssContent = "RSSData.Content = \"" + strRssContent + "\";";

            JobUtils.SaveData(strRssContent, "restaurants.rss");
        }

        /// <summary>
        /// Retrieves and saves Hotels information
        /// </summary>
        /// <param name="urlHotels"></param>
        private void GetHotels(string hotelsUrl)
        {
            HTMLDocument document = JobUtils.LoadDocument(hotelsUrl);

            string rssRoot = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?> <?xml-stylesheet href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"?> <!-- generator=\"ICMS GemWeb v1.1\" --> <rdf:RDF  xmlns=\"http://purl.org/rss/1.0/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"     xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:ev=\"http://www.w3.org/2001/xml-events\">#####1</rdf:RDF>";
            string rssItem = "<item rdf:about=\"#####2\" image=\"#####5\" > <dc:format>text/html</dc:format> <date>#####3</date> <title>#####4</title> <description>#####6</description> </item>";
            string strRssContent = "", strRssDetailsContent = "";

            // Find original content placeholder
            IHTMLElement contentBox = document.getElementById("contentboxsub");

            // Iterate over all <TR>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)contentBox.all)
            {
                string strTel = "", strTitle = "", strDetailID = "", strDetailUrl = "", strRssItem = "";
                if (el.tagName.ToLower() == "tr")
                {
                    IHTMLElement title = JobUtils.FindElement(el, "span a");
                    if (title != null)
                    {
                        // Process title
                        strTitle = title.innerText;
                        strDetailUrl = title.getAttribute("href").ToString();
                        int infoPos = strDetailUrl.IndexOf("_id=") + 4;
                        int ampPos = strDetailUrl.IndexOf("&", infoPos);
                        if (ampPos < 0) ampPos = strDetailUrl.Length;
                        strDetailID = strDetailUrl.Substring(infoPos, ampPos - infoPos);
                    }

                    IHTMLElement tel = JobUtils.FindElement(el, "td[nowrap] span");
                    if (tel != null)
                    {
                        strTel = tel.innerText;
                    }

                    if ((title == null) || (tel == null)) continue;
                    // Save Details content
                    strDetailUrl = strDetailUrl.Replace("about:blank", JobUtils.GetConfigValue("urlHotels"));
                    JobUtils.SaveEventsDetailsContent(strDetailUrl, strDetailID + ".data");

                    strRssItem = rssItem.Replace("#####2", strDetailUrl);
                    strRssItem = strRssItem.Replace("#####4", strTitle);
                    strRssItem = strRssItem.Replace("#####5", "");
                    strRssItem = strRssItem.Replace("#####3", "");
                    strRssItem = strRssItem.Replace("#####6", strTel);
                    strRssItem += "\\\n";

                    strRssContent += strRssItem;

                    strRssDetailsContent += (strDetailUrl + "   \n");

                }
            }

            strRssContent = rssRoot.Replace("#####1", strRssContent);

            // Updated processed content
            strRssContent = strRssContent.Replace('"', '\'');
            strRssContent = "RSSData.Content = \"" + strRssContent + "\";";

            JobUtils.SaveData(strRssContent, "hotels.rss");
        }

        /// <summary>
        /// Retrieves and saves Tourism information
        /// </summary>
        /// <param name="urlTourism"></param>
        private void GetTourism(string tourismUrl)
        {
            HTMLDocument document = JobUtils.LoadDocument(tourismUrl);

            string rssRoot = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?> <?xml-stylesheet href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"?> <!-- generator=\"ICMS GemWeb v1.1\" --> <rdf:RDF  xmlns=\"http://purl.org/rss/1.0/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"     xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:ev=\"http://www.w3.org/2001/xml-events\">#####1</rdf:RDF>";
            string rssItem = "<item rdf:about=\"#####2\" image=\"#####5\" > <dc:format>text/html</dc:format> <date>#####3</date> <title>#####4</title> <description>#####6</description> </item>";
            string strRssContent = "", strRssDetailsContent = "";

            // Find original content placeholder
            IHTMLElement contentBox = document.getElementById("contentboxsub");

            // Iterate over all <TR>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)contentBox.all)
            {
                string strTitle = "", strDetailID = "", strDetailUrl = "",
                    strRssItem = "", strImgName = "";
                if (el.tagName.ToLower() == "tr")
                {
                    IHTMLElement img = JobUtils.FindElement(el, "a img");
                    if (img != null)
                    {
                        string src = img.getAttribute("src").ToString();

                        if ((src != null) && (src.Length > 0))
                        {
                            int idx = src.LastIndexOf('/') + 1;
                            strImgName = src.Substring(idx, src.Length - idx);
                            src = JobUtils.GetConfigValue("sourceImgRoot") + strImgName;
                            
                            // Save thumbnail image
                            JobUtils.SaveImage(src, JobUtils.GetConfigValue("thumbTargetDir") + "\\" + strImgName);

                            // Get detasil ID
                            strDetailUrl = img.parentElement.getAttribute("href").ToString();
                            int infoPos = strDetailUrl.IndexOf("_id=") + 4;
                            int ampPos = strDetailUrl.IndexOf("&", infoPos);
                            if (ampPos < 0) ampPos = strDetailUrl.Length;
                            strDetailID = strDetailUrl.Substring(infoPos, ampPos - infoPos);
                        }
                    }
                    
                    IHTMLElement title = JobUtils.FindElement(el, "a b");
                    if (title != null)
                    {
                        strTitle = title.innerText;
                    }

                    if ((title == null) || (img == null)) continue;
                    
                    // Save Details content
                    strDetailUrl = strDetailUrl.Replace("about:", JobUtils.GetConfigValue("sourceWebRoot"));
                    JobUtils.SaveEventsDetailsContent(strDetailUrl, strDetailID + ".data");

                    strRssItem = rssItem.Replace("#####2", strDetailUrl);
                    strRssItem = strRssItem.Replace("#####4", strTitle);
                    strRssItem = strRssItem.Replace("#####5", "images/thumbs/" + strImgName);
                    strRssItem = strRssItem.Replace("#####3", "");
                    strRssItem = strRssItem.Replace("#####6", "");
                    strRssItem += "\\\n";

                    strRssContent += strRssItem;

                    strRssDetailsContent += (strDetailUrl + "   \n");
                }
            }

            strRssContent = rssRoot.Replace("#####1", strRssContent);

            // Updated processed content
            strRssContent = strRssContent.Replace('"', '\'');
            strRssContent = "RSSData.Content = \"" + strRssContent + "\";";

            JobUtils.SaveData(strRssContent, "tourism.rss");
        }

        /// <summary>
        /// Retrieves and saves Events information
        /// </summary>
        /// <param name="urlEvents"></param>
        private void GetEvents(string eventsUrl)
        {
            HTMLDocument document = JobUtils.LoadDocument(eventsUrl);

            string rssRoot = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?> <?xml-stylesheet href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"?> <!-- generator=\"ICMS GemWeb v1.1\" --> <rdf:RDF  xmlns=\"http://purl.org/rss/1.0/\" xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"     xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:ev=\"http://www.w3.org/2001/xml-events\">#####1</rdf:RDF>";
            string rssItem = "<item rdf:about=\"#####2\" image=\"#####5\" > <dc:format>text/html</dc:format> <date>#####3</date> <title>#####4</title> <description>#####6</description> </item>";
            string strRssContent = "", strRssDetailsContent = "";

            // Find original content placeholder
            IHTMLElement contentBox = document.getElementById("contentboxsub");

            // Iterate over all <TR>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)contentBox.all)
            {
                string strDate = "", strTitle = "", strDetailID = "", strDetailUrl = "", strRssItem = "";
                if (el.tagName.ToLower() == "tr")
                {
                    IHTMLElement date = JobUtils.FindElement(el, "td span");
                    if (date != null)
                    {
                        strDate = date.innerHTML;
                    }

                    IHTMLElement title = JobUtils.FindElement(el, "span a");
                    if (title != null)
                    {
                        // Process title
                        strTitle = title.innerText;
                        strDetailUrl = title.getAttribute("href").ToString();
                        int infoPos = strDetailUrl.IndexOf("_id=") + 4;
                        int ampPos = strDetailUrl.IndexOf("&", infoPos);
                        if (ampPos < 0) ampPos = strDetailUrl.Length;
                        strDetailID = strDetailUrl.Substring(infoPos, ampPos - infoPos);

                        // Save Details content
                        strDetailUrl = strDetailUrl.Replace("about:", JobUtils.GetConfigValue("sourceWebRoot"));
                        JobUtils.SaveEventsDetailsContent(strDetailUrl, strDetailID + ".data");
                    }

                    if ((title == null) || (date == null)) continue;

                    strRssItem = rssItem.Replace("#####2", strDetailUrl);
                    strRssItem = strRssItem.Replace("#####4", strTitle);
                    strRssItem = strRssItem.Replace("#####5", "");
                    strRssItem = strRssItem.Replace("#####3", strDate);
                    strRssItem = strRssItem.Replace("#####6", "");
                    strRssItem += "\\\n";

                    strRssContent += strRssItem;

                    strRssDetailsContent += (strDetailUrl + "   \n");

                }
            }

            strRssContent = rssRoot.Replace("#####1", strRssContent);

            // Updated processed content
            strRssContent = strRssContent.Replace('"', '\'');
            strRssContent = "RSSData.Content = \"" + strRssContent + "\";";

            JobUtils.SaveData(strRssContent, "events.rss");
        }

        #endregion

    }
}
