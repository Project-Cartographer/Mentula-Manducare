using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MentulaManducare;

namespace mentula_manducare.Objects
{
    public class UserCollection : System.Collections.CollectionBase, IEnumerable<User>
    {
        private FileStream xmlFile;

        public UserCollection()
        {
            var filePath = MainThread.BasePath + "users.xml";
            if (!File.Exists(filePath))
            {
                MainThread.WriteLine("No User.xml file found creating new...");
                xmlFile = File.Create(filePath);
                var random = new Random();
                var newPassword = new string(Enumerable
                    .Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+", 15)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                MainThread.WriteLine($"Created New User.xml storage creating Administrator Login");
                Add("Administrator", newPassword);
                MainThread.WriteLine($"Created new Administrator Login");
                MainThread.WriteLine($"Password: {newPassword}");
            }
            else
            {
                xmlFile = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                Load();
            }

        }

        public void Add(string Username, string Password)
        {
            List.Add(new User {Username = Username, Password = EncryptString(Password)});
            Save();
        }

        public void Remove(string Username)
        {
            foreach (User user in List)
            {
                if (user.Username == Username)
                {
                    List.Remove(user);
                    break;
                }
            }
            Save();
        }
        public LoginResult TokenLogin(string Token)
        {
            foreach (User o in List)
            {
                if (o.Token == Token)
                {
                    return new LoginResult
                    {
                        Result = true,
                        UserObject = o
                    };
                }
            }

            return new LoginResult
            {
                Result = false
            };
        }

        public bool ResetPassword(string Username, string Password)
        {
            foreach (User o in List)
            {
                if (o.Username == Username)
                {
                    o.Password = EncryptString(Password);
                    MainThread.WriteLine($"Password Reset for {Username}");
                    return true;
                }
            }

            MainThread.WriteLine($"User {Username} not found");
            return false;
        }

        public LoginResult Login(string Password, string Token)
        {
            var ePassword = EncryptString(Password);
            foreach (User o in List)
            {
                if (o.Password == ePassword)
                {
                    ;
                    o.Token = Token;
                    return new LoginResult
                    {
                        Result = true,
                        UserObject = o
                    };
                }
            }

            return new LoginResult
            {
                Result = false,
            };
        }

        public List<User> AsList =>
            List.Cast<User>().ToList();

        private string EncryptString(string Input)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(Input);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            return System.Convert.ToBase64String(data);
        }

        private void Save()
        {
            var serializer = new XmlSerializer(AsList.GetType());
            xmlFile.SetLength(0);
            xmlFile.Flush();
            serializer.Serialize(xmlFile, AsList);
            xmlFile.Flush();
        }

        private void Load()
        {
            var serializer = new XmlSerializer(typeof(List<User>));
            List<User> Users = (List<User>) serializer.Deserialize(xmlFile);
            foreach (var user in Users)
            {
                List.Add(new User {Username = user.Username, Password = user.Password});
            }
        }

        public new IEnumerator<User> GetEnumerator()
        {
            foreach (User user in List)
            {
                yield return user;
            }
        }
    }

    public class LoginResult
    {
        public bool Result { get; set; }
        public User UserObject { get; set; }
    }
}
