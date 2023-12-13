using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace mentula_manducare.Objects
{
    public class User
    {
        public string Username { get; set; }
        
        public string Password { get; set; }

        [XmlIgnore]
        public string Token { get; set; }
    }
}
