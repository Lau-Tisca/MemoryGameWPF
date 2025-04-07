using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemoryGameWPF.Models;
using MemoryGameWPF.Views;
using Microsoft.Win32;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Windows;

namespace MemoryGameWPF.ViewModels
{
    public class NewUserViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<User> _usersCollection; // Reference to the main Users list
        private readonly Func<bool> _saveAction; // Delegate to call the SaveUsers method in SignInViewModel

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _selectedImagePath;
        public string SelectedImagePath
        {
            get { return _selectedImagePath; }
            set
            {
                _selectedImagePath = value;
                OnPropertyChanged();
                LoadImagePreview(); // Load and set ImageSource when path changes
            }
        }

        private ImageSource _selectedImageSource;
        public ImageSource SelectedImageSource
        {
            get { return _selectedImageSource; }
            set
            {
                _selectedImageSource = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<object> BrowseImageCommand { get; }
        public RelayCommand<object> SaveUserCommand { get; }
        public RelayCommand<object> CancelNewUserCommand { get; }

        public NewUserViewModel()
        {
            BrowseImageCommand = new RelayCommand<object>(ExecuteBrowseImage);
            SaveUserCommand = new RelayCommand<object>(ExecuteSaveUser);
            CancelNewUserCommand = new RelayCommand<object>(ExecuteCancelNewUser);

            // Initialize with default values if needed
            UserName = "";
            SelectedImagePath = "";
            SelectedImageSource = null;
        }

        public NewUserViewModel(ObservableCollection<User> users, Func<bool> saveAction)
        {
            _usersCollection = users ?? throw new ArgumentNullException(nameof(users));
            _saveAction = saveAction ?? throw new ArgumentNullException(nameof(saveAction));

            BrowseImageCommand = new RelayCommand<object>(ExecuteBrowseImage);
            SaveUserCommand = new RelayCommand<object>(ExecuteSaveUser, CanExecuteSaveUser); // Add CanExecute
            CancelNewUserCommand = new RelayCommand<object>(ExecuteCancelNewUser);

            UserName = "";
            SelectedImagePath = "";
            SelectedImageSource = null;
        }

        private void ExecuteBrowseImage(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedImagePath = openFileDialog.FileName;
            }
        }

        private void ExecuteSaveUser(object parameter)
        {
            // 1. Validate UserName
            if (string.IsNullOrWhiteSpace(UserName))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Add uniqueness check
            if (_usersCollection.Any(u => u.UserName.Equals(UserName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Username '{UserName}' already exists. Please choose another.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Validate image selection (optional, depending on requirements)
            if (string.IsNullOrEmpty(SelectedImagePath))
            {
                MessageBox.Show("Please select an image for the user.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. TODO: Copy selected image to a local application folder (BEST PRACTICE)
            //    - Create an "Avatars" or "UserImages" folder within your app's data directory.
            //    - Generate a unique filename (e.g., using Guid or username + timestamp).
            //    - Copy the file from SelectedImagePath to the new location.
            //    - Update SelectedImagePath to the *new* relative path within your app's folder.
            //    - This prevents issues if the user later moves or deletes the original image.
            //    - For now, we'll just use the original path, but be aware of this limitation.
            string imagePathToSave = SelectedImagePath; // Placeholder - implement copying later

            // 3. Create new User object
            User newUser = new User(UserName, imagePathToSave);

            // 4. Add the new user to the shared Users collection
            _usersCollection.Add(newUser);

            // 5. Call the SaveAction delegate to trigger saving in SignInViewModel
            if (_saveAction()) // Check if save was successful
            {
                MessageBox.Show($"User '{UserName}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(parameter, true); // Close window after successful save
            }
            else
            {
                // Save failed, remove the user we just added to keep consistency
                _usersCollection.Remove(newUser);
                MessageBox.Show($"Failed to save user data. User '{UserName}' was not created.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Don't close the window if save failed
            }
        }

        private bool CanExecuteSaveUser(object parameter)
        {
            // Enable Save only if username is entered and an image is selected
            return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrEmpty(SelectedImagePath);
        }

        private void ExecuteCancelNewUser(object parameter)
        {
            CloseWindow(parameter, false); // Close window, indicating cancellation
        }

        private void CloseWindow(object parameter, bool dialogResult)
        {
            // Try to find the associated window and close it
            if (parameter is Window window) // Check if the command parameter is the window itself
            {
                try
                {
                    window.DialogResult = dialogResult; // Set DialogResult for ShowDialog()
                    window.Close();
                }
                catch (InvalidOperationException)
                {
                    // Window might have already been closed or wasn't shown as dialog
                    // Find window associated with this DataContext (less reliable)
                    foreach (Window openWindow in Application.Current.Windows)
                    {
                        if (openWindow.DataContext == this)
                        {
                            openWindow.DialogResult = dialogResult;
                            openWindow.Close();
                            break;
                        }
                    }
                }
            }
            else
            {
                // Fallback if parameter isn't the window (less ideal)
                // You might need a more robust window closing mechanism (e.g., passing Window reference, using a service)
                Console.WriteLine("Warning: Could not determine window to close from command parameter.");
                // Try finding the window anyway
                foreach (Window openWindow in Application.Current.Windows)
                {
                    if (openWindow.DataContext == this && openWindow is NewUserView) // Be specific
                    {
                        openWindow.DialogResult = dialogResult; // Attempt to set DialogResult
                        openWindow.Close();
                        break;
                    }
                }
            }
        }



        private void LoadImagePreview()
        {
            if (!string.IsNullOrEmpty(SelectedImagePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(SelectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Cache for better performance
                    bitmap.EndInit();
                    SelectedImageSource = bitmap;
                }
                catch (IOException ex)
                {
                    // Handle image loading errors (e.g., invalid file path, corrupted image)
                    System.Windows.MessageBox.Show($"Error loading image: {ex.Message}", "Image Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    SelectedImageSource = null; // Clear preview on error
                }
            }
            else
            {
                SelectedImageSource = null; // Clear preview if no image path
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}