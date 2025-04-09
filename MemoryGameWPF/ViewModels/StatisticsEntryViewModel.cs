using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF.ViewModels
{
    // Simple class, INotifyPropertyChanged not strictly needed if list is populated once
    public class StatisticsEntryViewModel
    {
        public string UserName { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
    }
}
