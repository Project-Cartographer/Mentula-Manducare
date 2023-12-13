using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentulaManducare;
using Newtonsoft.Json;

namespace mentula_manducare.Objects
{
    public class ServerMessageCollection: System.Collections.CollectionBase, IEnumerable<ServerMessage>
    {
        private string instance;
        private FileStream messagesFile;
        private StreamReader reader;
        private StreamWriter writer;

        public ServerMessageCollection(string instance)
        {
            this.instance = instance;
            var filePath = MainThread.BasePath + $"\\Messages\\{instance}.json";
            if (File.Exists(filePath))
            {
                messagesFile = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                reader = new StreamReader(messagesFile);
                writer = new StreamWriter(messagesFile);
                LoadMessages();
            }
            else
            {
                messagesFile = File.Create(filePath);
                reader = new StreamReader(messagesFile);
                writer = new StreamWriter(messagesFile);
            }
        }

        public void AddMessage(string message, string interval)
        {
            List.Add(new ServerMessage(interval, message));
            Save();
        }

        public void RemoveMessage(string message)
        {
            for(var i = 0; i < List.Count; i++)
                if (((ServerMessage) List[i]).message == message)
                {
                    List.RemoveAt(i);
                    Save();
                    break;
                }
        }
        public void LoadMessages()
        {
            try
            {
                reader.BaseStream.Position = 0;
                var Messages = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(reader.ReadToEnd());
                Messages.ForEach(pair => List.Add(new ServerMessage(pair.Value, pair.Key)));
            }
            catch (Exception)
            {

            }
        }
        private void Save()
        {
            writer.BaseStream.Position = 0;
            messagesFile.SetLength(0);
            var tList = (from ServerMessage serverMessage in List select new KeyValuePair<string, string>(serverMessage.message, serverMessage.interval.ToString())).ToList();
            writer.Write(JsonConvert.SerializeObject(tList));
            writer.Flush();
        }

        public new IEnumerator<ServerMessage> GetEnumerator()
        {
            foreach (ServerMessage serverMessage in List)
            {
                yield return serverMessage;
            }
        }
    }
}
