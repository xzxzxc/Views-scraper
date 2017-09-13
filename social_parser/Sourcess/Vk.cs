using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;

namespace SocialParser.Sourcess
{
    public class VK:ParsableMetricsSource
    {
        private VkApi vk;
        private WallGetObject wallPosts;
        private LoginForm loginForm;
        private bool isLogined;

        public VK()
        {
            var capchaSolver = new CapchaSolver();
            vk = new VkApi(capchaSolver);
            loginForm = new LoginForm("Login into Vk.com");
            (new Task(Autorize)).Start();
        }

        protected override Metrics GetMetricsFromApi(string href)
        {
            Thread.Sleep(TimeSpan.FromSeconds(rnd.Next(2, 10)));
            try
            {
                string id = GetPostId(href).Split('_')[0];
                if (id[0] == '-') // It`s group
                {
                    id = id.Substring(1);
                    var groups = vk.Groups.GetById(new [] {id}, id, GroupsFields.MembersCount);
                    var membersCount = groups[0]?.MembersCount;
                    if (membersCount != null) return new Metrics((ulong) membersCount, "Vk group members count");
                    return new Metrics();
                }
                var user = vk.Users.Get(id, ProfileFields.Counters);
                var metrics = user?.Counters.Friends + user?.Counters.Followers;
                if (metrics != null) return new Metrics((ulong) metrics, "VK friends+follovers count");
                return new Metrics();
            }
            catch (AccessTokenInvalidException)
            {
                Autorize();
                return GetMetricsFromApi(href);
            }

        }

        protected override Metrics GetMetricsFromWeb(string href)
        {
            Uri postInfoUri;
            string postId = GetPostId(href);
            postInfoUri =
                new Uri($"http://vk.com/like.php?act=a_get_stats&al=1&al_ad=0&object=wall{postId}&views=1");
            string html = GetHtml(postInfoUri);
            if (html.Contains("-->")) html = html.Substring(0, html.IndexOf("-->", StringComparison.Ordinal));
            var spl = html.Split(new[] {"<!>"}, StringSplitOptions.None);
            var viewFullStr = spl.Last();
            if (!viewFullStr.Contains(' '))
                throw new ArgumentException("Bad href");
            string views = viewFullStr.Split(' ')[0];
            return new Metrics(ulong.Parse(views), "VK post views count");
        }

        protected override bool IsFromApi(string href)
        {
            if (!isLogined) return false;
            try
            {
                wallPosts = vk.Wall.GetById(new[] { GetPostId(href)});
                if (wallPosts?.WallPosts.Count > 0)
                {
                    var post = wallPosts.WallPosts[0];
                    return post.Date < new DateTime(2017, 2, 1);
                }
                return false;
            }
            catch (AccessTokenInvalidException)
            {
                Autorize();
                return IsFromApi(href);
            }
        }

        private string GetPostId(string href)
        {
            if (href.Contains("vk.com/wall"))
            {
                href = StringManipulations.ClearHref(href);
                return href.Substring(href.IndexOf("vk.com/wall", StringComparison.Ordinal) + 11);
            }
            return href.Substring(href.IndexOf("=wall", StringComparison.Ordinal) + 5);
        }

        public void Autorize()
        {
            if (loginForm.Visible)
                return;
            Settings scope = Settings.All;
            string email, pass;
            while (true)
            {
                isLogined = loginForm.IsLogining(out email, out pass);
                if (isLogined)
                {
                    try
                    {
                        vk.Authorize(new ApiAuthParams
                        {
                            ApplicationId = ulong.Parse(ConfigurationManager.AppSettings["vk_appID"]),
                            Login = email,
                            Password = pass,
                            Settings = scope
                        });
                        break;
                    }
                    catch (VkApiAuthorizationException)
                    {
                        MessageBox.Show("Login or password is incorect");
                    }
                    catch (Exception e)
                    {
                        ErrorManager.LogError(e);
                        break;
                    }
                }
                else
                    break;
            }
        }

        protected override void TryAvoidBan()
        {
            
        }

        protected override bool IsGoodHref(string href)
        {
            return href.Contains("vk.com") && (href.Contains("/wall") || href.Contains("=wall"));
        }
    }
}