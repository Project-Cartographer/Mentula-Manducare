using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mentula_manducare.Objects
{
    public class PlayerCollection : System.Collections.CollectionBase, IEnumerable<PlayerContainer>
    {
        private ServerContainer Server;
        public PlayerCollection(ServerContainer Server)
        {
            this.Server = Server;
            Reload();
        }

        public void Reload()
        {
            for (var index = 0; index < List.Count; index++)
            {
                var p = (PlayerContainer) List[index];
                if (!p.IsReal)
                    List.RemoveAt(index);
                else
                {

                    p.resolveIndexes();
                }
            }

            for (var i = 0; i < Server.PlayerCount; i++)
            {
                var tPlayer = new PlayerContainer(Server.ServerMemory, i);
                if(!Exists(tPlayer.Name) && tPlayer.IsReal)
                    Add(tPlayer);
            }
        }
        public PlayerContainer this[int index]
        {
            get
            {
                Reload();
                return (PlayerContainer) List[index];
            }
        }

        public PlayerContainer this[string playerName]
        {
            get
            {
                Reload();
                return List.Cast<PlayerContainer>()
                    .FirstOrDefault(playerContainer => playerContainer.Name == playerName);
            }
        }

        public bool Exists(string playerName)
        {
            for (var index = 0; index < List.Count; index++)
            {
                var p = (PlayerContainer)List[index];
                if (p.Name == playerName)
                    return true;
            }

            return false;
        }
        public void Add(PlayerContainer Player) =>
            List.Add(Player);

        public new IEnumerator<PlayerContainer> GetEnumerator()
        {
            Reload();
            var TList = List;
            foreach (PlayerContainer player in TList)
                yield return player;
        }
    }
}
