using System;
using CsQuery;

namespace SocialParser.Sourcess
{
    //[Serializable]
    public class NewsSiteWithWiewCount:ParsableMetricsSource//MetricsSource
    {
        //private static WebBrowser webBrowser;
        //private static readonly Form form = new Form {Controls = { webBrowser}};
        //private static string userAgent = UserAgentManager.GetRandomAgent();
        //private static DateTime lastAgentChangeTime = DateTime.Now;
        //private static readonly TimeSpan agentChangeTime = TimeSpan.FromMinutes(20);
        public string Adress;
        public string Selector;
        public int Num;
        public NewsSiteWithWiewCount(string adress, string selector, int num)
        {
            Adress = adress;
            Selector = selector;
            Num = num;
        }
        /*private delegate void NavigateDeligate(string d, string c, byte[] b, string a);
        private static void NavigateInBrowser(string href)
        {
            if (webBrowser.InvokeRequired)
                form.Invoke(new NavigateDeligate(webBrowser.Navigate), href, null, null, userAgent);
                //form.Invoke((NavigateDeligate)Delegate.CreateDelegate(typeof(NavigateDeligate), webBrowser,
                //    webBrowser.GetType().GetMethod("Navigate")), );
            else webBrowser.Navigate(href, null, null, userAgent);
        }
        public static void Initialize()
        {
            webBrowser = new WebBrowser();
            form.Controls.Add(webBrowser);
        }
        private bool IsBrowserLoadFinished()
        {
            if (webBrowser.InvokeRequired)
            {
                return WebBrowserReadyState.Complete != (WebBrowserReadyState)
                       form.Invoke((FB.ReadyStateDelegate)Delegate.CreateDelegate(typeof(FB.ReadyStateDelegate), webBrowser,
                           webBrowser.GetType().GetProperty("ReadyState").GetGetMethod()));

            }
            return webBrowser.ReadyState != WebBrowserReadyState.Complete;
        }
        private Stream WebBrowserDocumentStream()
        {
            if (webBrowser.InvokeRequired)
            {
                return (Stream)form.Invoke((FB.StreamDelegate)Delegate.CreateDelegate(typeof(FB.StreamDelegate),
                    webBrowser, webBrowser.GetType().GetProperty("DocumentStream").GetGetMethod()));
            }
            return webBrowser.DocumentStream;
        }
        public override ulong GetMetrics(string href)
        {
            base.GetMetrics(href);
            CheckUserAgent();
            NavigateInBrowser(href);
            while (IsBrowserLoadFinished())
            {
                Thread.Sleep(20);
                //Application.DoEvents();
            }
            CQ cq = CQ.Create(WebBrowserDocumentStream());
            var views = cq.Find(Selector);
            if (views.Length==0) throw new ArgumentException("Bad href");
            try
            {
                return ulong.Parse(StringManipulations.GetOnlyNumbers(views[Num].InnerText));
            }
            catch (FormatException e)
            {
                ErrorManager.LogError(e);
                throw new ArgumentException("Bad href");
            }
        }*/

        protected override bool IsFromApi(string href)
        {
            return false;
        }

        protected override Metrics GetMetricsFromApi(string href)
        {
            throw new NotImplementedException();
        }

        protected override Metrics GetMetricsFromWeb(string href)
        {
            string html = webClient.DownloadString(href);
            CQ cq = CQ.Create(html);
            var views = cq.Find(Selector);
            if (views.Length == 0) throw new ArgumentException("Bad href");
            try
            {
                return new Metrics(ulong.Parse(StringManipulations.GetOnlyNumbers(views[Num].InnerText)),
                    "Views count on site");
            }
            catch (FormatException e)
            {
                ErrorManager.LogError(e);
                throw new ArgumentException("Bad href");
            }
        }

        protected override void TryAvoidBan()
        {
            
        }
        /*
        private void CheckUserAgent()
        {
            if (DateTime.Now - lastAgentChangeTime > agentChangeTime)
            {
                userAgent = UserAgentManager.GetRandomAgent();
                lastAgentChangeTime = DateTime.Now;
            }
        }*/

        protected override bool IsGoodHref(string href)
        {
            return href.Contains(".");
        }
    }
}