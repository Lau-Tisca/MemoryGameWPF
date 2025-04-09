// Models/GameState.cs
using System;
using System.Collections.Generic;
using MemoryGameWPF.ViewModels; // Need this for CardState or CardViewModel reference

namespace MemoryGameWPF.Models
{
    // Option 1: Save necessary CardViewModel state directly
    public class CardState
    {
        public int CardId { get; set; }
        public string ImagePath { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
        // Note: No need to save the callback Action or the Command

        // Parameterless constructor for deserialization
        public CardState() { }

        // Constructor to create from CardViewModel (optional helper)
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
        public string UserName { get; set; } // To know whose save it is
        public string SelectedCategory { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        // --- Board State ---
        // Save the state of each card, maintaining order
        public List<CardState> BoardCardStates { get; set; }

        // --- Timer State ---
        // Use long Ticks for reliable TimeSpan serialization with System.Text.Json
        public long TimeRemainingTicks { get; set; }
        public bool IsGameActive { get; set; } // Was the timer running?

        // --- Optional Game Progress ---
        // public int Moves { get; set; }
        // public int Score { get; set; }

        // Parameterless constructor for deserialization
        public GameState()
        {
            BoardCardStates = new List<CardState>();
        }
    }
}