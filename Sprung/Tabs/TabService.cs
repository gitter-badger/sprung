﻿using Nancy;
using Nancy.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sprung.Tabs
{
    public class TabService : NancyModule
    {
        // TODO per autofac einbinden
        private WindowManager windowManager = WindowManager.GetInstance();

        public TabService()
        {
            Post["/chrome"] = SetChromeTabs;
        }

        private string SetChromeTabs(dynamic parameters)
        {
            string body = this.Request.Body.AsString();
            JArray tabs = JArray.Parse(body);

            lock (windowManager)
            {
                List<TabWindow> tabList = new List<TabWindow>();

                foreach (JObject tab in tabs)
                {
                    TabWindow tabWindow = JsonConvert.DeserializeObject<TabWindow>(tab.ToString());
                    tabList.Add(tabWindow);
                }

                Dictionary<string, int> windowTitleToWindowId = new Dictionary<string, int>();
                IntPtr handle = IntPtr.Zero;
                
                TabWindow currentTab = tabList.Where(tab => tab.IsCurrent).FirstOrDefault();

                if (currentTab == null)
                {
                    return string.Empty;
                }

                string currentTabTitle = currentTab.RawTitle;
                int currentTabIndex = currentTab.Index;

                foreach(Window window in windowManager.getWindows())
                {
                    string titleWithoutProgramName = window.RawTitle.Replace(" - Google Chrome", "");
                    if (currentTabTitle == titleWithoutProgramName)
                    {
                        handle = window.Handle;
                    }
                }

                // Passiert wenn zu einem Tab geswitched wird, dann stimmt der Titel nicht mehr mit dem aktuellen Tab ueberein
                if (handle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                foreach (TabWindow tab in tabList)
                {
                    tab.Handle = handle;
                    int processId = tab.getWindowProcessId(tab.Handle.ToInt32());
                    tab.Process = Process.GetProcessById(processId);
                    tab.ProcessName = "chrome";
                    tab.Title = tab.RawTitle + " - Google Chrome";
                    tab.CurrentTabIndex = currentTabIndex;
                }

                if (tabList.Count == 0 && windowManager.Tabs.ContainsKey(handle))
                {
                    windowManager.Tabs.Remove(handle);
                }
                else
                {
                    windowManager.Tabs[handle] = tabList;
                }
            }

            return string.Empty;
        }
    }
}
