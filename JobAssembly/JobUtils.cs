using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using mshtml;
using Common.Logging;

namespace JobAssembly
{
    public static class JobUtils
    {
        public static string GetConfigValue(string key)
        {
            string res = string.Empty;
            AppSettingsReader reader = new AppSettingsReader();
            res = reader.GetValue(key, typeof(string)).ToString();

            return res;
        }

        public static void AppendElement(IHTMLDocument2 document, string html)
        {
            document.body.insertAdjacentHTML("beforeEnd", html);
        }

        public static IHTMLElement FindElement(IHTMLElement root, string path)
        {
            IHTMLElement result = null;
            string[] pathElements = path.Split(' ');

            foreach (IHTMLElement el in (IHTMLElementCollection)root.all)
            {
                if (pathElements.Length == 1)
                {
                    if (MatchElement(el, pathElements[0]))
                    {
                        result = el;
                        break;
                    }
                }
                else if (pathElements.Length == 2)
                {
                    if ((MatchElement(el, pathElements[1])) && 
                        (MatchElement(el.parentElement, pathElements[0])))
                    {
                        result = el;
                        break;
                    }
                }
                else if (pathElements.Length == 3)
                {
                    if ((MatchElement(el, pathElements[2])) &&
                        (MatchElement(el.parentElement, pathElements[1])) &&
                        (MatchElement(el.parentElement.parentElement, pathElements[0])))
                    {
                        result = el;
                        break;
                    }
                }
            }

            return result;
        }

        public static bool MatchElement(IHTMLElement el, string criteria)
        {
            int iBeg = -1, iEnd = -1;
            iBeg = criteria.IndexOf('[');
            iEnd = criteria.IndexOf(']');
            string tagName = "", attr = "", attrName = "";

            if ((iBeg < 0) && ( iEnd < 0)) 
            {
                tagName = criteria;
                return (el.tagName.ToLower() == tagName.ToLower());
            }
            else if ((iBeg >= 0) && ( iEnd > 0))
            {
                tagName = criteria.Substring(0, iBeg);
                attr = criteria.Substring(iBeg + 1, iEnd - (iBeg + 1)).ToLower();

                return ((el.tagName.ToLower() == tagName.ToLower()) && (el.outerHTML.ToLower().IndexOf(attr) >= 0));
            }

            // By default return false
            return false;
        }

        public static HTMLDocument LoadDocument(string url)
        {
            Stream data = null;
            StreamReader reader = null;
            try
            {
                string htmlContent = DownloadString(url);

                // Load HTML with injected scripts
                object[] oPageText = { htmlContent };
                HTMLDocument doc = new HTMLDocumentClass();
                IHTMLDocument2 doc2 = (IHTMLDocument2)doc;
                doc2.write(oPageText);

                while (doc2.body == null)
                {
                    Thread.Sleep(5000);
                }

                return doc;
            }
            catch (Exception e)
            {
                //logger.Error(e);
            }
            finally
            {
                // Cleanup
                if (data != null) data.Close();
                if (reader != null) reader.Close();
            }

            return null;
        }

        private static void SaveContent(string content, string filePath, ILog logger)
        {
            StreamWriter sw = new StreamWriter(filePath, false);
            sw.Write(content);

            logger.Info("Saved (" + content.Length + " bytes) of " + filePath);

            // Close the file
            sw.Close();
        }

        public static void SaveData(string content, string fileName, ILog logger)
        {
            string filePath = JobUtils.GetConfigValue("dataTargetDir") + fileName;

            SaveContent(content, filePath, logger);
        }

        public static void SaveImage(string url, string fileName)
        {
            Image tmpImage = null;
            try
            {
                // Open a connection
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.AllowWriteStreamBuffering = true;
                req.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";
                req.Referer = "http://www.google.com/";

                // set timeout for 20 seconds (Optional)
                req.Timeout = 20000;

                // Request response:
                WebResponse rsp = req.GetResponse();

                // Open data stream:
                Stream _WebStream = rsp.GetResponseStream();

                // convert webstream to image
                tmpImage = Image.FromStream(_WebStream);

                // Cleanup
                _WebStream.Close();
                rsp.Close();

                // Save image
                if (tmpImage != null)
                {
                    tmpImage.Save(fileName);
                }
            }
            catch (Exception e)
            {
            }
        }

        public static string DownloadString(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                byte[] buffer;
                using (Stream s = resp.GetResponseStream())
                {
                    buffer = ReadStream(s);
                }

                string pageEncoding = "";
                Encoding e = Encoding.UTF8;
                if (resp.ContentEncoding != "")
                    pageEncoding = resp.ContentEncoding;
                else if (resp.CharacterSet != "")
                    pageEncoding = resp.CharacterSet;
                else if (resp.ContentType != "")
                    pageEncoding = GetCharacterSet(resp.ContentType);

                if (pageEncoding == "")
                    pageEncoding = GetCharacterSet(buffer);

                if (pageEncoding != "")
                {
                    try
                    {
                        e = Encoding.GetEncoding(pageEncoding);
                    }
                    catch { }
                }

                string data = e.GetString(buffer);

                return data;
            }
        }

        private static string GetCharacterSet(string s)
        {
            s = s.ToUpper();
            int start = s.LastIndexOf("CHARSET");
            if (start == -1)
                return "";

            start = s.IndexOf("=", start);
            if (start == -1)
                return "";

            start++;
            s = s.Substring(start).Trim();
            int end = s.Length;

            int i = s.IndexOf(";");
            if (i != -1)
                end = i;
            i = s.IndexOf("\"");
            if (i != -1 && i < end)
                end = i;
            i = s.IndexOf("'");
            if (i != -1 && i < end)
                end = i;
            i = s.IndexOf("/");
            if (i != -1 && i < end)
                end = i;

            return s.Substring(0, end).Trim();
        }

        private static string GetCharacterSet(byte[] data)
        {
            string s = Encoding.Default.GetString(data);
            return GetCharacterSet(s);
        }

        private static byte[] ReadStream(Stream s)
        {
            try
            {
                byte[] buffer = new byte[8096];
                long CurLength;
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = s.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            CurLength = 0;
                            return ms.ToArray();
                        }
                        ms.Write(buffer, 0, read);
                        CurLength = ms.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static void SaveDetailsContent(string strDetailUrl, string fileName, ILog logger)
        {
            HTMLDocument docDetails = LoadDocument(strDetailUrl);

            if (docDetails != null)
            {

                // Get the detasil content and cleanup off the not needed elements
                IHTMLElement contentBox = docDetails.getElementById("contentboxsub");
                IHTMLElement cleaner = FindElement(contentBox, "div table");
                if (cleaner != null) { cleaner.outerHTML = ""; cleaner = null; }

                cleaner = FindElement(contentBox, "td input");
                if (cleaner != null) { cleaner.outerHTML = ""; cleaner = null; }

                cleaner = FindElement(contentBox, "input");
                if (cleaner != null) { cleaner.outerHTML = ""; cleaner = null; }

                // Save all related images
                SaveImages(contentBox, GetConfigValue("imgTargetDir"));

                // Update content (update image urls and disable links)
                UpdateImages(contentBox);
                DisableLinks(contentBox);
                string content = contentBox.innerHTML;
                content = content.Replace("\"", "'");
                content = content.Replace('\r', ' ');
                content = content.Replace('\n', ' ');

                // Save the details content
                SaveData("﻿RSSData.Detail = \"" + content + "\";", fileName, logger);
            }
        }

        public static void SaveEventsDetailsContent(string strDetailUrl, string fileName, ILog logger)
        {
            HTMLDocument docDetails = LoadDocument(strDetailUrl);

            if (docDetails != null)
            {
                // Get the detasil content and cleanup off the not needed elements
                IHTMLElement contentBox = docDetails.getElementById("contentboxsub");
                IHTMLElement cleaner = FindElement(contentBox, "td input");
                if (cleaner != null) { cleaner.outerHTML = ""; cleaner = null; }

                cleaner = FindElement(contentBox, "input");
                if (cleaner != null) { cleaner.outerHTML = ""; cleaner = null; }

                // Save all related images
                SaveImages(contentBox, GetConfigValue("imgTargetDir"));

                // Update content (update image urls and disable links)
                UpdateImages(contentBox);
                DisableLinks(contentBox);
                
                string content = contentBox.innerHTML;
                content = content.Replace("\"", "'");
                content = content.Replace('\r', ' ');
                content = content.Replace('\n', ' ');

                // Save the details content
                SaveData("﻿RSSData.Detail = \"" + content + "\";", fileName, logger);
            }
        }

        public static void DisableLinks(IHTMLElement rootElm)
        {
            // Iterate over all <a>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)rootElm.all)
            {
                if (el.tagName.ToLower() == "a")
                {
                    el.removeAttribute("href");
                }
            }
        }

        public static void UpdateImages(IHTMLElement rootElm)
        {
            // Iterate over all <img>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)rootElm.all)
            {
                string strName = "";
                if (el.tagName.ToLower() == "img")
                {
                    string src = el.getAttribute("src").ToString();

                    if ((src != null) && (src.Length > 0))
                    {
                        int idx = src.LastIndexOf('/') + 1;
                        strName = src.Substring(idx, src.Length - idx);
                        src = "images/" + strName;
                        el.setAttribute("src", (object)src);
                    }
                }
            }
        }

        public static void SaveImages(IHTMLElement rootElm, string targetFolder)
        { 
            // Iterate over all <img>'s
            foreach (IHTMLElement el in (IHTMLElementCollection)rootElm.all)
            {
                string strName = "";
                if (el.tagName.ToLower() == "img")
                {
                    string src = el.getAttribute("src").ToString();

                    if ((src != null) && (src.Length > 0))
                    {
                        int idx = src.LastIndexOf('/') + 1;
                        strName = src.Substring(idx, src.Length - idx);
                        src = JobUtils.GetConfigValue("sourceImgRoot") + strName;

                        // Save
                        SaveImage(src, targetFolder + "\\" + strName);
                    }
                }
            }
        }
    }
}
