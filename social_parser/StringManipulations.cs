using System;

namespace SocialParser
{
    public static class StringManipulations
    {
        public static string ClearHref(string href)
        {
            if (href.Contains("?"))
            {
                return href.Substring(0, href.IndexOf('?'));
            }
            if (href.Contains("&"))
            {
                return href.Substring(0, href.IndexOf('&'));
            }
            return href;
        }

        public static bool IsHref(string href)
        {
            return Uri.IsWellFormedUriString(href, UriKind.Absolute);
        }

        public static string GetOnlyNumbers(string s)
        {
            string result = String.Empty;
            foreach (var c in s)
            {
                int ascii = c;
                if ((ascii >= 48 && ascii <= 57) || ascii == 44 || ascii == 46)
                    result += c;
            }
            return result;
        }
    }
}