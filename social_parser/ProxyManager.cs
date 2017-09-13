using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocialParser
{
    public static class ProxyManager
    {
        private static DateTime lastRefreshTime;
        private static readonly TimeSpan refreshDelta = TimeSpan.FromMinutes(30);
        private static WebProxy currentProxy;
        private const string FName = "ProxyServers.txt";
        private static bool isEnd;
        private static StreamReader streamReader;
        public static bool IsDefined;
        private static bool IsCurrProxyExist => CurrentProxy != null;
        public static WebProxy CurrentProxy
        {
            get
            {
                if (isEnd) return null;
                if (currentProxy == null || lastRefreshTime - DateTime.Now > refreshDelta)
                    ChangeProxy();
                return currentProxy;
            }
        }

        public static bool ChangeProxy()
        {
            if (streamReader == null)
            {
                Initialize(); // Caling change proxy again
                return streamReader != null; // this can be only if Dispose wasn`t called
            }
            while (true)
            {
                if (streamReader.Peek() < 0)
                {
                    Dispose();
                    MessageBox.Show(@"All proxy servers was used, prorgramm will run without it.");
                    return false;
                }
                var line = streamReader.ReadLine().Split(':');
                var proxy = new WebProxy(line[0], int.Parse(line[1]));
                if (isCorrect(proxy))
                {
                    lastRefreshTime = DateTime.Now;
                    currentProxy = proxy;
                    return true;
                }
            }
        }

        public static void Initialize()
        {
            Task.Factory.StartNew(() =>
            {
                AutoClosingMessageBox.Show("Initializing proxy server. Please, wait...",
                    "Initializing proxy server");
            });
            if (!IsFileExistAndNotEmpty())
            {
                Dispose();
                MessageBox.Show("Proxy list not exist or empty.\nProgram will run without it.");
                return;
            }
            isEnd = false;
            IsDefined = true;
            try
            {
                streamReader = new StreamReader(FName);
            }
            catch (IOException e)
            {
                ErrorManager.LogError(e);
                IsDefined = false;
            }
            if (IsDefined)
                IsDefined = IsCurrProxyExist;
            if (!IsDefined)
                Dispose();
        }

        private static void Dispose()
        {
            IsDefined = false;
            isEnd = true;
            currentProxy = null;
            streamReader = null;
        }

        private static bool IsFileExistAndNotEmpty()
        {
            if (!File.Exists(FName) || new FileInfo(FName).Length == 0)
                return false;
            return true;
        }

        private static bool isCorrect(WebProxy proxy)
        {
            MyWebClient webClient = new MyWebClient();
            proxy.BypassProxyOnLocal = false;
            webClient.Proxy = proxy;
            try
            {
                if (webClient.DownloadString("http://google.com").Contains("google"))
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        private class MyWebClient : WebClient
        {
            public int Timeout = 10;
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = Timeout * 1000;
                return w;
            }
        }
    }
}