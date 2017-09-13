using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SocialParser
{
    public static class ErrorManager
    {
        private static List<string> badHrefs = new List<string>();

        public static void ShowBadHrefs()
        {
            if (badHrefs.Count == 0) return;
            var str = new StringBuilder();
            foreach (var href in badHrefs)
            {
                str.AppendLine(href);
            }
            MessageBox.Show(str.ToString(), "Bad Hrefs");
        }
        public static void ClearBedHrefs()
        {
            badHrefs = new List<string>();
        }
        public static void LogError(Exception error)
        {
#if DEBUG
            MessageBox.Show(error.ToString(), "Error");
#endif
            using (StreamWriter writer = new StreamWriter("errors.txt", true))
            {
                writer.WriteLineAsync($"{DateTime.Now}: {error}");
            }
        }

        public static void ShowError(string error)
        {
            MessageBox.Show(error, "Error");
        }

        public static void LogBadHref(string href)
        {
            badHrefs.Add(href);
        }
    }
}