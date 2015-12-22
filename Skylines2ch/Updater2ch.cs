using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Timers;
using UnityEngine;

namespace Client2ch
{
    public class Updater2ch : ChirperExtensionBase
    {
        private Timer timer = new Timer();
        private Dictionary<Thread2ch, int> lastResNums = new Dictionary<Thread2ch, int>();

        private string boardURL_sc = @"http://anago.2ch.sc/game/";
        private string boardURL_net = @"http://potato.2ch.net/game/";
        private const string BBSMENU_URL_SC = @"http://2ch.sc/bbsmenu.html";
        private const string BBSMENU_URL_NET = @"http://menu.2ch.net/bbsmenu.html";
        private bool webexceptionFlag = false;

        private CitizenMessage lastCitizenMessage = null;
        public Message last2chMessage = null;
        private DateTime startTime = DateTime.Now;
        private DateTime lastsubjectsupdatetime = DateTime.Now;

        private bool IsPaused
        {
            get
            {
                return SimulationManager.instance.SimulationPaused;
            }
        }
        
        private static bool IsChirpyBannerActive()
        {
            foreach (PluginManager.PluginInfo current in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (current.name == "406623071" && current.publishedFileID.ToString() == "406623071")
                {
                    return current.isEnabled;
                }
            }
            return false;
        }

        public static string GetSeparator()
        {
            if (IsChirpyBannerActive()) return " ";
            return "\n";
        }

        public override void OnCreated(IChirper threading)
        {
            startTime = DateTime.Now;
            try
            {
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler((sender, e) => Update2chPosts());
                timer.Interval = 60 * 1000 * ModInfo.ModConf.TimerInMinutes;
                timer.Start();
            }
            catch (Exception e)
            {
                Utility.Log(e.Message);
            }
        }

        public override void OnReleased()
        {
            timer.Stop();
            timer.Dispose();
        }

        private void Update2chPosts()
        {
            try
            {
                if (IsPaused) return;

                if (webexceptionFlag)
                {
                    boardURL_sc = TinyWeb.getBoardURL(BBSMENU_URL_SC);
                }

                if (lastResNums == null || lastResNums.Count == 0 || (DateTime.Now - lastsubjectsupdatetime).TotalHours >= 1 || webexceptionFlag)
                {
                    boardURL_net = TinyWeb.getBoardURL(BBSMENU_URL_NET);
                    List<Thread2ch> subjectList = TinyWeb.GetSubjects(boardURL_sc);
                    foreach (Thread2ch thread2ch in subjectList)
                    {
                        if (!lastResNums.ContainsKey(thread2ch))
                            lastResNums.Add(thread2ch, 0);
                    }
                    for (int i = lastResNums.Count - 1; i >= 0; i--)
                    {
                        Thread2ch thr = Enumerable.ToList(lastResNums.Keys)[i];
                        if (!subjectList.Contains(thr))
                        {
                            lastResNums.Remove(thr);
                        }
                    }
                    lastsubjectsupdatetime = DateTime.Now;
                }

                webexceptionFlag = false;

                // ランダムでスレッドを選択
                Thread2ch thread = Enumerable.ToList(lastResNums.Keys)[new System.Random().Next(lastResNums.Count)];
                int lastResNum = lastResNums[thread];

                IEnumerable<Post> newestPosts = TinyWeb.FindLastPosts(boardURL_sc + "dat/" + thread.thread_num + ".dat");
                foreach (Post newestPost in newestPosts)
                {
                    if (lastResNum < newestPost.res_num && !ShouldFilterPost(thread.title, newestPost))
                    {
                        AddMessage(new Message(thread.title, string.Format("{0}:{1} {2} ID:{3}" + GetSeparator() + "{4}", newestPost.res_num, newestPost.name, newestPost.time.ToString("MM/dd HH:mm"), newestPost.user_id, Utility.ChangeToWrappable(newestPost.content)), thread.thread_num + @"/" + newestPost.res_num));
                        lastResNums[thread] = newestPost.res_num;
                        return;
                    }
                }
            }
            catch (WebException webe)
            {
                Debug.LogException(webe);
                webexceptionFlag = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                timer.Interval = 60 * 1000 * ModInfo.ModConf.TimerInMinutes;
                timer.Start();
            }
        }

        private void AddMessage(Message m)
        {
            if (IsPaused)
                return;

            MessageManager.instance.QueueMessage(m);
        }

        private bool ShouldFilterPost(string threadtitle, Post post)
        {
#if DEBUG
#else
            if (post.time < startTime) return true;
#endif
            if (ModInfo.ModConf.FilterAA)
            {
                if(post.content.Contains(" 　") || post.content.Contains("　 ")) return true;
            }
            foreach(string NGword in ModInfo.ModConf.NGWords)
            {
                if (string.IsNullOrEmpty(NGword)) continue;
                if (threadtitle.Contains(NGword) || post.name.Contains(NGword) || post.content.Contains(NGword)) return true;
            }
            return false;
        }


        public override void OnNewMessage(IChirperMessage message)
        {
            CitizenMessage cm = message as CitizenMessage;
            if (cm != null)
            {
                lastCitizenMessage = cm;
            }
            else if (message is Message)
            {
                last2chMessage = message as Message;
            }
        }

        private void Click2chChirp(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!string.IsNullOrEmpty(component.stringUserData) && UIMouseButtonExtensions.IsFlagSet(eventParam.buttons, UIMouseButton.Left))
            {
                string url = boardURL_net.Replace(@".2ch.net/", @".2ch.net/test/read.cgi/") + component.stringUserData;

                switch (ModInfo.ModConf.ClickBehaviourIndex)
                {
                    case 0:
                        // Open Steam Overlay
                        Steam.ActivateGameOverlayToWebPage(url);
                        break;

                    case 1:
                        // Copy to Clipboard
                        Clipboard.text = url;
                        break;

                    case 2:
                        // Open system browser
                        Application.OpenURL(url);
                        break;
                    case 3:
                        // Nothing
                        break;
                }
            }
        }

        public override void OnUpdate()
        {
            if (lastCitizenMessage == null && last2chMessage == null)
                return;

            // This code is roughly based on the work by Juuso "Zuppi" Hietala.
            var container = ChirpPanel.instance.transform.FindChild("Chirps").FindChild("Clipper").FindChild("Container").gameObject.transform;
            for (int i = 0; i < container.childCount; ++i)
            {
                var elem = container.GetChild(i);
                var label = elem.GetComponentInChildren<UILabel>();
                if (last2chMessage != null)
                {
                    if (label.text.Equals(last2chMessage.GetText()) && string.IsNullOrEmpty(label.stringUserData))
                    {
                        label.stringUserData = last2chMessage.GetURL();
                        label.eventClick += Click2chChirp;

                        last2chMessage = null;
                    }
                }

                if (lastCitizenMessage != null)
                {
                    if (label.text.Equals(lastCitizenMessage.GetText()))
                    {
                        lastCitizenMessage = null;
                    }
                }
            }
        }

        /// <summary>
        /// Resolve private assembly fields
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private T GetPrivateVariable<T>(object obj, string fieldName)
        {
            return (T)obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        }
    }
}
