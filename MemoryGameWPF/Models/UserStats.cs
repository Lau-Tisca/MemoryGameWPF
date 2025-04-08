using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF.Models
{
    public class UserStats
    {
        // Properties to store statistics for a single user
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }

        // Add a parameterless constructor for JSON deserialization
        public UserStats()
        {
            GamesPlayed = 0;
            GamesWon = 0;
        }
    }
}
