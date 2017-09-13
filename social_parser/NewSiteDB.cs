using System;
using System.Collections.Generic;
using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
using SocialParser.Sourcess;

namespace SocialParser
{
    public class NewSiteDB
    {
        //private static BinaryFormatter formatter = new BinaryFormatter();
        public static List<NewsSiteWithWiewCount> DB;
        private const string DBname = "SitesDB.txt";

        public static NewsSiteWithWiewCount GetSite(string adress)
        {
            foreach (var site in DB)
                if (site.Adress == adress)
                    return site;
            throw new ArgumentOutOfRangeException();
        }

        /*public static void SaveDB()
        {
            using (FileStream stream = new FileStream(DBname, FileMode.Create))
            {
                formatter.Serialize(stream, DB);
                stream.Close();
            }
        }*/
        public static void LoadDB()
        {
            if (File.Exists(DBname))
            {
                DB = new List<NewsSiteWithWiewCount>();
                using (StreamReader sr = new StreamReader(DBname))
                {
                    while (!sr.EndOfStream)
                    {
                        var temp = sr.ReadLine().Split('|');
                        DB.Add(new NewsSiteWithWiewCount(temp[0], temp[1], int.Parse(temp[2]) - 1));
                    }
                }
                /*try
                {
                    using (FileStream stream = new FileStream(DBname, FileMode.Open))
                    {
                        DB = (List<NewsSiteWithWiewCount>) formatter.Deserialize(stream);
                        stream.Close();
                    }
                    return;
                }
                catch (SerializationException e)
                {
                    ErrorManager.LogError(e);
                }*/
            }
            else DB = new List<NewsSiteWithWiewCount>();
        }
    }
}