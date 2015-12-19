using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.IO;

namespace Client2ch
{
    public class ModInfo : IUserMod
    {
        public const string SETTINGFILENAME = "2chForChirpy.xml";
        internal static string MODNAME = "2ch for Chirpy";

        public static string configPath;

        public static string[] ClickBehaviourValues = new string[]
        {
            "Steamオーバーレイ上で開く",
            "URLをクリップボードにコピーする",
            "Webブラウザで開く",
            "何もしない",
        };

        public string Description
        {
            get { return "Show what's new on 2ch"; }
        }

        public string Name
        {
            get { return MODNAME; }
        }

        public static ModConfiguration ModConf;

        public void OnSettingsUI(UIHelperBase helper)
        {
            this.InitConfigFile();
            UIHelperBase group = helper.AddGroup("投稿表示設定");
            group.AddSlider("通信間隔", 1, 10, 0.5f, ModConf.TimerInMinutes, delegate (float n)
            {
                ModInfo.ModConf.TimerInMinutes = n;
                ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf);
            });
            group.AddSpace(10);
            UITextField textField = (UITextField)group.AddTextfield("NGワード(半角スペース区切りで入力)", string.Join(" ",ModConf.NGWords), delegate (string text)
            {
                ModInfo.ModConf.NGWords = text.Split(new string[] { " " }, StringSplitOptions.None);
                ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf);
            });
            textField.width *= 3f;
            group.AddCheckbox("AAを含むと思われる投稿を非表示", ModConf.FilterAA, delegate (bool isChecked)
            {
                ModInfo.ModConf.FilterAA = isChecked;
                ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf);
            });
            group.AddSpace(10);
            UIDropDown dropDown = (UIDropDown)group.AddDropdown("投稿をクリックした際の挙動", ModInfo.ClickBehaviourValues, ModConf.ClickBehaviourIndex, delegate (int c)
            {
                ModInfo.ModConf.ClickBehaviourIndex = c;
                ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf);
            });
            dropDown.width *= 2.5f;
            dropDown.listWidth = (int)dropDown.width;

        }

        private void InitConfigFile()
        {
            try
            {
                string pathName = GameSettings.FindSettingsFileByName("gameSettings").pathName;
                string str = "";
                if (pathName != "")
                {
                    str = Path.GetDirectoryName(pathName) + Path.DirectorySeparatorChar;
                }
                ModInfo.configPath = str + SETTINGFILENAME;
                ModInfo.ModConf = ModConfiguration.Deserialize(ModInfo.configPath);
                if (ModInfo.ModConf == null)
                {
                    ModInfo.ModConf = ModConfiguration.Deserialize(SETTINGFILENAME);
                    if (ModInfo.ModConf != null && ModConfiguration.Serialize(str + SETTINGFILENAME, ModInfo.ModConf))
                    {
                        try
                        {
                            File.Delete(SETTINGFILENAME);
                        }
                        catch
                        {
                        }
                    }
                }
                if (ModInfo.ModConf == null)
                {
                    ModInfo.ModConf = new ModConfiguration();
                    if (!ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf))
                    {
                        ModInfo.configPath = SETTINGFILENAME;
                        ModConfiguration.Serialize(ModInfo.configPath, ModInfo.ModConf);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
