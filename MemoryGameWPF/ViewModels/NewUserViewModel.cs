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

namespace MemoryGameWPF.ViewModels
{
    public class NewUserViewModel : INotifyPropertyChanged
    {
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

        public ICommand BrowseImageCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelNewUserCommand { get; }

        public NewUserViewModel()
        {
            BrowseImageCommand = new RelayCommand(ExecuteBrowseImage);
            SaveUserCommand = new RelayCommand(ExecuteSaveUser);
            CancelNewUserCommand = new RelayCommand(ExecuteCancelNewUser);

            // Initialize with default values if needed
            UserName = "";
            SelectedImagePath = "";
            SelectedImageSource = null;
        }

        private void ExecuteBrowseImage(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures); // Optional: start in Pictures folder

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedImagePath = openFileDialog.FileName; // Set the selected path, which will trigger LoadImagePreview
            }
        }

        private void ExecuteSaveUser(object parameter)
        {
            // TODO: Implement Save User Logic here
            // 1. Validate UserName (e.g., not empty, unique)
            if (string.IsNullOrWhiteSpace(UserName))
            {
                // Display error message to user - e.g., using MessageBox.Show
                System.Windows.MessageBox.Show("Username cannot be empty.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return; // Stop saving if validation fails
            }

            // 2. Create new User object
            User newUser = new User(UserName, SelectedImagePath); // Assuming User constructor takes name and image path

            // 3. TODO: Add the new user to the Users collection in SignInViewModel (You'll need to find a way to pass or access SignInViewModel)

            // 4. TODO: Save the updated user data to file

            // 5. TODO: Close the New User Window (You'll need to find a way to access and close the window from here)
            System.Windows.MessageBox.Show($"User '{UserName}' saved! (Image path: {SelectedImagePath})", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information); // Placeholder success message
            ExecuteCancelNewUser(null); // For now, just close the window after "saving" (replace with proper window closing logic later)
        }

        private void ExecuteCancelNewUser(object parameter)
        {
            // TODO: Implement Cancel Logic - Close the New User Window
            // You'll need a way to access and close the window from here.
            // For example, if you opened it as a dialog, you might need to set DialogResult = false;
            System.Windows.MessageBox.Show("New user creation cancelled.", "Cancelled", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information); // Placeholder message
            // For now, just placeholder - you'll need to close the actual window/dialog properly
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