using System;
using System.Collections.Generic;

namespace SocialParser
{
    public static class UserAgentManager
    {
        private static List<string> agents = new List<string>
        {
        "Mozilla/5.0 (iPad; CPU OS 7_0 like Mac OS X) AppleWebKit/537.51.1 (KHTML, like Gecko) Version/7.0 Mobile/11A465 Safari/9537.53", // ipad safari
        "Mozilla/5.0 (iPhone; CPU iPhone OS 7_0_2 like Mac OS X) AppleWebKit/537.51.1 (KHTML, like Gecko) Version/7.0 Mobile/11A501 Safari/9537.53", // iphone safari
        "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36", // chrome
        //"Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1", // firefox
        "Opera/9.80 (Windows NT 6.0) Presto/2.12.388 Version/12.14", // opera
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.75.14 (KHTML, like Gecko) Version/7.0.3 Safari/7046A194A" // safari
        };
        private static Random rnd = new Random();
        public static string GetRandomAgent()
        {
            return agents[rnd.Next(agents.Count - 1)];
        }

        public static string GetChromeAgent()
        {
            return agents[2];
        }
    }
}