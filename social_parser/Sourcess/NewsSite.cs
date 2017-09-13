using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
//using CsQuery;

namespace SocialParser.Sourcess
{
    public class NewsSite:ParsableMetricsSource
    {
        public NewsSite()
        { 
            minRequestDelay = 3;
            maxRequestDelay = 10;
        }

        //private const string action = "http://www.websiteoutlook.com/Getstats.php";
        private const string apiHref = "https://api.similarweb.com/SimilarWebAddon/";

        protected override bool IsGoodHref(string href)
        {
            return href.Contains(".");
        }

        private string GetHostName(string href)
        {
            var tmp = href.Substring(href.IndexOf(@"://", StringComparison.Ordinal) + 3).Split('/')[0];
            return tmp.IndexOf("www.", StringComparison.Ordinal) == 0? tmp.Substring(4) : tmp;
        }

        protected override bool IsFromApi(string href)
        {
            return false;
        }

        protected override Metrics GetMetricsFromWeb(string href)
        {
            string hostName = GetHostName(href);
            try
            {
                return NewSiteDB.GetSite(hostName).GetMetrics(href);
            }
            catch (ArgumentOutOfRangeException) { }

            string result = "";

            HttpWebRequest objRequest = (HttpWebRequest) WebRequest.Create(apiHref + hostName + "/all");
            objRequest.UserAgent = UserAgentManager.GetChromeAgent();
            if (ProxyManager.IsDefined)
                objRequest.Proxy = ProxyManager.CurrentProxy;
            objRequest.Method = "GET";
            objRequest.ContentType = "application/json; charset=utf-8";
            int count = 0;
            HttpWebResponse objResponse = null;
            while (count < 5)
            {
                try
                {
                    objResponse = (HttpWebResponse) objRequest.GetResponse();
                }
                catch (WebException)
                {
                    count++;
                    Thread.Sleep(500);
                    continue;
                }
                using (StreamReader sr =
                    new StreamReader(objResponse.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    sr.Close();
                    break;
                }
            }
            if (objResponse == null || !result.Contains(hostName))
                throw new ArgumentException("Bad href");

            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            Dictionary<string, decimal> engagments =
                JsonConvert.DeserializeObject<Dictionary<string, decimal>>(values["Engagments"].ToString());
            List<Dictionary<string, double>> topCountries = JsonConvert.DeserializeObject<List
                <Dictionary<string, double>>>(values["TopCountryShares"].ToString());
            double fromUkraine = Double.MinValue;
            foreach (var country in topCountries)
                if (country["Country"] == 804)
                {
                    fromUkraine = country["Value"];
                    break;
                }

            fromUkraine = fromUkraine == Double.MinValue ? topCountries.Last()["Value"] / 2 : fromUkraine;
            return new Metrics((ulong) (engagments["Visits"] * (1m - engagments["BounceRate"]) * (decimal) fromUkraine /
                            DateTime.DaysInMonth((int) engagments["Year"], (int) engagments["Month"])), "Similarweb");
        }
        /*string hostName = GetHostName(href);
        string result;
        string strPost = "website=" + hostName;
        StreamWriter myWriter = null;

        HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(action);
        objRequest.UserAgent = UserAgentManager.GetRandomAgent();
        if (ProxyManager.IsDefined)
            objRequest.Proxy = ProxyManager.CurrentProxy;
        objRequest.Method = "POST";
        objRequest.ContentLength = strPost.Length;
        objRequest.ContentType = "application/x-www-form-urlencoded";

        try
        {
            myWriter = new StreamWriter(objRequest.GetRequestStream());
            myWriter.Write(strPost);
        }
        catch (Exception e)
        {
            ErrorManager.LogError(e.ToString());
            return 0;
        }
        finally
        {
            myWriter?.Close();
        }

        HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
        using (StreamReader sr =
           new StreamReader(objResponse.GetResponseStream()))
        {
            result = sr.ReadToEnd();

            // Close and clean up the StreamReader
            sr.Close();
        }
        CQ cq = CQ.Create(result);
        foreach (IDomObject obj in cq.Find("span.label-warning"))
        {
            string text = obj.InnerText;
            try
            {
                if (text.Contains("Day"))
                    return GetNormalNumPresentation(text.Split('/')[0]);
            }
            catch (Exception e)
            {
                ErrorManager.LogError(e.ToString());
            }
        }
        throw new ArgumentException("Bad href");*/
        
        /*
        private ulong GetNormalNumPresentation(string s)
        {
            var num = double.Parse(s.Substring(0, s.Length - 1).Replace('.', ','));
            switch (s[s.Length - 1])
            {
                case 'K':
                    return (ulong) (num * 1e3);
                case 'M':
                    return (ulong)(num * 1e6);
                case 'B':
                    return (ulong)(num * 1e9);
                default:
                    return (ulong)decimal.Parse(s);
            }
        }
        */
        protected override void TryAvoidBan()
        {
            
        }

        protected override Metrics GetMetricsFromApi(string href)
        {
            throw new NotImplementedException();
        }
    }
}