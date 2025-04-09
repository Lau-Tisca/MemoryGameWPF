using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
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
            _userDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UserDataFileName);
            _statsDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatsDataFileName);

            LoadUsers();
            LoadStatistics();

            NewUserCommand = new RelayCommand<object>(ExecuteNewUser);
            DeleteUserCommand = new RelayCommand<object>(ExecuteDeleteUser, CanExecuteDeleteUser);
            PlayCommand = new RelayCommand<object>(ExecutePlay, CanExecutePlay);
            CancelCommand = new RelayCommand<object>(ExecuteCancel);
        }

        private void LoadStatistics()
        {
            if (!File.Exists(_statsDataFilePath))
            {
                _allUserStats = new Dictionary<string, UserStats>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_statsDataFilePath);
                _allUserStats = JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json);
                if (_allUserStats == null)
                {
                    _allUserStats = new Dictionary<string, UserStats>();
                    System.Diagnostics.Debug.WriteLine("Warning: Statistics file was empty or invalid. Initialized new statistics.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading game statistics from {_statsDataFilePath}:\n{ex.Message}", "Statistics Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _allUserStats = new Dictionary<string, UserStats>();
            }
        }

        private bool SaveStatistics()
        {
            if (_allUserStats == null)
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

        private void UpdateUserStats(string username, bool wonGame)
        {
            if (string.IsNullOrEmpty(username) || _allUserStats == null) return;

            if (!_allUserStats.TryGetValue(username, out UserStats stats))
            {
                stats = new UserStats();
                _allUserStats[username] = stats;
            }

            stats.GamesPlayed++;
            if (wonGame)
            {
                stats.GamesWon++;
            }

            System.Diagnostics.Debug.WriteLine($"Stats Updated for {username}: Played={stats.GamesPlayed}, Won={stats.GamesWon}");

            SaveStatistics();
        }

        private void LoadUsers()
        {
            if (!File.Exists(_userDataFilePath))
            {
                Users = new ObservableCollection<User>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_userDataFilePath);
                var loadedUsers = JsonSerializer.Deserialize<ObservableCollection<User>>(json);
                Users = loadedUsers ?? new ObservableCollection<User>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user data from {_userDataFilePath}:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Users = new ObservableCollection<User>();
            }
        }

        private bool SaveUsers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Users, options);
                File.WriteAllText(_userDataFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user data to {_userDataFilePath}:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void ExecuteNewUser(object parameter)
        {
            NewUserViewModel newUserViewModel = new NewUserViewModel(Users, SaveUsers);
            NewUserView newUserView = new NewUserView();
            newUserView.DataContext = newUserViewModel;

            if (Application.Current.MainWindow != null && Application.Current.MainWindow is Window ownerWindow)
            {
                newUserView.Owner = ownerWindow;
            }

            newUserView.ShowDialog();
        }

        private void ExecuteDeleteUser(object parameter)
        {
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
                return;
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

                    if (!SaveUsers())
                    {
                        MessageBox.Show($"CRITICAL ERROR: Failed to update user data file after removing '{userNameToDelete}'. Deletion incomplete.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Users.Add(userToDelete);
                        return;
                    }

                    try
                    {
                        Users.Remove(userToDelete);
                        SelectedUser = null;

                        if (!SaveUsers())
                        {
                            MessageBox.Show($"CRITICAL ERROR: Failed to update user data file after removing '{userNameToDelete}'.\nUser deletion incomplete. Please check file permissions or disk space.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Users.Add(userToDelete);
                            return;
                        }

                        if (!string.IsNullOrEmpty(imagePathToDelete) && File.Exists(imagePathToDelete))
                        {
                            try
                            {
                                File.Delete(imagePathToDelete);
                                System.Diagnostics.Debug.WriteLine($"Deleted image file: {imagePathToDelete}");
                            }
                            catch (IOException ioEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not delete image file {imagePathToDelete}. Reason: {ioEx.Message}");
                            }
                            catch (UnauthorizedAccessException authEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Permission denied deleting image file {imagePathToDelete}. Reason: {authEx.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Unexpected error deleting image file {imagePathToDelete}. Reason: {ex.Message}");
                            }
                        }

                        string saveGamePattern = $"{userNameToDelete}_*.sav";
                        string saveGameDirectory = AppDomain.CurrentDomain.BaseDirectory; 

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
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Warning: Could not delete save file {file}. Reason: {ex.Message}");
                                    }
                                }
                                MessageBox.Show($"Associated save files for '{userNameToDelete}' deleted.", "Cleanup Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No save files found matching pattern '{saveGamePattern}' for user '{userNameToDelete}'.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Could not search for/delete save files for {userNameToDelete}. Reason: {ex.Message}");
                        }

                        if (_allUserStats != null && _allUserStats.Remove(userNameToDelete))
                        {
                            System.Diagnostics.Debug.WriteLine($"Removed statistics for user '{userNameToDelete}' from memory.");
                            if (!SaveStatistics())
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not process/delete statistics for {userNameToDelete}. Reason: {ex.Message}");
                    }

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
            if (SelectedUser == null)
            {
                MessageBox.Show("Please select a user to play.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                GameViewModel gameViewModel = new GameViewModel(SelectedUser, UpdateUserStats);

                GameWindow gameWindow = new GameWindow();

                gameWindow.DataContext = gameViewModel;

                Window currentMainWindow = Application.Current.MainWindow;

                gameWindow.Show();

                if (currentMainWindow != null)
                {
                    Application.Current.MainWindow = gameWindow;
                    currentMainWindow.Close();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Could not find original MainWindow to close in ExecutePlay.");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"ERROR starting game: {ex.ToString()}");
            }
        }

        private bool CanExecutePlay(object parameter)
        {
            return SelectedUser != null;
        }

        private void ExecuteCancel(object parameter)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}