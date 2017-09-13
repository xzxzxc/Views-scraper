using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CsQuery;
using Facebook;
using SocialParser.Properties;

namespace SocialParser.Sourcess
{
    public class FB:ParsableMetricsSource, IOleClientSite, IServiceProvider, IAuthenticate
    {
        private FacebookClient fb;
        private dynamic hrefResult;
        private bool isLogined;
        private bool isNeedWait;
        private WebBrowser webBrowser;
        private static Form form;
        private string currentId;

        private void InitializeProxy()
        {
            webBrowser.Navigate("about:blank");
            object obj = webBrowser.ActiveXInstance;
            IOleObject oc = obj as IOleObject;
            oc.SetClientSite(this);
            SetProxyServer(ProxyManager.CurrentProxy.Address.Authority);
            
        }

        public FB()
        {
            fb = new FacebookClient();

            

            InitializeFormComponents();
            if (!Form1.IsInternetConnection()) return;
            AutorizeApi();
            AutorizeWeb();
        }

        private void AutorizeApi()
        {
            int retryCount = 0;
            dynamic result = null;
            while (retryCount < 5)
            {
                try
                {
                    result = fb.Get("oauth/access_token", new
                    {
                        client_id = ConfigurationManager.AppSettings["fb_client_id"],
                        client_secret = ConfigurationManager.AppSettings["fb_client_secret"],
                        grant_type = "client_credentials"
                    });
                    break;
                }
                catch (FacebookOAuthException)
                {
                    retryCount++;
                }
                catch (WebExceptionWrapper e)
                {
                    ErrorManager.LogError(e);
                    break;
                }
            }
            if (result != null) fb.AccessToken = result.access_token;
        }

        private void InitializeFormComponents()
        {
            InitializeForm();
            InitializeBrowser();
            if (ProxyManager.IsDefined)
                InitializeProxy();
        }

        private void InitializeBrowser()
        {
            webBrowser = new WebBrowser();
            webBrowser.Size = new Size(340, 460);
            form.Controls.Add(webBrowser);
        }

        private void InitializeForm()
        {
            form = new Form{
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(340, 480),
                Icon = Resources.Social_Parser
            };
            var btn = new Button();
            btn.Click += (sender, args) => { isLogined = false; form.Hide(); };
            var homeBtn = new Button { Text = @"Home", Location = new Point(0, 420), Size = new Size(50, 20) };
            homeBtn.Click += (sender, args) => {NavigateInBrowser(@"http://m.facebook.com/");};
            form.Controls.Add(homeBtn);
            form.CancelButton = btn;
        }

        private void NavigateInBrowser(string href)
        {
            webBrowser.Navigate(href, null, null, webClient.Headers.Get("user-agent"));
        }

        public void AutorizeWeb()
        {
            if (form.Visible) return;
            if (form.IsDisposed) { InitializeFormComponents(); }
            webBrowser.Navigate("about:blank");
            while (IsBrowserLoadFinished())
            {
                Thread.Sleep(20);
                Application.DoEvents();
            }
            isNeedWait = true;
            if (form.InvokeRequired)
                form.Invoke(new VoidDelegate(form.Show));
            else
                form.Show();
            // navigate to facebook
            NavigateInBrowser(@"http://m.facebook.com/");
            webBrowser.DocumentCompleted += DoLoginWork;
        }

        delegate void VoidDelegate();
        public delegate WebBrowserReadyState ReadyStateDelegate();
        private bool IsBrowserLoadFinished()
        {
            if (webBrowser.InvokeRequired)
            {
                return WebBrowserReadyState.Complete != (WebBrowserReadyState) 
                    form.Invoke((ReadyStateDelegate) Delegate.CreateDelegate(typeof(ReadyStateDelegate), webBrowser,
                    webBrowser.GetType().GetProperty("ReadyState").GetGetMethod()));

            }
            return webBrowser.ReadyState != WebBrowserReadyState.Complete;
        }

        private void DoLoginWork(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            while (true)
            {
                if (webBrowser.IsDisposed || form.IsDisposed)
                    return;
                if (IsFacebookLogined())
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
            (sender as WebBrowser).DocumentCompleted -= DoLoginWork;
#if !DEBUG
            form.Hide();
#endif
            isNeedWait = false;
            isLogined = true;
        }

        private bool IsFacebookLogined()
        {
            return null != WebBrowserDocument()?.GetElementById("m_newsfeed_stream");
        }

        protected override Metrics GetMetricsFromApi(string href)
        {
            if (href.Contains("/events/"))
            {
                int retryCount = 0;
                while (retryCount < 5)
                {
                    try
                    {
                        dynamic result = fb.Get($"/{hrefResult.id}/?fields=attending_count,interested_count,maybe_count");
                        return new Metrics((ulong) (result.attending_count + result.interested_count 
                            + result.maybe_count), "FB event attending+interested+maybe count");
                    }
                    catch (FacebookOAuthException)
                    {
                        retryCount++;
                    }
                }
            }
            else
            {
                int retryCount = 0;
                while (retryCount < 5)
                {
                    try
                    {
                        dynamic fanCountResult = fb.Get($"/{hrefResult.id}/?fields=fan_count");
                        return new Metrics((ulong) fanCountResult.fan_count, "FB page fans count");
                    }
                    catch (FacebookOAuthException)
                    {
                        retryCount++;
                    }
                }
            }
            throw new ArgumentException("Bad href");
        }

        protected override Metrics GetMetricsFromWeb(string href)
        {
            while (!isLogined)
            {
                AutorizeWeb();
                while (isNeedWait)
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
            }
            while (isNeedWait)
            {
                Thread.Sleep(20);
                //Application.DoEvents();
            }
            if (href.Contains("/groups/"))
            {
                NavigateInBrowser("http://facebook.com/" + currentId);
                while (IsBrowserLoadFinished())
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
                var temp = WebBrowserDocument().GetElementById("count_text");
                if (temp == null)
                    throw new ArgumentException("Bad href");
                return new Metrics(ulong.Parse(StringManipulations.GetOnlyNumbers(temp.InnerText)),
                    "FB group members count");
            }
            NavigateInBrowser("http://m.facebook.com/" + currentId);
            while (IsBrowserLoadFinished())
            {
                Thread.Sleep(20);
                //Application.DoEvents();
            }
            CQ cq = CQ.Create(WebBrowserDocumentStream());
            long friends = -1;
            foreach (IDomObject obj in cq.Find("a"))
            {
                if (obj.HasAttribute("href") && (obj.GetAttribute("href").Contains("friends?") ||
                                                 obj.GetAttribute("href").Contains("friends&")) &&
                    !obj.HasAttribute("class"))
                {
                    friends = long.Parse(StringManipulations.GetOnlyNumbers(obj.InnerText.Split('(').Last()));
                }
            }
            NavigateInBrowser("http://facebook.com/" + currentId);
            while (IsBrowserLoadFinished())
            {
                Thread.Sleep(20);
                //Application.DoEvents();
            }
            cq = CQ.Create(WebBrowserDocumentStream());
            var folHref = cq.Find("a[href$=followers]");
            long followers = folHref.Length == 0?0:long.Parse(StringManipulations.GetOnlyNumbers(folHref[0].InnerText.Split(' ')[0]));
            if (friends == -1 && followers==0)
                throw new ArgumentException("Bad href");
            return new Metrics((ulong) (friends + followers), "FB friends+followers count");
            
        }
        delegate HtmlDocument HtmlDocumentDelegate();
        private HtmlDocument WebBrowserDocument()
        {
            if (webBrowser.InvokeRequired)
            {
                return (HtmlDocument)form.Invoke((HtmlDocumentDelegate)Delegate.CreateDelegate(typeof(HtmlDocumentDelegate),
                    webBrowser, webBrowser.GetType().GetProperty("Document").GetGetMethod()));
            }
            return webBrowser.Document;
        }
        public delegate Stream StreamDelegate();
        private Stream WebBrowserDocumentStream()
        {
            if (webBrowser.InvokeRequired)
            {
                return (Stream) form.Invoke((StreamDelegate)Delegate.CreateDelegate(typeof(StreamDelegate),
                    webBrowser, webBrowser.GetType().GetProperty("DocumentStream").GetGetMethod()));
            }
            return webBrowser.DocumentStream;
        }

        protected override void TryAvoidBan()
        {
            if (rnd.Next(25) == 0)
            {
                NavigateInBrowser("http://m.facebook.com/");
                while (IsBrowserLoadFinished())
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
                if (rnd.Next(2) == 0)
                    return;
                CQ cq = CQ.Create(WebBrowserDocumentStream());
                var posts = cq.Find("div[role|=\"article\"]");
                var hrefs = posts.Find("a[href]").ToList();
                if (hrefs.Count == 0) return;
                hrefs.Shuffle();
                string href = null;
                foreach (var hr in hrefs)
                {
                    href = hr.GetAttribute("href");
                    if (href.Contains("profile.php?") || href.Contains("/events/") || href.Contains("/groups/"))
                        break;
                }
                if (href == null)
                    return;
                NavigateInBrowser("http://m.facebook.com" + href);
                while (IsBrowserLoadFinished())
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
            }
        }

        protected override bool IsFromApi(string href)
        {
            hrefResult = null;
            currentId = GetId(href);
            int retryCount = 0;
            while (retryCount < 5)
            {
                try
                {
                    hrefResult = fb.Get($"{currentId}?fields=category");
                    break;
                }
                catch (FacebookOAuthException e)
                {
                    if (e.ErrorCode == 100 || e.ErrorCode == 803)
                        return false;
                    retryCount++;
                }
                catch (FacebookApiException e)
                {
                    if (e.ErrorCode == 100) // Unaviable material
                        throw new ArgumentException("Bad href");
                }
                catch (Exception e)
                {
                    ErrorManager.LogError(e);
                }
            }
            if (hrefResult == null)
                throw new WebException("Can't connect to facebook");
            return true;
        }

        private string GetId(string href)
        {
            if (href.Contains("permalink.php?story_fbid="))
                return href.Substring(href.IndexOf("&id=", StringComparison.Ordinal) + 4);
            if (href.Contains("photo.php?"))
            {
                while (!isLogined)
                {
                    AutorizeWeb();
                    while (isNeedWait)
                    {
                        Thread.Sleep(20);
                        //Application.DoEvents();
                    }
                }
                while (isNeedWait)
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
                // href = href.Split('&')[0];
                NavigateInBrowser(href);
                while (IsBrowserLoadFinished())
                {
                    Thread.Sleep(20);
                    //Application.DoEvents();
                }
                CQ cq = CQ.Create(WebBrowserDocumentStream());
                foreach (IDomObject obj in cq.Find("a"))
                {
                    if (obj.HasAttribute("href") && obj.GetAttribute("href").Contains("facebook.com") &&
                        obj.HasAttribute("data-hovercard") && obj.GetAttribute("data-hovercard").Contains("user.php?"))
                    {
                        return obj.GetAttribute("href").Split('/')[3].Split('?')[0];
                    }
                }
                throw new ArgumentException("Bad href");
            }
            if (href.Contains("/groups/") || href.Contains("/events/"))
                return href.Split('/')[4];
            return href.Split('/')[3];
        }

        protected override bool IsGoodHref(string href)
        {
            return href.Contains("permalink.php?story_fbid=") || href.Contains("/posts/")
                   || href.Contains("/photos/") || href.Contains("/media_set?") || href.Contains("/videos/") 
                   || href.Contains("photo.php?") || href.Contains("/groups/") || href.Contains("/events/");
        }


        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption,
            IntPtr lpBuffer, int lpdwBufferLength);
        private Guid IID_IAuthenticate = new Guid("79eac9d0-baf9-11ce-8c82-00aa004ba90b");
        private const int INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011);
        private const int S_OK = 0x00000000;
        private const int INTERNET_OPTION_PROXY = 38;
        private const int INTERNET_OPEN_TYPE_DIRECT = 1;
        private const int INTERNET_OPEN_TYPE_PROXY = 3;
#pragma warning disable CS0649 // Field 'FB._currentUsername' is never assigned to, and will always have its default value null
        private string _currentUsername;
#pragma warning restore CS0649 // Field 'FB._currentUsername' is never assigned to, and will always have its default value null
#pragma warning disable CS0649 // Field 'FB._currentPassword' is never assigned to, and will always have its default value null
        private string _currentPassword;
#pragma warning restore CS0649 // Field 'FB._currentPassword' is never assigned to, and will always have its default value null


        private void SetProxyServer(string proxy)
        {
            //Create structure
            INTERNET_PROXY_INFO proxyInfo = new INTERNET_PROXY_INFO();

            if (proxy == null)
            {
                proxyInfo.dwAccessType = INTERNET_OPEN_TYPE_DIRECT;
            }
            else
            {
                proxyInfo.dwAccessType = INTERNET_OPEN_TYPE_PROXY;
                proxyInfo.proxy = Marshal.StringToHGlobalAnsi(proxy);
                proxyInfo.proxyBypass = Marshal.StringToHGlobalAnsi("local");
            }

            // Allocate memory
            IntPtr proxyInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(proxyInfo));

            // Convert structure to IntPtr
            Marshal.StructureToPtr(proxyInfo, proxyInfoPtr, true);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY,
                proxyInfoPtr, Marshal.SizeOf(proxyInfo));
        }

        #region IOleClientSite Members

        public void SaveObject()
        {
            // TODO:  Add Form1.SaveObject implementation
        }

        public void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk)
        {
            // TODO:  Add Form1.GetMoniker implementation
        }

        public void GetContainer(object ppContainer)
        {
        }

        public void ShowObject()
        {
            // TODO:  Add Form1.ShowObject implementation
        }

        public void OnShowWindow(bool fShow)
        {
            // TODO:  Add Form1.OnShowWindow implementation
        }

        public void RequestNewObjectLayout()
        {
            // TODO:  Add Form1.RequestNewObjectLayout implementation
        }

        #endregion

        #region IServiceProvider Members

        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            int nRet = guidService.CompareTo(IID_IAuthenticate);
            if (nRet == 0)
            {
                nRet = riid.CompareTo(IID_IAuthenticate);
                if (nRet == 0)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(IAuthenticate));
                    return S_OK;
                }
            }

            ppvObject = new IntPtr();
            return INET_E_DEFAULT_ACTION;
        }

        #endregion

        #region IAuthenticate Members

        public int Authenticate(ref IntPtr phwnd, ref IntPtr pszUsername, ref IntPtr pszPassword)
        {
            IntPtr sUser = Marshal.StringToCoTaskMemAuto(_currentUsername);
            IntPtr sPassword = Marshal.StringToCoTaskMemAuto(_currentPassword);
            pszUsername = sUser;
            pszPassword = sPassword;
            return S_OK;
        }

        #endregion
    }

    public struct INTERNET_PROXY_INFO
    {
        public int dwAccessType;
        public IntPtr proxy;
        public IntPtr proxyBypass;
    }

    #region COM Interfaces

    [ComImport, Guid("00000112-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleObject
    {
        void SetClientSite(IOleClientSite pClientSite);
        void GetClientSite(IOleClientSite ppClientSite);
        void SetHostNames(object szContainerApp, object szContainerObj);
        void Close(uint dwSaveOption);
        void SetMoniker(uint dwWhichMoniker, object pmk);
        void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk);
        void InitFromData(IDataObject pDataObject, bool
            fCreation, uint dwReserved);
        void GetClipboardData(uint dwReserved, IDataObject ppDataObject);
        void DoVerb(uint iVerb, uint lpmsg, object pActiveSite,
            uint lindex, uint hwndParent, uint lprcPosRect);
        void EnumVerbs(object ppEnumOleVerb);
        void Update();
        void IsUpToDate();
        void GetUserClassID(uint pClsid);
        void GetUserType(uint dwFormOfType, uint pszUserType);
        void SetExtent(uint dwDrawAspect, uint psizel);
        void GetExtent(uint dwDrawAspect, uint psizel);
        void Advise(object pAdvSink, uint pdwConnection);
        void Unadvise(uint dwConnection);
        void EnumAdvise(object ppenumAdvise);
        void GetMiscStatus(uint dwAspect, uint pdwStatus);
        void SetColorScheme(object pLogpal);
    }

    [ComImport, Guid("00000118-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleClientSite
    {
        void SaveObject();
        void GetMoniker(uint dwAssign, uint dwWhichMoniker, object ppmk);
        void GetContainer(object ppContainer);
        void ShowObject();
        void OnShowWindow(bool fShow);
        void RequestNewObjectLayout();
    }

    [ComImport, Guid("6d5140c1-7436-11ce-8034-00aa006009fa"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    ComVisible(false)]
    public interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
    }

    [ComImport, Guid("79EAC9D0-BAF9-11CE-8C82-00AA004BA90B"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    ComVisible(false)]
    public interface IAuthenticate
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int Authenticate(ref IntPtr phwnd, ref IntPtr pszUsername, ref IntPtr pszPassword);
    }

    #endregion
}
