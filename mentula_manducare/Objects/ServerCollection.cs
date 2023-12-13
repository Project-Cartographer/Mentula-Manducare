using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace mentula_manducare.Objects
{
    public class ServerCollection : System.Collections.CollectionBase
    {
        public ServerContainer this[int index] => 
            (ServerContainer) List[index];

        public ServerContainer this[string instance]
        {
            get
            {
                foreach(ServerContainer server in List)
                {
                    if (server.Instance == instance)
                        return server;
                }
                return null;
            }
        }

        public int ValidCount =>
            List.Cast<ServerContainer>().Select(x => x.Name != "").Count();

        public ServerContainer this[Guid guid] 
            => (ServerContainer) List.Cast<object>().SingleOrDefault(
                o => guid == ((ServerContainer)o).WebGuid);

        public void Add(ServerContainer server) =>
            List.Add(server);

        public bool ServerCollected(Process serverProcess) => 
            List.Cast<object>().Any(o => serverProcess.Id == ((ServerContainer) o).ServerProcess.Id);

        public List<ServerContainer> AsList =>
            (List<ServerContainer>) List;
    }
}
