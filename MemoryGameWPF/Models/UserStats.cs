using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF.Models
{
    public class UserStats
    {
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }

        public UserStats()
        {
            GamesPlayed = 0;
            GamesWon = 0;
        }
    }
}
