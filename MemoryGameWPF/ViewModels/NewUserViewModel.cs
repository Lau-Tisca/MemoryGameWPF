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
        private readonly ObservableCollection<User> _usersCollection;
        private readonly Func<bool> _saveAction; // Delegate to call the SaveUsers method in SignInViewModel

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
                SaveUserCommand.RaiseCanExecuteChanged();
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
                LoadImagePreview();
                SaveUserCommand.RaiseCanExecuteChanged();
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
            SaveUserCommand = new RelayCommand<object>(ExecuteSaveUser, CanExecuteSaveUser);
            CancelNewUserCommand = new RelayCommand<object>(ExecuteCancelNewUser);

            UserName = "";
            SelectedImagePath = "";
            SelectedImageSource = null;
        }

        public NewUserViewModel(ObservableCollection<User> users, Func<bool> saveAction)
        {
            _usersCollection = users ?? throw new ArgumentNullException(nameof(users));
            _saveAction = saveAction ?? throw new ArgumentNullException(nameof(saveAction));

            BrowseImageCommand = new RelayCommand<object>(ExecuteBrowseImage);
            SaveUserCommand = new RelayCommand<object>(ExecuteSaveUser, CanExecuteSaveUser);
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
            if (string.IsNullOrWhiteSpace(UserName))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_usersCollection.Any(u => u.UserName.Equals(UserName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Username '{UserName}' already exists. Please choose another.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(SelectedImagePath))
            {
                MessageBox.Show("Please select an image for the user.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string imagePathToSave = SelectedImagePath;

            User newUser = new User(UserName, imagePathToSave);

            _usersCollection.Add(newUser);

            if (_saveAction())
            {
                MessageBox.Show($"User '{UserName}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(parameter, true); 
            }
            else
            {
                _usersCollection.Remove(newUser);
                MessageBox.Show($"Failed to save user data. User '{UserName}' was not created.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSaveUser(object parameter)
        {
            bool hasUserName = !string.IsNullOrWhiteSpace(UserName);
            bool hasImagePath = !string.IsNullOrEmpty(SelectedImagePath);
            return hasUserName && hasImagePath;
        }

        private void ExecuteCancelNewUser(object parameter)
        {
            CloseWindow(parameter, false);
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
                    bitmap.UriSource = new Uri(SelectedImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    SelectedImageSource = bitmap;
                }
                catch (Exception ex) 
                {
                    System.Windows.MessageBox.Show($"Error loading image preview:\n{SelectedImagePath}\n{ex.Message}", "Image Preview Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SelectedImageSource = null;
                }
            }
            else
            {
                SelectedImageSource = null;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}