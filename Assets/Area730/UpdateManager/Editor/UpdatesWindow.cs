using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Area730.UpdatesManager
{
    [InitializeOnLoad]
    public static class UpdatePlugins
    {
        private static readonly string pluginDescriptorFilename = "pluginInfo.json";
        private static readonly string LastTimeKey = "last_check_time";

        private static readonly string
            pluginDataUrl = "https://www.dropbox.com/s/w8ef4h6nz5ermo7/pluginData.json?raw=1";

        public static List<PluginDesc> pluginsToUpdate;
        private static WWW www;

        private static WWW rateIconWWW;

        static UpdatePlugins()
        {
            if (HasChecked() == false) CheckUpdates();
        }

        private static bool IsFirstRun
        {
            get => EditorPrefs.GetBool("area730_um_first_run", true);

            set => EditorPrefs.SetBool("area730_um_first_run", value);
        }

        public static long CurrentTimeMilis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        [MenuItem("Area730/Check updates")]
        private static void CheckUpdates()
        {
            Debug.Log("[Area730] Started checking updates...");

            www = new WWW(pluginDataUrl);
            EditorApplication.update += CheckPlugins;
        }

        private static void CheckPlugins()
        {
            if (www.isDone)
            {
                EditorApplication.update -= CheckPlugins;

                if (www.error != null)
                {
                    Debug.Log("[Area730] Error reading remote list");
                    return;
                }

                CueckUpdates(www.text);
                SetChecked();
            }
        }


        private static void CueckUpdates(string serverData)
        {
            var serverDescriptors = ParseDescriptions(serverData);
            var localDescriptors = GetLocalDescriptors();

            var remoteForLocals = new List<PluginDesc>();

            pluginsToUpdate = new List<PluginDesc>();

            foreach (var localDesc in localDescriptors)
            {
                PluginDesc remoteDesc;

                if (serverDescriptors.TryGetValue(localDesc.Id, out remoteDesc))
                {
                    remoteForLocals.Add(remoteDesc);

                    if (remoteDesc.Version > localDesc.Version)
                        // localDesc plugin needs update
                        pluginsToUpdate.Add(remoteDesc);
                }
            }

            CheckRate(remoteForLocals);

            UpdatesWindow.ShowWindow();
        }

        private static bool HasChecked()
        {
            var currentTime = CurrentTimeMilis();
            var lastTimeString = EditorPrefs.GetString(LastTimeKey, "0");
            var lastTime = Convert.ToInt64(lastTimeString);
            var delta = TimeSpan.FromMilliseconds(currentTime - lastTime);

            return delta.TotalHours <= 5;
        }

        private static void SetChecked()
        {
            var currentTime = CurrentTimeMilis();
            EditorPrefs.SetString(LastTimeKey, currentTime.ToString());
        }

        private static Dictionary<string, PluginDesc> ParseDescriptions(string json)
        {
            var res = new Dictionary<string, PluginDesc>();

            var rootNode = JSON.Parse(json);

            var plugins = rootNode["PluginList"].AsArray;

            for (var i = 0; i < plugins.Count; ++i)
            {
                var desc = new PluginDesc();
                var node = plugins[i];

                string id = node["pluginId"];
                string version = node["pluginVersion"];
                string pluginName = node["pluginName"];
                string iconUrl = node["iconUrl"];

                desc.Id = id;
                desc.Name = pluginName;
                desc.Version = float.Parse(version);
                desc.IconUrl = iconUrl;

                res.Add(desc.Id, desc);
            }

            return res;
        }


        private static string[] GetPluginsDescriptorPaths()
        {
            var dirs = Directory.GetDirectories(Path.Combine("Assets", "Area730"));

            var descriptors = new List<string>();

            foreach (var dir in dirs)
            {
                var descriptorPath = Path.Combine(dir, pluginDescriptorFilename);
                if (File.Exists(descriptorPath)) descriptors.Add(descriptorPath);
            }

            return descriptors.ToArray();
        }

        private static List<PluginDesc> GetLocalDescriptors()
        {
            var res = new List<PluginDesc>();

            var files = GetPluginsDescriptorPaths();

            for (var i = 0; i < files.Length; ++i)
            {
                var filePath = files[i];
                var json = File.ReadAllText(filePath);

                var rootNode = JSON.Parse(json);
                string id = rootNode["pluginId"];
                string version = rootNode["pluginVersion"];

                var desc = new PluginDesc();
                desc.Id = id;
                desc.Version = float.Parse(version);

                res.Add(desc);
            }

            return res;
        }

        private static void ShowRateWindowRandom(List<PluginDesc> items)
        {
            var notRated = new List<PluginDesc>();

            foreach (var item in items)
                if (IsAppRated(item.Id) == false)
                    notRated.Add(item);

            if (notRated.Count > 0)
            {
                var index = Random.Range(0, notRated.Count);

                ShowRateWindowForApp(notRated[index]);
            }
        }

        private static void CheckRate(List<PluginDesc> items)
        {
            if (IsFirstRun)
                IsFirstRun = false;
            else
                ShowRateWindowRandom(items);
        }

        private static void ShowRateWindowForApp(PluginDesc desc)
        {
            RateWindow.desc = desc;

            rateIconWWW = new WWW(desc.IconUrl);
            EditorApplication.update += DownloadAppIcon;
        }

        private static void DownloadAppIcon()
        {
            if (rateIconWWW.isDone)
            {
                EditorApplication.update -= DownloadAppIcon;

                if (rateIconWWW.error != null)
                {
                    Debug.Log("[Area730] Error downloading rate icon");
                }
                else
                {
                    RateWindow.tex = rateIconWWW.texture;
                    RateWindow.ShowWindow();
                }
            }
        }

        private static bool IsAppRated(string appId)
        {
            return EditorPrefs.GetBool("area730_is_app_rated_" + appId, false);
        }

        public static void SetAppRated(string appId)
        {
            EditorPrefs.SetBool("area730_is_app_rated_" + appId, true);
        }


        public class PluginDesc
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public float Version { get; set; }
            public string IconUrl { get; set; }

            public override string ToString()
            {
                return Name + " [" + Id + "] v" + Version;
            }
        }
    }

    public class RateWindow : EditorWindow
    {
        private static readonly int WindowWidth = 250;
        private static readonly int WindowHeight = 130;

        private static readonly string WindowTitle = "Rate us please!";

        public static UpdatePlugins.PluginDesc desc;
        public static Texture2D tex;

        private void OnGUI()
        {
            var guiStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true
            };

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.DrawTexture(new Rect(10, 25, 70, 70), tex, ScaleMode.StretchToFill);

            GUI.Label(new Rect(85, 6, 150, 70), "<size=13><b>We improve plugins\n based on your\n reviews!</b></size>",
                labelStyle);

            if (GUI.Button(new Rect(110, 80, 100, 30), "<size=14><b>Rate</b></size>", guiStyle))
            {
                UpdatePlugins.SetAppRated(desc.Id);

                AssetStore.Open("content/" + desc.Id);

                Close();
            }
        }

        public static void ShowWindow()
        {
            if (desc == null || tex == null) return;

            var window = (RateWindow) GetWindow(typeof(RateWindow), true, WindowTitle, true);
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth, WindowHeight);
            window.Show();
        }
    }

    public class UpdatesWindow : EditorWindow
    {
        private static readonly int WindowWidth = 300;
        private static readonly int WindowHeight = 500;
        private static readonly string WindowTitle = "Area730 Update Manager";

        private static readonly string eventTrackUrl =
            "https://labs.area730.com/registerEvent?type=assetstoreUpdatesManager&eventName=";

        private Vector2 _scrollPos = Vector2.zero;

        private void OnGUI()
        {
            var guiStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true
            };

            var y = 15;
            const int x = 10;
            var width = WindowWidth - x * 2;
            const int scrollAreaHeight = 150;


            if (UpdatePlugins.pluginsToUpdate.Count > 0)
            {
                const int btnHeight = 40;
                const int btnSpace = 7;
                var scrollY = 0;

                var scrollHeight = UpdatePlugins.pluginsToUpdate.Count * (btnHeight + btnSpace);

                // Make sure scrollbar is always visible
                if (scrollHeight < scrollAreaHeight + 2) scrollHeight = scrollAreaHeight + 2;

                var scrollWidth = width - 10;
                _scrollPos = GUI.BeginScrollView(new Rect(x, y, width + 10, scrollAreaHeight), _scrollPos,
                    new Rect(0, 0, scrollWidth, scrollHeight));


                for (var i = 0; i < UpdatePlugins.pluginsToUpdate.Count; ++i)
                {
                    var desc = UpdatePlugins.pluginsToUpdate[i];

                    if (GUI.Button(new Rect(0, scrollY, scrollWidth, btnHeight),
                        "<size=11><b>Update</b>\n" + desc.Name + "</size>", guiStyle))
                    {
                        AssetStore.Open("content/" + desc.Id);
                        RegisterEvent("pluginUpdatePress");
                    }

                    scrollY += btnHeight + btnSpace;
                }

                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(new Rect(x, 40, width, 100), "<size=15><b>All plugins are up-to-date</b></size>", guiStyle);
            }

            // Start new area with promo
            y = 170;

            GUI.DrawTexture(new Rect(0, y, WindowWidth, 3), GetWhiteTex());


            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle.normal.textColor = guiStyle.normal.textColor;

            y += 10;
            if (GUI.Button(new Rect(x, y, width, 80), ""))
            {
                AssetStore.Open("content/45507");
                RegisterEvent("promo1Press");
            }

            // button texture 1
            GUI.DrawTexture(new Rect(x + 10, y + 5, 70, 70), GetPromo1Tex(), ScaleMode.StretchToFill);
            GUI.Label(new Rect(x + 90, y + 4, width - 100, 72),
                "<size=15><b>Android\nNotifications:</b></size><size=15>\nLocal and Push</size>", labelStyle);


            y += 90;
            if (GUI.Button(new Rect(x, y, width, 80), ""))
            {
                AssetStore.Open("content/58550");
                RegisterEvent("promo2Press");
            }

            // button texture 2
            GUI.DrawTexture(new Rect(x + 10, y + 5, 70, 70), GetPromo2Tex(), ScaleMode.StretchToFill);
            GUI.Label(new Rect(x + 90, y + 4, width - 100, 72),
                "<size=15><b>iOS\nNotifications:</b></size><size=15>\nLocal and Push</size>", labelStyle);


            y += 90;
            if (GUI.Button(new Rect(x, y, width, 80), ""))
            {
                AssetStore.Open("content/57268");
                RegisterEvent("promo3Press");
            }

            // button texture 3
            GUI.DrawTexture(new Rect(x + 10, y + 5, 70, 70), GetPromo3Tex(), ScaleMode.StretchToFill);
            GUI.Label(new Rect(x + 90, y + 4, width - 100, 72),
                "<size=15><b>Admob Ads\nfor Android, iOS,\nAmazon</b></size><size=15></size>", labelStyle);


            y = WindowHeight - 50;
            GUI.DrawTexture(new Rect(0, y, WindowWidth, 3), GetWhiteTex());

            const int socBtnSize = 37;
            const int socBtnSpace = 5;

            var socRect = new Rect(WindowWidth - 10 - socBtnSize, y + 8, socBtnSize, socBtnSize);
            var s = new GUIStyle();

            if (GUI.Button(socRect, GetIconFb(), s))
            {
                Application.OpenURL("https://www.facebook.com/Area730Official/");
                RegisterEvent("socialFbPress");
            }

            EditorGUIUtility.AddCursorRect(socRect, MouseCursor.Link);

            socRect.x -= socBtnSize + socBtnSpace;

            if (GUI.Button(socRect, GetIconTw(), s))
            {
                Application.OpenURL("https://twitter.com/area_730");
                RegisterEvent("socialTwPress");
            }

            EditorGUIUtility.AddCursorRect(socRect, MouseCursor.Link);

            socRect.x -= socBtnSize + socBtnSpace;

            if (GUI.Button(socRect, GetIconVk(), s))
            {
                Application.OpenURL("https://vk.com/area730");
                RegisterEvent("socialVkPress");
            }

            EditorGUIUtility.AddCursorRect(socRect, MouseCursor.Link);

            socRect.x -= socBtnSize + socBtnSpace;

            if (GUI.Button(socRect, GetIconUnity(), s))
            {
                Application.OpenURL(
                    "https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:12354");
                RegisterEvent("socialUnityPress");
            }

            EditorGUIUtility.AddCursorRect(socRect, MouseCursor.Link);
        }

        public static void RegisterEvent(string name)
        {
            var eventRequest = new WWW(eventTrackUrl + name);
        }

        public static void ShowWindow()
        {
            RegisterEvent("windowOpened");
            var window = (UpdatesWindow) GetWindow(typeof(UpdatesWindow), true, WindowTitle, true);
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window.maxSize = new Vector2(WindowWidth, WindowHeight);
            window.Show();
        }

        #region Textures

        private static Texture2D whiteTex;

        private static Texture2D GetWhiteTex()
        {
            if (whiteTex == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texture.SetPixel(0, 0, new Color(1, 1, 1, 0.15f));
                texture.Apply();

                whiteTex = texture;
            }

            return whiteTex;
        }

        private static Texture2D _promo1Tex;

        private static Texture2D GetPromo1Tex()
        {
            if (_promo1Tex == null) _promo1Tex = EditorGUIUtility.Load("promo1.png") as Texture2D;

            return _promo1Tex;
        }

        private static Texture2D _promo2Tex;

        private static Texture2D GetPromo2Tex()
        {
            if (_promo2Tex == null) _promo2Tex = EditorGUIUtility.Load("promo2.png") as Texture2D;

            return _promo2Tex;
        }

        private static Texture2D _promo3Tex;

        private static Texture2D GetPromo3Tex()
        {
            if (_promo3Tex == null) _promo3Tex = EditorGUIUtility.Load("promo3.png") as Texture2D;

            return _promo3Tex;
        }

        private static Texture2D _iconFb;

        private static Texture2D GetIconFb()
        {
            if (_iconFb == null) _iconFb = EditorGUIUtility.Load("fb.png") as Texture2D;

            return _iconFb;
        }

        private static Texture2D _iconVk;

        private static Texture2D GetIconVk()
        {
            if (_iconVk == null) _iconVk = EditorGUIUtility.Load("vk.png") as Texture2D;

            return _iconVk;
        }

        private static Texture2D _iconTw;

        private static Texture2D GetIconTw()
        {
            if (_iconTw == null) _iconTw = EditorGUIUtility.Load("tw.png") as Texture2D;

            return _iconTw;
        }

        private static Texture2D _iconUnity;

        private static Texture2D GetIconUnity()
        {
            if (_iconUnity == null) _iconUnity = EditorGUIUtility.Load("u.png") as Texture2D;

            return _iconUnity;
        }

        #endregion
    }
}