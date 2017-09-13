using System;
using System.Net;
using System.Threading;

namespace SocialParser.Sourcess
{
    public abstract class ParsableMetricsSource:MetricsSource
    {
        protected WebClient webClient;
        private DateTime lastAgentChangeTime;
        private TimeSpan agentChangeTime;
        protected Random rnd;
        protected int minRequestDelay;
        protected int maxRequestDelay;
        public override TimeSpan ElapsedWhaitTime => TimeSpan.FromSeconds(maxRequestDelay/1.5);
        protected ParsableMetricsSource()
        {
            webClient = new WebClient();
            agentChangeTime = TimeSpan.FromMinutes(15);
            rnd = new Random();
            minRequestDelay = ProxyManager.IsDefined ? 5 : 10;
            maxRequestDelay = ProxyManager.IsDefined ? 10 : 15;
            SetUserAgent();
            SetProxy();
        }

        public override Metrics GetMetrics(string href)
        {
            base.GetMetrics(href);
            TryAvoidBan();
            if (IsFromApi(href))
            {
                return GetMetricsFromApi(href);
            }
            Thread.Sleep(TimeSpan.FromSeconds(rnd.Next(minRequestDelay, maxRequestDelay)));
            CheckUserAgent();
            return GetMetricsFromWeb(href);
        }

        protected abstract bool IsFromApi(string href);

        protected abstract Metrics GetMetricsFromApi(string href);


        private void SetProxy()
        {
            if (ProxyManager.IsDefined)
                webClient.Proxy = ProxyManager.CurrentProxy;
        }

        private void ClearProxy()
        {
            webClient.Proxy = null;
        }

        protected abstract Metrics GetMetricsFromWeb(string href);

        protected abstract void TryAvoidBan();

        private void CheckUserAgent()
        {
            if (DateTime.Now - lastAgentChangeTime > agentChangeTime)
                SetUserAgent();
        }

        private void SetUserAgent()
        {
            webClient.Headers.Clear();
            webClient.Headers.Add("user-agent", UserAgentManager.GetRandomAgent());
            lastAgentChangeTime = DateTime.Now;
        }

        protected string GetHtml(Uri postInfoUri)
        {
            while (true)
            {
                int counter = 0;
                // return webClient.DownloadString(postInfoUri);
                while (counter < 5)
                {
                    try
                    {
                        return webClient.DownloadString(postInfoUri);
                    }
                    catch (WebException)
                    {
                        counter++;
                    }
                }
                bool isProxyEnd = !ProxyManager.ChangeProxy();
                if (isProxyEnd)
                {
                    ClearProxy();
                    return webClient.DownloadString(postInfoUri);
                }
                SetProxy();
            }
        }
    }
}