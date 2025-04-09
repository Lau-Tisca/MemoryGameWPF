using MemoryGameWPF.Models;
using MemoryGameWPF.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Reflection;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Text.Json;

namespace MemoryGameWPF.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly Random _random = new Random(); // For shuffling
        private List<CardViewModel> _currentlyFlippedCards = new List<CardViewModel>(); // Track flipped cards
        private bool _isCheckingPair = false; // Flag to prevent clicking during pair check delay
        private readonly Action<string, bool> _updateStatsAction;
        private string _currentSaveFilePath = null; // Track the path if a game was loaded/saved

        // Timer Fields
        private DispatcherTimer _gameTimer;
        private TimeSpan _timeRemaining;
        private bool _isGameActive = false; // To control timer starting/stopping

        #endregion

        #region Properties

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

        // Timer Property for UI Binding
        public TimeSpan TimeRemaining
        {
            get => _timeRemaining;
            private set // Private setter, controlled by timer logic
            {
                _timeRemaining = value;
                OnPropertyChanged();
                // Optional: Update formatted string property too
                OnPropertyChanged(nameof(TimeRemainingFormatted));
            }
        }

        // Optional Formatted Time for Cleaner UI Display
        public string TimeRemainingFormatted => $"Time: {_timeRemaining:mm\\:ss}"; // Format as MM:SS

        #endregion

        #region Commands

        public RelayCommand<object> NewGameCommand { get; }
        public RelayCommand<string> SelectCategoryCommand { get; } // Takes category name string
        public RelayCommand<object> SetStandardOptionsCommand { get; }
        public RelayCommand<object> SetCustomOptionsCommand { get; } // Placeholder
        public RelayCommand<object> ExitCommand { get; }
        public RelayCommand<object> AboutCommand { get; }
        public RelayCommand<object> ShowStatisticsCommand { get; }
        public RelayCommand<object> SaveGameCommand { get; }
        public RelayCommand<object> OpenGameCommand { get; } // Need this in File Menu


        #endregion

        #region Constructor

        public GameViewModel(User currentUser, Action<string, bool> updateStatsAction)
        {
            CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _updateStatsAction = updateStatsAction ?? throw new ArgumentNullException(nameof(updateStatsAction)); // Store the delegate

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
            SelectCategoryCommand = new RelayCommand<string>(ExecuteSelectCategory);
            SetStandardOptionsCommand = new RelayCommand<object>(ExecuteSetStandardOptions);
            SetCustomOptionsCommand = new RelayCommand<object>(ExecuteSetCustomOptions, CanExecuteSetCustomOptions); // Add CanExecute later if needed
            ExitCommand = new RelayCommand<object>(ExecuteExit);
            AboutCommand = new RelayCommand<object>(ExecuteAbout);
            SaveGameCommand = new RelayCommand<object>(ExecuteSaveGame, CanExecuteSaveGame);
            OpenGameCommand = new RelayCommand<object>(ExecuteOpenGame);
            _currentlyFlippedCards = new List<CardViewModel>(2);

            ShowStatisticsCommand = new RelayCommand<object>(ExecuteShowStatistics); // Initialize new command
            InitializeTimer(); // Make sure timer is initialized
        }
        #endregion

        #region Methods

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
                        imageNames.AddRange(new[] { "cat.jpg", "dog.jpg", "lion.jpg", "panda.jpg", "tiger.jpg", "elephant.png", "giraffe.jpg", "monkey.png", "zebra.jpg", "bear.png", "fox.jpg", "hippo.png", "kangaroo.jpg", "koala.png", "owl.jpg", "penguin.png", "rabbit.jpg", "wolf.jpg" }); // Need at least 18 for 6x6
                        break;
                    case "Nature":
                        imageNames.AddRange(new[] { "beach.jpg", "desert.png", "forest.jpg", "island.png", "lake.jpg", "mountain.png", "ocean.jpg", "river.png", "sunrise.jpg", "sunset.png", "tree.jpg", "valley.jpg", "volcano.jpg", "waterfall.png", "aurora.jpg", "canyon.jpg", "glacier.jpeg", "meadow.png" }); // Need 18
                        break;
                    case "Objects":
                        imageNames.AddRange(new[] { "book.png", "car.jpg", "chair.jpg", "clock.jpg", "computer.png", "cup.jpg", "guitar.png", "house.jpg", "key.png", "lamp.jpg", "phone.png", "plane.jpg", "ship.png", "table.jpg", "train.jpg", "umbrella.jpg", "vase.jpg", "watch.jpg" }); // Need 18
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

        // Method to populate ViewModel from a loaded GameState
        private void LoadState(GameState state)
        {
            if (state == null) return; // Or throw?

            // Stop current game processes
            StopTimer();
            _currentlyFlippedCards.Clear();
            _isCheckingPair = false;

            // Restore configuration (Username should match CurrentUser ideally)
            if (CurrentUser.UserName != state.UserName)
            {
                MessageBox.Show($"Warning: Loading game saved by '{state.UserName}' as user '{CurrentUser.UserName}'.", "User Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Potentially prevent loading or adjust CurrentUser? For now, just warn.
            }
            SelectedCategory = state.SelectedCategory; // Update property (will trigger path loading)
            // Assuming Rows/Columns are now settable properties:
            // Rows = state.Rows;
            // Columns = state.Columns;
            // If Rows/Columns are still fixed to 4x4, you might only allow loading 4x4 saves
            if (state.Rows != this.Rows || state.Columns != this.Columns)
            {
                MessageBox.Show($"Cannot load game: Save file has dimensions {state.Rows}x{state.Columns}, but current options are {this.Rows}x{this.Columns}.", "Dimension Mismatch", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeNewGame(); // Start a default new game instead?
                return;
            }


            // Restore Board
            GameBoardCards.Clear();
            if (state.BoardCardStates != null)
            {
                foreach (var cardState in state.BoardCardStates)
                {
                    // Create CardViewModel from saved state, passing the click handler
                    var cardVM = new CardViewModel(cardState.CardId, cardState.ImagePath, HandleCardClicked)
                    {
                        IsFlipped = cardState.IsFlipped,
                        IsMatched = cardState.IsMatched
                    };
                    GameBoardCards.Add(cardVM);
                }
            }

            // Restore Timer
            TimeRemaining = TimeSpan.FromTicks(state.TimeRemainingTicks);
            _isGameActive = state.IsGameActive; // Store if timer should be running
            if (_isGameActive)
            {
                StartTimer(); // Start timer only if it was active in saved state
            }
            else
            {
                // Ensure timer UI updates even if not started (e.g., game already won/lost)
                OnPropertyChanged(nameof(TimeRemaining));
                OnPropertyChanged(nameof(TimeRemainingFormatted));
            }

            // TODO: Restore other state (Moves, Score, _currentlyFlippedCards if saving mid-turn)

            System.Diagnostics.Debug.WriteLine($"Game state loaded. Timer Active: {_isGameActive}, Time Left: {TimeRemaining}");
        }

        // --- Command Execute Methods ---

        private void ExecuteSaveGame(object parameter)
        {
            if (!_isGameActive && !GameBoardCards.Any(c => c.IsFlipped && !c.IsMatched))
            {
                // Optional: Prevent saving if game isn't started, or is already won/lost/timed out
                // Or allow saving anytime the board has cards? Your choice.
                // MessageBox.Show("No active game to save.", "Save Game", MessageBoxButton.OK, MessageBoxImage.Information);
                // return;
            }
            // Stop timer temporarily while saving? Usually not necessary for this data.
            // StopTimer();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Memory Game Save (*.mgs)|*.mgs|All Files (*.*)|*.*";
            // Default filename based on user
            saveFileDialog.FileName = $"{CurrentUser.UserName}_MemoryGame_{DateTime.Now:yyyyMMddHHmm}.mgs";
            // Suggest initial directory (e.g., Documents or BaseDirectory)
            // saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;


            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    // 1. Create GameState object from current ViewModel state
                    GameState currentState = new GameState
                    {
                        UserName = CurrentUser.UserName,
                        SelectedCategory = this.SelectedCategory,
                        Rows = this.Rows,
                        Columns = this.Columns,
                        BoardCardStates = this.GameBoardCards.Select(vm => new CardState(vm)).ToList(), // Convert VMs to States
                        TimeRemainingTicks = this.TimeRemaining.Ticks,
                        IsGameActive = this._isGameActive // Save if timer was running
                        // TODO: Add Moves, Score etc. if implemented
                    };

                    // 2. Serialize GameState to JSON
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(currentState, options);

                    // 3. Write JSON to selected file
                    File.WriteAllText(filePath, json);

                    _currentSaveFilePath = filePath; // Remember where we saved last
                    MessageBox.Show($"Game saved successfully to:\n{filePath}", "Game Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving game to {filePath}:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Restart timer if it was stopped only for saving
                    // if (_isGameActive) StartTimer();
                }
            }
        }

        private bool CanExecuteSaveGame(object parameter)
        {
            // Allow saving only if there are cards on the board
            // You could add more conditions (e.g., game not already won/lost)
            bool boardHasCards = GameBoardCards != null && GameBoardCards.Count > 0;
            // Optional: Add check for game not finished
            // bool gameInProgress = _isGameActive || (_currentlyFlippedCards.Count > 0) || !GameBoardCards.All(c => c.IsMatched);
            // return boardHasCards && gameInProgress;

            return boardHasCards; // Current simple logic
        }

        private void ExecuteOpenGame(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Memory Game Save (*.mgs)|*.mgs|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;


            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    // 1. Read JSON from file
                    string json = File.ReadAllText(filePath);

                    // 2. Deserialize JSON to GameState object
                    GameState loadedState = JsonSerializer.Deserialize<GameState>(json);

                    if (loadedState != null)
                    {
                        // 3. Load the state into the ViewModel
                        LoadState(loadedState);
                        _currentSaveFilePath = filePath; // Remember path of loaded game
                        MessageBox.Show($"Game loaded successfully from:\n{filePath}", "Game Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to load game data from file:\n{filePath}\nFile might be empty or corrupted.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading game from {filePath}:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeTimer()
        {
            if (_gameTimer == null)
            {
                _gameTimer = new DispatcherTimer();
                _gameTimer.Interval = TimeSpan.FromSeconds(1); // Tick every second
                _gameTimer.Tick += GameTimer_Tick;
            }
            // Reset state even if timer existed
            _gameTimer.Stop();
            TimeRemaining = GetDefaultGameTime(); // Set initial time
            _isGameActive = false;
        }

        private TimeSpan GetDefaultGameTime()
        {
            // TODO: Allow this to be configurable via Options later
            // For now, fixed time based on board size (e.g., more time for bigger boards)
            int totalSeconds = 30 + (Rows * Columns * 2); // Example calculation
            return TimeSpan.FromSeconds(totalSeconds);
        }

        private void StartTimer()
        {
            if (_gameTimer != null && !_gameTimer.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"Starting timer with {TimeRemaining}");
                _isGameActive = true;
                _gameTimer.Start();
            }
        }

        private void StopTimer()
        {
            if (_gameTimer != null && _gameTimer.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Stopping timer.");
                _gameTimer.Stop();
                _isGameActive = false;
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!_isGameActive) return; // Should not happen if timer is stopped, but safety check

            TimeRemaining = TimeRemaining.Subtract(TimeSpan.FromSeconds(1));

            if (TimeRemaining <= TimeSpan.Zero)
            {
                // --- Time Ran Out ---
                StopTimer();
                TimeRemaining = TimeSpan.Zero; // Ensure it shows 00:00
                System.Diagnostics.Debug.WriteLine("Game Over - Time Ran Out!");

                // Disable remaining cards visually (optional) - could iterate GameBoardCards
                // foreach(var card in GameBoardCards.Where(c => !c.IsMatched)) { card.IsEnabled = false; } // Requires IsEnabled property on CardViewModel OR handle in Style

                MessageBox.Show($"Sorry {CurrentUser.UserName}, you ran out of time!", "Game Over", MessageBoxButton.OK, MessageBoxImage.Stop);

                _updateStatsAction(CurrentUser.UserName, false);
                DeleteCurrentSaveFile();
            }
        }

        private void ExecuteShowStatistics(object parameter)
        {
            // TODO: Implement showing the statistics window
            // 1. Need to get the latest stats data (maybe load it again, or pass it somehow?)
            //    For simplicity now, we might just load it again inside StatisticsViewModel
            // 2. Create StatisticsViewModel
            // 3. Create StatisticsWindow
            // 4. Set DataContext
            // 5. ShowDialog

            StatisticsViewModel statsViewModel = new StatisticsViewModel(); // It will load data itself
            StatisticsWindow statsWindow = new StatisticsWindow();
            statsWindow.DataContext = statsViewModel;

            // Set owner
            Window owner = Application.Current.Windows.OfType<GameWindow>().FirstOrDefault(w => w.DataContext == this);
            if (owner != null) statsWindow.Owner = owner;

            statsWindow.ShowDialog();
        }

        private void ExecuteSelectCategory(string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName) && AvailableCategories.Contains(categoryName))
            {
                SelectedCategory = categoryName; // This setter already calls LoadImagePathsForCategory
                System.Diagnostics.Debug.WriteLine($"Category selected: {SelectedCategory}");
                // Optional: Automatically start a new game when category changes?
                // InitializeNewGame();
                MessageBox.Show($"Category set to '{SelectedCategory}'. Start a New Game to apply.", "Category Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteSetStandardOptions(object parameter)
        {
            // Since Rows/Columns are currently read-only returning 4, this doesn't
            // functionally change anything *yet*. But if we re-introduce settable
            // Rows/Columns later, this command would set them back to 4x4.
            // For now, it can just confirm the setting.
            // Rows = 4; // Would need settable properties
            // Columns = 4; // Would need settable properties
            System.Diagnostics.Debug.WriteLine("Options set to Standard (4x4).");
            MessageBox.Show("Options set to Standard (4x4). Start a New Game to apply.", "Options Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            // Optional: Automatically start a new game?
            // InitializeNewGame();
        }

        private void ExecuteSetCustomOptions(object parameter)
        {
            // TODO: Implement Custom Options Dialog
            // 1. Create a new Window (e.g., CustomOptionsWindow.xaml) and ViewModel (CustomOptionsViewModel.cs)
            // 2. CustomOptionsViewModel should have properties for Rows & Columns (e.g., with validation 2-6, even total)
            // 3. Open the CustomOptionsWindow as a dialog.
            // 4. If the dialog returns OK (user clicked Save/OK):
            //    - Get the selected Rows/Columns from CustomOptionsViewModel.
            //    - Update the Rows/Columns properties in *this* GameViewModel.
            //    - (Requires making Rows/Columns properties settable again)
            // 5. Optional: Automatically start a new game with custom dimensions?

            MessageBox.Show("Custom Options dialog not yet implemented.", "TODO", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanExecuteSetCustomOptions(object parameter)
        {
            // TODO: Return false if custom options feature is disabled
            return true; // Enable for now, even if not implemented
        }

        private void ExecuteExit(object parameter)
        {
            // Goal: Close the GameWindow and show the SignInView/MainWindow again.

            // 1. Stop any ongoing game processes (like the timer)
            StopTimer();
            _isGameActive = false; // Ensure game state is inactive

            // 2. Find the GameWindow associated with this ViewModel instance
            Window currentGameWindow = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this && window is GameWindow)
                {
                    currentGameWindow = window;
                    break;
                }
            }

            if (currentGameWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Could not find GameWindow instance to close in ExecuteExit.");
                // Might need a more robust way to get the window reference if this fails
                return;
            }

            try
            {
                // 3. Create and show the SignIn view (MainWindow)
                //    We assume MainWindow hosts SignInView initially.
                MainWindow signInWindow = new MainWindow(); // Creates the window hosting SignInView
                Application.Current.MainWindow = signInWindow; // Set it as the main window *before* showing
                signInWindow.Show();

                // 4. Close the current GameWindow
                currentGameWindow.Close();
                System.Diagnostics.Debug.WriteLine("Exited game, returned to Sign In.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error trying to return to Sign In screen: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteAbout(object parameter)
        {
            AboutWindow aboutWindow = new AboutWindow();

            // Try to set the owner window for better modality behavior
            Window owner = Application.Current.Windows.OfType<GameWindow>().FirstOrDefault(w => w.DataContext == this);
            if (owner != null)
            {
                aboutWindow.Owner = owner;
            }
            else // Fallback if GameWindow not found easily (might happen during quick transitions)
            {
                aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            aboutWindow.ShowDialog(); // Show as a modal dialog
        }

        // Core logic to set up a new game board
        private void InitializeNewGame()
        {
            System.Diagnostics.Debug.WriteLine($"Starting new game: {Rows}x{Columns}, Category: {SelectedCategory}");

            GameBoardCards.Clear(); // Clear previous cards

            _currentSaveFilePath = null;

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

            // 6. Reset game state & Timer
            _currentlyFlippedCards.Clear();
            _isCheckingPair = false;
            _isGameActive = false; // Ensure game isn't active before timer reset
            // TODO: Reset moves/score counters

            // Reset timer to default duration and start it
            InitializeTimer(); // Resets TimeRemaining and stops timer if running
            StartTimer(); // Start the countdown

            // Ensure cards are playable (needed if previous game ended by time out and disabled cards)
            foreach (var card in GameBoardCards)
            {
                card.IsFlipped = false;
                card.IsMatched = false;
                // Make sure FlipCardCommand CanExecute is re-evaluated if it depends on IsEnabled property
                card.FlipCardCommand.RaiseCanExecuteChanged();
            }

            SaveGameCommand.RaiseCanExecuteChanged();

            System.Diagnostics.Debug.WriteLine($"Game board initialized with {GameBoardCards.Count} cards. Timer started.");
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

        // Callback method for when a card is clicked
        private async void HandleCardClicked(CardViewModel clickedCard) // Make async for Task.Delay
        {
            // Ignore clicks if already checking a pair or card is already flipped/matched or timer is not active
            if (!_isGameActive || _isCheckingPair || clickedCard.IsFlipped || clickedCard.IsMatched)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Handling Click: ID={clickedCard.CardId}");

            // Flip the clicked card
            clickedCard.IsFlipped = true;

            // Add to tracked flipped cards
            _currentlyFlippedCards.Add(clickedCard);

            // Check if two cards are now flipped
            if (_currentlyFlippedCards.Count == 2)
            {
                _isCheckingPair = true; // Prevent further clicks during check
                CardViewModel card1 = _currentlyFlippedCards[0];
                CardViewModel card2 = _currentlyFlippedCards[1];

                // TODO: Increment move counter here

                // Compare CardId for a match
                if (card1.CardId == card2.CardId)
                {
                    // --- Match Found ---
                    System.Diagnostics.Debug.WriteLine($"Match Found: ID={card1.CardId}");
                    // Mark both as matched
                    card1.IsMatched = true;
                    card2.IsMatched = true;

                    // Clear the flipped cards tracker
                    _currentlyFlippedCards.Clear();
                    _isCheckingPair = false; // Allow clicks again

                    CheckIfGameWon();
                }
                else
                {
                    // --- No Match ---
                    System.Diagnostics.Debug.WriteLine($"No Match: ID={card1.CardId} vs ID={card2.CardId}");
                    // Wait for a short period so the user can see the second card
                    await Task.Delay(1000); // Wait for 1 second 

                    // Flip both cards back down (only if they haven't been matched by some other rapid interaction - unlikely)
                    if (!card1.IsMatched) card1.IsFlipped = false;
                    if (!card2.IsMatched) card2.IsFlipped = false;

                    // Clear the flipped cards tracker
                    _currentlyFlippedCards.Clear();
                    _isCheckingPair = false; // Allow clicks again
                }
            }
            else if (_currentlyFlippedCards.Count > 2)
            {
                // This case shouldn't ideally happen if _isCheckingPair works,
                // but as a fallback, clear the list and flip back extras if needed.
                System.Diagnostics.Debug.WriteLine("Warning: More than 2 cards flipped unexpectedly.");
                // Consider flipping back all in _currentlyFlippedCards except perhaps the last one?
                _currentlyFlippedCards.Clear(); // Reset state
                _isCheckingPair = false;
            }
        }

        // Placeholder for checking win condition
        private void CheckIfGameWon()
        {
            bool allMatched = GameBoardCards.All(card => card.IsMatched);
            if (allMatched && GameBoardCards.Any() && _isGameActive) // Only count win if game was active
            {
                System.Diagnostics.Debug.WriteLine("Game Won!");
                StopTimer(); 
                MessageBox.Show($"Congratulations {CurrentUser.UserName}, you won!\nTime remaining: {TimeRemaining:mm\\:ss}", "You Won!", MessageBoxButton.OK, MessageBoxImage.Information);
                _updateStatsAction(CurrentUser.UserName, true);
                DeleteCurrentSaveFile();
            }
        }

        private void DeleteCurrentSaveFile()
        {
            // Check if we actually loaded from a specific file path
            if (!string.IsNullOrEmpty(_currentSaveFilePath))
            {
                string pathToDelete = _currentSaveFilePath; // Copy to local variable
                _currentSaveFilePath = null; // Clear the tracked path immediately

                System.Diagnostics.Debug.WriteLine($"Attempting to delete completed save file: {pathToDelete}");
                try
                {
                    if (File.Exists(pathToDelete))
                    {
                        File.Delete(pathToDelete);
                        MessageBox.Show("The loaded save file for the completed game has been deleted.", "Save File Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                        System.Diagnostics.Debug.WriteLine($"Successfully deleted save file: {pathToDelete}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Save file not found for deletion (already deleted?): {pathToDelete}");
                    }
                }
                catch (IOException ioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"IO Error deleting save file {pathToDelete}: {ioEx.Message}");
                    MessageBox.Show($"Could not delete the save file:\n{pathToDelete}\n\nReason: {ioEx.Message}\n\nYou may need to delete it manually.", "Save Deletion Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (UnauthorizedAccessException authEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Permission Error deleting save file {pathToDelete}: {authEx.Message}");
                    MessageBox.Show($"Permission denied when trying to delete the save file:\n{pathToDelete}\n\nYou may need to delete it manually.", "Save Deletion Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected Error deleting save file {pathToDelete}: {ex.Message}");
                    MessageBox.Show($"An unexpected error occurred while deleting the save file:\n{pathToDelete}\n\nYou may need to delete it manually.", "Save Deletion Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No loaded save file path to delete.");
            }
        }

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