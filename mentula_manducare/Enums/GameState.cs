using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mentula_manducare.Enums
{
    public enum GameState : byte
    {
        Lobby = 1,
        Starting = 2,
        InGame = 3,
        PostGame = 4,
        MatchMaking = 5,
        Unknown = 0
    }
   
}
