using MemoryGameWPF.Models;
using System;
using System.Collections.Generic; // For List
using System.Collections.ObjectModel; // For ObservableCollection
using System.ComponentModel;
using System.IO; // For Path
using System.Linq; // For LINQ methods like Select, Take, etc.
using System.Runtime.CompilerServices;
using System.Windows.Input; // For ICommand (later)
using System.Reflection; // For Assembly.GetExecutingAssembly (optional, for pathing)
using GalaSoft.MvvmLight.Command;
using System.Windows; // For RelayCommand (optional, for command binding)

namespace MemoryGameWPF.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        #region Fields & Properties

        private readonly Random _random = new Random(); // For shuffling

        private ObservableCollection<CardViewModel> _gameBoardCards;
        public ObservableCollection<CardViewModel> GameBoardCards
        {
            get => _gameBoardCards;
            set { _gameBoardCards = value; OnPropertyChanged(); }
        }

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            // Typically CurrentUser doesn't change during a game session
            private set { _currentUser = value; OnPropertyChanged(); }
        }

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        // --- Category Management ---
        public List<string> AvailableCategories { get; } // List of category names
        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    // Optionally, trigger a new game or reset when category changes?
                    // Or just use this value when "New Game" is clicked.
                    LoadImagePathsForCategory(); // Load paths for the selected category
                }
            }
        }
        private List<string> _availableImagePaths = new List<string>(); // Holds paths for the currently selected category

        // --- Game Options ---
        private int _rows = 4; // Default to Standard 4x4
        public int Rows
        {
            get => _rows;
            set { if (_rows != value) { _rows = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoardSizeDescription)); } }
        }

        private int _columns = 4; // Default to Standard 4x4
        public int Columns
        {
            get => _columns;
            set { if (_columns != value) { _columns = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoardSizeDescription)); } }
        }

        // Helper property for UI display (optional)
        public string BoardSizeDescription => $"{Rows} x {Columns}";

        // --- Board Representation (Placeholder for now) ---
        // We will add ObservableCollection<CardViewModel> here later

        #endregion

        #region Commands
        // We will add commands for menu items later (New Game, Options, etc.)
        // public ICommand NewGameCommand { get; }
        // public ICommand SetStandardOptionsCommand { get; }
        // public ICommand SetCustomOptionsCommand { get; }

        public RelayCommand<object> NewGameCommand { get; }
        #endregion

        // --- Constructor ---
        public GameViewModel(User currentUser)
        {
            CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            WelcomeMessage = $"Welcome, {CurrentUser.UserName}! Select options and start a new game!";

            // Initialize Categories
            AvailableCategories = new List<string> { "Animals", "Nature", "Objects" }; // Match your folder names
            SelectedCategory = AvailableCategories.FirstOrDefault(); // Select the first category by default

            // Load initial image paths
            _availableImagePaths = new List<string>();
            LoadImagePathsForCategory();

            // Inside GameViewModel constructor
            GameBoardCards = new ObservableCollection<CardViewModel>();
            // Initialize Commands
            NewGameCommand = new RelayCommand<object>(ExecuteNewGame);
            // TODO: Initialize other commands (Options, Save, Load etc.)
            // Example (implement methods later):
            // SetStandardOptionsCommand = new RelayCommand<object>(ExecuteSetStandardOptions);
        }

        #region Methods

        // Add to Methods region in GameViewModel.cs
        private void HandleCardClicked(CardViewModel clickedCard)
        {
            System.Diagnostics.Debug.WriteLine($"Card Clicked: ID={clickedCard.CardId}, Path={clickedCard.ImagePath}");
            // TODO: Implement game logic here (flipping, matching)
        }

        private void LoadImagePathsForCategory()
        {
            _availableImagePaths.Clear();
            if (string.IsNullOrEmpty(SelectedCategory)) return;

            try
            {
                // --- Using Resource Paths ---
                // This assumes images are embedded as Resources.
                // Format: /AssemblyName;component/FolderPath/ImageName.ext
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                string resourcePathBase = $"/{assemblyName};component/GameImages/{SelectedCategory}/";

                // Note: Getting resource names isn't trivial directly like file system.
                // A common approach is to *know* the filenames or use a naming convention.
                // For simplicity here, let's assume we know some filenames.
                // In a real app, you might list them in a config file or use reflection
                // on the resources if needed (more complex).

                // Example: Manually list known image names for simplicity
                // IMPORTANT: Replace these with your ACTUAL filenames!
                List<string> imageNames = new List<string>();
                switch (SelectedCategory)
                {
                    case "Animals":
                        imageNames.AddRange(new[] { "cat.png", "dog.jpg", "lion.jpg", "panda.png", "tiger.jpg", "elephant.png", "giraffe.jpg", "monkey.png", "zebra.jpg", "bear.png", "fox.jpg", "hippo.png", "kangaroo.jpg", "koala.png", "owl.jpg", "penguin.png", "rabbit.jpg", "wolf.png" }); // Need at least 18 for 6x6
                        break;
                    case "Nature":
                        imageNames.AddRange(new[] { "beach.jpg", "desert.png", "forest.jpg", "island.png", "lake.jpg", "mountain.png", "ocean.jpg", "river.png", "sunrise.jpg", "sunset.png", "tree.jpg", "valley.png", "volcano.jpg", "waterfall.png", "aurora.jpg", "canyon.png", "glacier.jpg", "meadow.png" }); // Need 18
                        break;
                    case "Objects":
                        imageNames.AddRange(new[] { "book.png", "car.jpg", "chair.png", "clock.jpg", "computer.png", "cup.jpg", "guitar.png", "house.jpg", "key.png", "lamp.jpg", "phone.png", "plane.jpg", "ship.png", "table.jpg", "train.png", "umbrella.jpg", "vase.png", "watch.jpg" }); // Need 18
                        break;
                }

                foreach (var imageName in imageNames)
                {
                    _availableImagePaths.Add(resourcePathBase + imageName);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_availableImagePaths.Count} images for category '{SelectedCategory}'");
                if (_availableImagePaths.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No images found or listed for category '{SelectedCategory}'. Check paths and filenames.");
                    // Consider showing a message to the user?
                }

                // --- Alternative: Using File System Paths (If Build Action is Content/Copy) ---
                // string categoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameImages", SelectedCategory);
                // if (Directory.Exists(categoryPath))
                // {
                //     _availableImagePaths = Directory.GetFiles(categoryPath, "*.*")
                //                                     .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".gif"))
                //                                     .ToList();
                // } else {
                //      System.Diagnostics.Debug.WriteLine($"Warning: Directory not found {categoryPath}");
                // }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image paths for category '{SelectedCategory}': {ex.Message}");
                // Handle error appropriately (e.g., show message to user)
            }
        }

        private void ExecuteNewGame(object parameter)
        {
            InitializeNewGame();
        }

        // Core logic to set up a new game board
        private void InitializeNewGame()
        {
            System.Diagnostics.Debug.WriteLine($"Starting new game: {Rows}x{Columns}, Category: {SelectedCategory}");

            GameBoardCards.Clear(); // Clear previous cards

            int totalCards = Rows * Columns;
            if (totalCards % 2 != 0)
            {
                // This shouldn't happen if options are validated, but good safety check
                MessageBox.Show("Board size must have an even number of cards.", "Invalid Size", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int numberOfPairs = totalCards / 2;

            // 1. Ensure enough images are available for the selected category
            if (_availableImagePaths == null || _availableImagePaths.Count < numberOfPairs)
            {
                MessageBox.Show($"Not enough unique images found in category '{SelectedCategory}' for a {Rows}x{Columns} board (Need {numberOfPairs} pairs, found {_availableImagePaths?.Count ?? 0}).\nPlease select another category or add more images.",
                                "Insufficient Images", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Select unique image paths for the pairs needed
            //    Shuffle available paths and take the required number
            List<string> selectedImagePathsForGame = _availableImagePaths
                                                        .OrderBy(x => _random.Next()) // Shuffle
                                                        .Take(numberOfPairs)          // Take needed pairs
                                                        .ToList();

            // 3. Create CardViewModel pairs
            List<CardViewModel> cards = new List<CardViewModel>();
            int cardIdCounter = 0; // Simple ID for matching pairs
            foreach (string imagePath in selectedImagePathsForGame)
            {
                // Create two cards for each image, using the same CardId
                var card1 = new CardViewModel(cardIdCounter, imagePath, HandleCardClicked);
                var card2 = new CardViewModel(cardIdCounter, imagePath, HandleCardClicked);
                cards.Add(card1);
                cards.Add(card2);
                cardIdCounter++;
            }

            // 4. Shuffle the final list of cards
            Shuffle(cards);

            // 5. Add shuffled cards to the ObservableCollection bound to the UI
            foreach (var card in cards)
            {
                GameBoardCards.Add(card);
            }

            // 6. TODO: Reset game state (timer, moves counter, flipped cards tracker etc.)
            //    (We'll add fields/properties for these later)
            //    ResetFlippedCards();
            //    ResetTimer();
            //    StartTimer(); // If implementing timer

            System.Diagnostics.Debug.WriteLine($"Game board initialized with {GameBoardCards.Count} cards.");
        }

        // Helper method to shuffle a list (Fisher-Yates algorithm)
        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // --- TODO: Implement Command Execute Methods ---
        // private void ExecuteSetStandardOptions(object param) { Rows = 4; Columns = 4; }
        // etc.

        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}