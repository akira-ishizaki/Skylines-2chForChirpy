using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Client2ch
{
    internal class TinyWeb
    {
        private static Dictionary<int, int> resNums = new Dictionary<int, int>();

        public static IEnumerable<Post> FindLastPosts(string URL)
        {
            var postList = new List<Post>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Timeout = 20000;
            request.Method = WebRequestMethods.Http.Get;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK) return null;

                Stream st = response.GetResponseStream();
                string postListTxt = Utility.ToUnicode(Utility.ReadBinaryData(st));
                st.Close();
                StringReader sr = new StringReader(postListTxt);
                Regex r = new Regex(@"(?<name>.*?)\<\>(?<mail>.*?)\<\>(?<date>.*?)\(.*?\s(?<time>.*?)\sID:(?<user_id>.*?)\.net\<\>\s(?<content>.*?)\s\<\>(?<title>.*)");
                int sc_post_id = 1;
                string separator = Updater2ch.GetSeparator();
                while (sr.Peek() >= 0)
                {
                    // 1 行ずつ読み込む
                    string postStr = sr.ReadLine();
                    Match m = r.Match(postStr);
                    if (m.Success)
                    {
                        resNums.Add(postList.Count + 1, sc_post_id);

                        string content = m.Groups["content"].Value;
                        content = Regex.Replace(content, @"\s?\<br\>\s?", separator);
                        content = Regex.Replace(content, @"\<.*?\>", string.Empty);
                        content = WebUtility.HtmlDecode(content);
                        content = Regex.Replace(content, @"(?<=\>\>(\d+-)?)\d+", new MatchEvaluator(RevertResNum));

                        Post post = new Post
                        {
                            res_num = postList.Count + 1,
                            name = m.Groups["name"].Value,
                            user_id = m.Groups["user_id"].Value,
                            time = DateTime.Parse(m.Groups["date"].Value + " " + m.Groups["time"].Value),
                            content = content,
                        };
                        postList.Add(post);
                    }
                    sc_post_id++;
                }
                sr.Close();
                resNums.Clear();
            }
            if (postList.Count == 0) throw new WebException("Can't get post!");
            return postList;
        }
        
        private static string RevertResNum(Match m)
        {
            int revertedValue;
            if (resNums.TryGetValue(int.Parse(m.Value), out revertedValue))
            {
                return revertedValue.ToString();
            }
            else
            {
                return m.Value;
            }
        }

        public static string getBoardURL(string bbsmenuURL)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(bbsmenuURL);
            request.Timeout = 20000;
            request.Method = WebRequestMethods.Http.Get;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK) return null;
                Stream st = response.GetResponseStream();
                string boardListHtml = Utility.ToUnicode(Utility.ReadBinaryData(st));
                st.Close();

                // <A HREF=http://anago.2ch.sc/game/>PCゲーム</A><br>
                Regex r = new Regex(@"(?<=<A HREF=.?)http.*/(?=.?>PCゲーム</A>)");
                Match m = r.Match(boardListHtml);
                if (m.Success) return m.Value;
            }
            throw new WebException("Can't get BoardURL!");
        }

        public static List<Thread2ch> GetSubjects(string boardURL)
        {
            DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, boardURL + @"subject.txt");
            List<Thread2ch> subjectList = new List<Thread2ch>();
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(boardURL + @"subject.txt");
            request.Timeout = 20000;
            request.Method = WebRequestMethods.Http.Get;
                
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK) return subjectList;

                Stream st = response.GetResponseStream();
                string subjectListTxt = Utility.ToUnicode(Utility.ReadBinaryData(st));
                st.Close();
                
                Regex r = new Regex(@"(?<thread_num>\d*)\.dat\<\>(?<title>.*Skylines.*)\s\(\d*\)", RegexOptions.IgnoreCase);
                MatchCollection mc = r.Matches(subjectListTxt);
                foreach (Match m in mc)
                {
                    Thread2ch subject = new Thread2ch {
                        thread_num = m.Groups["thread_num"].Value,
                        title = m.Groups["title"].Value
                    };
                    subjectList.Add(subject);
                }
            }
            if (subjectList.Count == 0) throw new WebException("Can't get subjects!");
            return subjectList;
        }
    }

    internal class Post
    {
        internal int res_num;
        internal string user_id;
        internal string name;
        internal DateTime time;
        internal string content;
    }

    internal class Thread2ch : IEquatable<Thread2ch>
    {
        internal string thread_num;
        internal string title;

        public bool Equals(Thread2ch other)
        {
            if (other == null)
            {
                return false;
            }

            return thread_num == other.thread_num && title == other.title;
        }
        
        public override int GetHashCode()
        {
            return thread_num.GetHashCode() ^ title.GetHashCode();
        }
    }
}
