using System;
using System.Configuration;
using System.Net;
using System.Windows.Forms;
using TweetSharp;

namespace SocialParser.Sourcess
{
    public class Twitter:MetricsSource
    {
        private TwitterService twitterService;

        public Twitter()
        {
            twitterService = new TwitterService(ConfigurationManager.AppSettings["twitter_consumer_key"],
                ConfigurationManager.AppSettings["twitter_consumer_secret"]);
            twitterService.AuthenticateWith(ConfigurationManager.AppSettings["twitter_access_token"],
                ConfigurationManager.AppSettings["twitter_access_token_secret"]);
        }


        public override Metrics GetMetrics(string href)
        {
            base.GetMetrics(href);
            var id = href.Split('/')[3];
            int count = 0;
            do
            {
                count++;
                var user = twitterService.GetUserProfileFor(new GetUserProfileForOptions() {ScreenName = id});
                // Look for a null object if a real error, or serialization problem, occurred
                if (user == null)
                {
                    // If a real error occurred, you can get it here        
                    TwitterError error = twitterService.Response.Error;
                    if (error != null)
                    {
                        //ErrorManager.LogError(new Exception(error.Message));
                        throw new ArgumentException("Bad href");
                    }
                    continue;
                }
                return new Metrics((ulong) user.FollowersCount, "Twitter followers count");
            } while (twitterService.Response.StatusCode != HttpStatusCode.OK && count<5);
            MessageBox.Show("Can`t connect to twitter");
            return new Metrics();
        }

        protected override bool IsGoodHref(string href)
        {
            return href.Contains("twitter.com") && href.Split('/').Length > 3;
        }
    }
}