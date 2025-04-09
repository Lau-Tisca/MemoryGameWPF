using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO; // For file operations
using System.Linq; // For LINQ queries like FirstOrDefault
using System.Runtime.CompilerServices;
using System.Text.Json; // For JSON serialization
using System.Windows;
using System.Windows.Input;
using MemoryGameWPF.Models;
using MemoryGameWPF.Views;

namespace MemoryGameWPF.ViewModels
{
    public class SignInViewModel : INotifyPropertyChanged
    {
        private const string UserDataFileName = "users.json";
        private const string StatsDataFileName = "game_stats.json";
        private readonly string _userDataFilePath;
        private readonly string _statsDataFilePath;

        private ObservableCollection<User> _users;
        private Dictionary<string, UserStats> _allUserStats;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        private User _selectedUser;
        public User SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                    DeleteUserCommand.RaiseCanExecuteChanged();
                    PlayCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand<object> NewUserCommand { get; }
        public RelayCommand<object> DeleteUserCommand { get; }
        public RelayCommand<object> PlayCommand { get; }
        public RelayCommand<object> CancelCommand { get; }

        public SignInViewModel()
        {
            // Determine the path for the user data file
            _userDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UserDataFileName);
            _statsDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatsDataFileName);
            // Alternative: Use ApplicationData folder for better practice
            // string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // string appFolder = Path.Combine(appDataPath, "YourMemoryGame"); // Create a folder for your app
            // Directory.CreateDirectory(appFolder); // Ensure folder exists
            // _userDataFilePath = Path.Combine(appFolder, UserDataFileName);

            LoadUsers(); // Load users when ViewModel is created
            LoadStatistics();

            NewUserCommand = new RelayCommand<object>(ExecuteNewUser);
            DeleteUserCommand = new RelayCommand<object>(ExecuteDeleteUser, CanExecuteDeleteUser);
            PlayCommand = new RelayCommand<object>(ExecutePlay, CanExecutePlay);
            CancelCommand = new RelayCommand<object>(ExecuteCancel);
        }

        // --- Statistics Load/Save ---
        private void LoadStatistics()
        {
            if (!File.Exists(_statsDataFilePath))
            {
                _allUserStats = new Dictionary<string, UserStats>(); // Create empty dictionary if file doesn't exist
                return;
            }

            try
            {
                string json = File.ReadAllText(_statsDataFilePath);
                _allUserStats = JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json);
                if (_allUserStats == null) // Handle potential null from deserialization or empty file
                {
                    _allUserStats = new Dictionary<string, UserStats>();
                    System.Diagnostics.Debug.WriteLine("Warning: Statistics file was empty or invalid. Initialized new statistics.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading game statistics from {_statsDataFilePath}:\n{ex.Message}", "Statistics Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _allUserStats = new Dictionary<string, UserStats>(); // Start with empty stats on error
            }
        }

        private bool SaveStatistics()
        {
            if (_allUserStats == null) // Safety check
            {
                System.Diagnostics.Debug.WriteLine("Error: Attempted to save null statistics.");
                return false;
            }
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_allUserStats, options);
                File.WriteAllText(_statsDataFilePath, json);
                System.Diagnostics.Debug.WriteLine("Game statistics saved.");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game statistics to {_statsDataFilePath}:\n{ex.Message}", "Statistics Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // --- Method to Update Stats (will be passed as delegate) ---
        private void UpdateUserStats(string username, bool wonGame)
        {
            if (string.IsNullOrEmpty(username) || _allUserStats == null) return;

            // Find or create stats entry for the user
            if (!_allUserStats.TryGetValue(username, out UserStats stats))
            {
                stats = new UserStats();
                _allUserStats[username] = stats;
            }

            // Update stats
            stats.GamesPlayed++;
            if (wonGame)
            {
                stats.GamesWon++;
            }

            System.Diagnostics.Debug.WriteLine($"Stats Updated for {username}: Played={stats.GamesPlayed}, Won={stats.GamesWon}");

            // Save updated stats immediately
            SaveStatistics();
        }

        private void LoadUsers()
        {
            if (!File.Exists(_userDataFilePath))
            {
                Users = new ObservableCollection<User>(); // Create empty list if file doesn't exist
                return;
            }

            try
            {
                string json = File.ReadAllText(_userDataFilePath);
                var loadedUsers = JsonSerializer.Deserialize<ObservableCollection<User>>(json);
                Users = loadedUsers ?? new ObservableCollection<User>(); // Handle potential null from deserialization
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user data from {_userDataFilePath}:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Users = new ObservableCollection<User>(); // Start with empty list on error
            }
        }

        private bool SaveUsers()
        {
            try
            {
                // Configure JsonSerializer options for readability (optional)
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Users, options);
                File.WriteAllText(_userDataFilePath, json);
                return true; // Indicate success
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user data to {_userDataFilePath}:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false; // Indicate failure
            }
        }

        private void ExecuteNewUser(object parameter)
        {
            NewUserViewModel newUserViewModel = new NewUserViewModel(Users, SaveUsers); // Pass Users collection and Save method
            NewUserView newUserView = new NewUserView();
            newUserView.DataContext = newUserViewModel;

            // Set the owner window for better dialog behavior
            if (Application.Current.MainWindow != null && Application.Current.MainWindow is Window ownerWindow)
            {
                newUserView.Owner = ownerWindow;
            }

            newUserView.ShowDialog();
            // No need to explicitly save here, NewUserViewModel's Save command will call SaveUsers
        }

        private void ExecuteDeleteUser(object parameter)
        {
            // 1. Safety Check & Confirmation
            if (SelectedUser == null)
            {
                MessageBox.Show("Please select a user to delete.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete user '{SelectedUser.UserName}'?\nThis will permanently remove the user profile, associated image (if possible), saved games, and statistics.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return; // User cancelled
            }

            if (result == MessageBoxResult.Yes)
            {
                User userToDelete = SelectedUser;
                string userNameToDelete = userToDelete.UserName;
                string imagePathToDelete = userToDelete.ImagePath;

                try
                {
                    Users.Remove(userToDelete);
                    SelectedUser = null;

                    if (!SaveUsers()) // Save user list first
                    {
                        MessageBox.Show($"CRITICAL ERROR: Failed to update user data file after removing '{userNameToDelete}'. Deletion incomplete.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Users.Add(userToDelete); // Revert UI
                        return;
                    }

                    try // Wrap core deletion logic in a try block
                    {
                        // 3. Remove from ObservableCollection (Updates UI immediately)
                        Users.Remove(userToDelete);
                        SelectedUser = null; // Clear selection after removing from list

                        // 4. Save Updated User List to File
                        if (!SaveUsers()) // Attempt to save the modified user list
                        {
                            // Save failed - critical error. Revert UI change and inform user.
                            MessageBox.Show($"CRITICAL ERROR: Failed to update user data file after removing '{userNameToDelete}'.\nUser deletion incomplete. Please check file permissions or disk space.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            // Re-add user to collection for UI consistency, as the file wasn't updated
                            Users.Add(userToDelete); // Consider inserting at original position if order matters
                                                     // Do not proceed with deleting associated files if the main save failed.
                            return;
                        }

                        // --- Associated File Cleanup (Attempt best-effort deletion) ---

                        // 5. Delete Associated Image File (Optional but recommended)
                        if (!string.IsNullOrEmpty(imagePathToDelete) && File.Exists(imagePathToDelete))
                        {
                            try
                            {
                                File.Delete(imagePathToDelete);
                                System.Diagnostics.Debug.WriteLine($"Deleted image file: {imagePathToDelete}");
                            }
                            catch (IOException ioEx) // Catch specific IO errors
                            {
                                // Log or show a non-critical warning. The user profile is gone, image deletion is secondary.
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not delete image file {imagePathToDelete}. Reason: {ioEx.Message}");
                                // Optionally inform the user with a less alarming message:
                                // MessageBox.Show($"Could not delete the associated image file:\n{imagePathToDelete}\n\nYou may need to delete it manually.", "Image Deletion Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            catch (UnauthorizedAccessException authEx) // Catch permission errors
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Permission denied deleting image file {imagePathToDelete}. Reason: {authEx.Message}");
                            }
                            catch (Exception ex) // Catch unexpected errors
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error deleting image file {imagePathToDelete}. Reason: {ex.Message}");
                            }
                        }

                        // 6. Delete Saved Game Data
                        // Define the pattern and location for saved games
                        string saveGamePattern = $"{userNameToDelete}_*.sav"; // Example pattern - ADJUST AS NEEDED
                        string saveGameDirectory = AppDomain.CurrentDomain.BaseDirectory; // Or specific saves folder

                        try
                        {
                            string[] saveFiles = Directory.GetFiles(saveGameDirectory, saveGamePattern);
                            if (saveFiles.Length > 0)
                            {
                                foreach (string file in saveFiles)
                                {
                                    try
                                    {
                                        File.Delete(file);
                                        System.Diagnostics.Debug.WriteLine($"Deleted save file: {file}");
                                    }
                                    catch (Exception ex) // Catch errors deleting individual save files
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Warning: Could not delete save file {file}. Reason: {ex.Message}");
                                        // Continue trying to delete other save files
                                    }
                                }
                                MessageBox.Show($"Associated save files for '{userNameToDelete}' deleted.", "Cleanup Info", MessageBoxButton.OK, MessageBoxImage.Information); // Optional confirmation
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No save files found matching pattern '{saveGamePattern}' for user '{userNameToDelete}'.");
                            }
                        }
                        catch (Exception ex) // Catch errors searching for save files (e.g., directory access issues)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not search for/delete save files for {userNameToDelete}. Reason: {ex.Message}");
                        }


                        // 7. Delete Statistics Data (using loaded _allUserStats)
                        if (_allUserStats != null && _allUserStats.Remove(userNameToDelete))
                        {
                            System.Diagnostics.Debug.WriteLine($"Removed statistics for user '{userNameToDelete}' from memory.");
                            if (!SaveStatistics()) // Save updated stats
                            {
                                MessageBox.Show($"Warning: Could not save statistics file after removing stats for '{userNameToDelete}'. Stats might reappear on next load.", "Statistics Save Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                MessageBox.Show($"Statistics for '{userNameToDelete}' deleted.", "Cleanup Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Statistics for user '{userNameToDelete}' not found. Skipping stats deletion.");
                        }
                    }
                    catch (Exception ex) // Catch errors reading/writing/processing stats file
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not process/delete statistics for {userNameToDelete}. Reason: {ex.Message}");
                        // MessageBox.Show($"Could not update statistics file:\n{ex.Message}", "Statistics Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }


                        // 8. Final Confirmation (Optional)
                        MessageBox.Show($"User '{userNameToDelete}' and associated data deleted successfully.", "Deletion Complete", MessageBoxButton.OK, MessageBoxImage.Information);


                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred during the deletion process for '{userNameToDelete}':\n{ex.Message}", "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool CanExecuteDeleteUser(object parameter)
        {
            return SelectedUser != null;
        }

        private void ExecutePlay(object parameter)
        {
            // 1. Safety Check
            if (SelectedUser == null)
            {
                MessageBox.Show("Please select a user to play.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Create the GameViewModel, passing the selected user
                GameViewModel gameViewModel = new GameViewModel(SelectedUser, UpdateUserStats);

                // 3. Create the GameWindow
                GameWindow gameWindow = new GameWindow();

                // 4. Set the DataContext for the GameWindow
                gameWindow.DataContext = gameViewModel;

                // 5. Get a reference to the current MainWindow (which hosts SignInView)
                Window currentMainWindow = Application.Current.MainWindow;

                // 6. Show the new GameWindow
                //    Show() makes it non-modal, allowing interaction with other windows if any were open.
                //    If you wanted it modal (though unlikely for the main game window), use ShowDialog().
                gameWindow.Show();

                // 7. Close the current MainWindow (hosting SignInView)
                //    Check if it exists before trying to close.
                if (currentMainWindow != null)
                {
                    // Important: Setting the new GameWindow as the MainWindow *before* closing
                    // the old one can sometimes help with application lifetime management,
                    // especially if the original MainWindow closing triggers App shutdown.
                    Application.Current.MainWindow = gameWindow; // Set the new window as the main one
                    currentMainWindow.Close(); // Close the old sign-in window
                }
                else
                {
                    // Fallback if the original MainWindow reference was lost somehow
                    System.Diagnostics.Debug.WriteLine("Warning: Could not find original MainWindow to close in ExecutePlay.");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log the full exception details for debugging
                System.Diagnostics.Debug.WriteLine($"ERROR starting game: {ex.ToString()}");
            }
        }

        private bool CanExecutePlay(object parameter)
        {
            return SelectedUser != null;
        }

        private void ExecuteCancel(object parameter)
        {
            // Close the main application window
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Close();
            }
            else
            {
                Application.Current.Shutdown(); // Fallback
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}