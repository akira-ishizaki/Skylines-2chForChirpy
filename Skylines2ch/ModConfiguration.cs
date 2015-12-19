using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Client2ch
{
	public class ModConfiguration
	{
		public int ClickBehaviourIndex;

        public bool FilterAA;

        public string[] NGWords;

        public float TimerInMinutes;

        public ModConfiguration()
		{
			this.ClickBehaviourIndex = 0;
            this.FilterAA = true;
            this.NGWords = new string[0];
            this.TimerInMinutes = 1;
        }

		public static bool Serialize(string filename, ModConfiguration config)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfiguration));
			try
			{
				using (StreamWriter streamWriter = new StreamWriter(filename))
				{
					xmlSerializer.Serialize(streamWriter, config);
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		public static ModConfiguration Deserialize(string filename)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfiguration));
			try
			{
				using (StreamReader streamReader = new StreamReader(filename))
				{
					ModConfiguration modConfiguration = (ModConfiguration)xmlSerializer.Deserialize(streamReader);
                    if (modConfiguration.ClickBehaviourIndex < 0 || ModInfo.ClickBehaviourValues.Length <= modConfiguration.ClickBehaviourIndex)
                    {
						modConfiguration.ClickBehaviourIndex = 0;
					}
					return modConfiguration;
				}
			}
			catch
			{
			}
			return null;
		}
	}
}
