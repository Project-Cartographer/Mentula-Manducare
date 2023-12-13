using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using MentulaManducare;
using Newtonsoft.Json;

namespace mentula_manducare.Objects
{
    public class SettingsCollection : System.Collections.CollectionBase, IEnumerable<KeyValuePair<string, string>>
    {
        private FileStream settingsFile;
        private StreamReader reader;
        private StreamWriter writer;
        public SettingsCollection(string settingsStore)
        {
            var filePath = MainThread.BasePath + $"\\Settings\\{settingsStore}.json";
            if (File.Exists(filePath))
            {
                settingsFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                reader = new StreamReader(settingsFile);
                writer = new StreamWriter(settingsFile);
                Load();
            }
            else
            {
                settingsFile = File.Create(filePath);
                reader = new StreamReader(settingsFile);
                writer = new StreamWriter(settingsFile);
            }

        }

        public string GetSetting(string key, string defaultReturn)
        {
            
            foreach (KeyValuePair<string, string> keyValuePair in List)
                if (keyValuePair.Key == key)
                {
                    return keyValuePair.Value;
                }

            return defaultReturn;
        }
        public void AddSetting(string name, string value)
        {
            if(AsList.Count(x=> x.Key == name) == 0)
                List.Add(new KeyValuePair<string, string>(name, value));
            else
            {
                for (var index = 0; index < List.Count; index++)
                {
                    var keyValuePair = (KeyValuePair<string, string>) List[index];
                    if (keyValuePair.Key == name)
                    {
                        this.List.RemoveAt(index);
                        List.Add(new KeyValuePair<string, string>(name, value));
                    }
                }
            }
            Save();
        }
        private void Load()
        {
            try
            {
                reader.BaseStream.Position = 0;
                var Settings = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(reader.ReadToEnd());
                Settings.ForEach(pair => List.Add(pair));
            }
            catch (Exception)
            {

            }
        }

        private void Save()
        {
            writer.BaseStream.Position = 0;
            settingsFile.SetLength(0);
            var Settings = List.Cast<KeyValuePair<string, string>>().ToList();
            writer.Write(JsonConvert.SerializeObject(Settings));
            writer.Flush();
        }
        private List<KeyValuePair<string, string>> AsList =>
            List.Cast<KeyValuePair<string, string>>().ToList();
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string,string> keyValuePair in List)
            {
                yield return keyValuePair;
            }
        }
    }
}
