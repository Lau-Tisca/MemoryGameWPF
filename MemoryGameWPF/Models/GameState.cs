using System;
using System.Collections.Generic;
using MemoryGameWPF.ViewModels;

namespace MemoryGameWPF.Models
{
    public class CardState
    {
        public int CardId { get; set; }
        public string ImagePath { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
       
        public CardState() { }

        public CardState(CardViewModel vm)
        {
            CardId = vm.CardId;
            ImagePath = vm.ImagePath;
            IsFlipped = vm.IsFlipped;
            IsMatched = vm.IsMatched;
        }
    }


    public class GameState
    {
        // --- Game Configuration ---
        public string UserName { get; set; } 
        public string SelectedCategory { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        // --- Board State ---
        public List<CardState> BoardCardStates { get; set; }

        // --- Timer State ---
        public long TimeRemainingTicks { get; set; }
        public bool IsGameActive { get; set; }

        public GameState()
        {
            BoardCardStates = new List<CardState>();
        }
    }
}