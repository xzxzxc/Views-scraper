using System;
using System.Configuration;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace SocialParser.Sourcess
{
    public class Youtube:MetricsSource
    {
        private readonly YouTubeService youtubeService;
        public Youtube()
        {
            youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = ConfigurationManager.AppSettings["youtube_api_key"],
                ApplicationName = "parser"
            });
        }
        public override Metrics GetMetrics(string href)
        {
            base.GetMetrics(href);
            var videos = youtubeService.Videos.List("id, statistics");
            videos.Id = href.Substring(href.IndexOf("?v=", StringComparison.Ordinal) + 3, 11);
            var result = videos.Execute();
            if (result.Items.Count > 0)
            {
                var statisticsViewCount = result.Items[0].Statistics.ViewCount;
                if (statisticsViewCount != null) return new Metrics((ulong) statisticsViewCount, "Youtube views count");
            }
            else
                throw new ArgumentException("Bad href");
            return new Metrics();
        }
        protected override bool IsGoodHref(string href)
        {
            return href.Contains("www.youtube.com/watch?v=");
        }
    }
}