// ViewModels/CardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace MemoryGameWPF.ViewModels
{
    public class CardViewModel : INotifyPropertyChanged
    {
        private readonly Action<CardViewModel> _cardClickedCallback; // Action to notify GameViewModel

        // --- Properties ---

        // ID to identify matching cards (e.g., the unique ID of the image source)
        public int CardId { get; }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            // Typically set once during creation
            private set { _imagePath = value; OnPropertyChanged(); }
        }

        private bool _isFlipped;
        public bool IsFlipped
        {
            get => _isFlipped;
            set
            {
                if (_isFlipped != value)
                {
                    _isFlipped = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatched;
        public bool IsMatched
        {
            get => _isMatched;
            set
            {
                if (_isMatched != value)
                {
                    _isMatched = value;
                    OnPropertyChanged();
                    // When matched, re-evaluate CanExecuteFlipCard (to disable)
                    FlipCardCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Command ---
        public RelayCommand<object> FlipCardCommand { get; }


        // --- Constructor ---
        // Accepts ID, image path, and a callback to notify the GameViewModel when clicked
        public CardViewModel(int cardId, string imagePath, Action<CardViewModel> cardClickedCallback)
        {
            CardId = cardId;
            ImagePath = imagePath ?? throw new ArgumentNullException(nameof(imagePath));
            _cardClickedCallback = cardClickedCallback ?? throw new ArgumentNullException(nameof(cardClickedCallback));

            IsFlipped = false; // Start face down
            IsMatched = false; // Start unmatched

            FlipCardCommand = new RelayCommand<object>(ExecuteFlipCard, CanExecuteFlipCard);
        }

        // --- Command Methods ---
        private void ExecuteFlipCard(object parameter)
        {
            // Notify the main GameViewModel that this card was clicked
            _cardClickedCallback(this);
        }

        private bool CanExecuteFlipCard(object parameter)
        {
            // Can only flip if the card is not already face up OR already matched
            return !IsFlipped && !IsMatched;
        }


        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}