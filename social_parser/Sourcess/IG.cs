using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace SocialParser.Sourcess
{
    public class IG:ParsableMetricsSource
    {
        protected override Metrics GetMetricsFromWeb(string href){
            href = StringManipulations.ClearHref(href);
            Uri postInfoUri = new Uri($"http://api.instagram.com/oembed?url={href}");
            string username;
            int count = 0;
            while (true)
            {
                try
                {
                    string html = webClient.DownloadString(postInfoUri);
                    if (html.Contains("No Media Match") || html.Contains("No URL Match"))
                        throw new ArgumentException("Bad href");
                    Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(html);
                    username = values["author_name"];
                    break;
                }
                catch (WebException)
                {
                    if (count == 3)
                        throw new ArgumentException("Bad href");
                    count++;
                }
                catch (Exception e)
                {
                    ErrorManager.LogError(e);
                    throw new ArgumentException("Bad href");
                }
            }
            if (username == null) return new Metrics();
            Uri userInfoUri = new Uri($"http://www.instagram.com/web/search/topsearch/?query={username}");
            count = 0;
            while (true)
            {
                try
                {
                    string html = webClient.DownloadString(userInfoUri);
                    Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(html);

                    List<Dictionary<string, object>> values2 =
                        JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(values["users"].ToString());
                    Dictionary<string, object> user =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(values2[0]["user"].ToString());
                    return new Metrics(ulong.Parse(user["follower_count"].ToString()), "Instagram followers count");

                }
                catch (WebException)
                {
                    if (count == 3)
                        throw new ArgumentException("Bad href");
                    count++;
                }
                catch (Exception e)
                {
                    ErrorManager.LogError(e);
                    throw new ArgumentException("Bad href");
                }
            }
        }

        protected override bool IsGoodHref(string href)
        {
            return href.Contains("www.instagram.com/p");
        }

        protected override bool IsFromApi(string href)
        {
            return false;
        }

        protected override void TryAvoidBan()
        {
            
        }

        protected override Metrics GetMetricsFromApi(string href)
        {
            throw new NotImplementedException();
        }
    }
}