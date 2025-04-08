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
        private const string UserDataFileName = "users.json"; // Name of the data file
        private readonly string _userDataFilePath; // Full path to the data file

        private ObservableCollection<User> _users;
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
            // Alternative: Use ApplicationData folder for better practice
            // string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // string appFolder = Path.Combine(appDataPath, "YourMemoryGame"); // Create a folder for your app
            // Directory.CreateDirectory(appFolder); // Ensure folder exists
            // _userDataFilePath = Path.Combine(appFolder, UserDataFileName);

            LoadUsers(); // Load users when ViewModel is created

            NewUserCommand = new RelayCommand<object>(ExecuteNewUser);
            DeleteUserCommand = new RelayCommand<object>(ExecuteDeleteUser, CanExecuteDeleteUser);
            PlayCommand = new RelayCommand<object>(ExecutePlay, CanExecutePlay);
            CancelCommand = new RelayCommand<object>(ExecuteCancel);
        }

        // --- Load and Save User Data ---

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

        // --- Command Execution Methods ---

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
            if (SelectedUser == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete user '{SelectedUser.UserName}'?\nThis will remove all associated data (user profile, saved games, stats).",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                User userToDelete = SelectedUser; // Store user before clearing selection

                // 1. Remove from ObservableCollection (updates UI)
                Users.Remove(userToDelete);

                // 2. Save updated user list to file
                if (!SaveUsers())
                {
                    // If saving failed, potentially add the user back to the list for consistency? Or just warn.
                    MessageBox.Show("Failed to update user data file after deletion.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Consider adding userToDelete back to Users list here if save failed.
                    // Users.Add(userToDelete); // Re-add if save failed? Decide on desired behavior.
                    return; // Stop further deletion steps if save failed
                }

                // 3. Delete associated image file (Optional, with error handling)
                if (!string.IsNullOrEmpty(userToDelete.ImagePath) && File.Exists(userToDelete.ImagePath))
                {
                    try
                    {
                        File.Delete(userToDelete.ImagePath);
                    }
                    catch (Exception ex)
                    {
                        // Log or show a non-critical warning, as the main user data is deleted
                        Console.WriteLine($"Warning: Could not delete image file {userToDelete.ImagePath}: {ex.Message}");
                        // MessageBox.Show($"Warning: Could not delete image file:\n{ex.Message}", "File Deletion Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // 4. TODO: Delete saved game data for this user
                //    Example: Find files matching pattern "{userToDelete.UserName}_*.sav" and delete them
                try
                {
                    string searchPattern = $"{userToDelete.UserName}_*.sav"; // Adjust pattern as needed
                    string directory = AppDomain.CurrentDomain.BaseDirectory; // Or wherever saves are stored
                    string[] saveFiles = Directory.GetFiles(directory, searchPattern);
                    foreach (string file in saveFiles)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted save file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete save files for {userToDelete.UserName}: {ex.Message}");
                }


                // 5. TODO: Delete statistics for this user
                //    - This depends heavily on how stats are stored. If in a single file,
                //      you'd load the stats, remove the user's entry, and save back.
                Console.WriteLine($"Placeholder: Delete stats for {userToDelete.UserName}");


                MessageBox.Show($"User '{userToDelete.UserName}' and associated data deleted.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                SelectedUser = null; // Clear selection
            }
        }

        private bool CanExecuteDeleteUser(object parameter)
        {
            return SelectedUser != null;
        }

        private void ExecutePlay(object parameter)
        {
            //if (SelectedUser == null) return;

            //// 1. Create the GameViewModel, passing the selected user
            //GameViewModel gameViewModel = new GameViewModel(SelectedUser); // Assuming constructor takes User

            //// 2. Create the GameWindow
            //GameWindow gameWindow = new GameWindow();

            //// 3. Set the DataContext
            //gameWindow.DataContext = gameViewModel;

            //// 4. Show the GameWindow
            //gameWindow.Show();

            //// 5. Close the current SignIn/MainWindow
            //// This assumes the current Application.Current.MainWindow *is* the sign-in window.
            //// Be cautious if your application structure is different.
            //Application.Current.MainWindow?.Close();
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

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}