using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MemoryGameWPF.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MemoryGameWPF.ViewModels
{
    // ViewModel for the SignInView
    // This class handles the logic for user sign-in, including user selection and command execution
    // It implements INotifyPropertyChanged to notify the view of property changes
    public class SignInViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
                _selectedUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagePath));
                UpdateCommandStates(); // Update button enabled states when selection changes
            }
        }

        public string ImagePath
        {
            get { return SelectedUser?.ImagePath; } // Return the image path of the selected user
        }

        private void UpdateCommandStates()
        {
            // Logic to enable/disable buttons based on SelectedUser

            // Example: Enable "Delete User" and "Play" buttons only if a user is selected
            //DeleteUserCommand.RaiseCanExecuteChanged(); // Trigger re-evaluation of CanExecute for DeleteUserCommand
            //PlayCommand.RaiseCanExecuteChanged();     // Trigger re-evaluation of CanExecute for PlayCommand
        }
        public string img { get; set; } = "pack://application:,,,/MemoryGameWPF;component/Images/lion.jpg"; // Default image path
        public ICommand NewUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand CancelCommand { get; }

        public SignInViewModel()
        {
            // Placeholder users - replace with file loading later
            Users = new ObservableCollection<User>()
        {
            new User("Corina", "../Images/lion.jpg"),
            new User("Raul", "pack://application:,,,/MemoryGameWPF;component/Images/cat.jpg"),
            new User("Simona", "../Images/dog.jpg"),
            new User("Maria", "../Images/panda.jpg"),
            new User("Andrei", "../Images/tiger.jpg")
        };

            //NewUserCommand = new RelayCommand(ExecuteNewUser);
            //DeleteUserCommand = new RelayCommand(ExecuteDeleteUser, CanExecuteDeleteUser);
            //PlayCommand = new RelayCommand(ExecutePlay, CanExecutePlay);
            //CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private void ExecuteNewUser(object parameter)
        {
            // ... (Logic for New User) ...
        }

        private void ExecuteDeleteUser(object parameter)
        {
            // ... (Logic for Delete User) ...
        }

        private bool CanExecuteDeleteUser(object parameter)
        {
            return SelectedUser != null; // Enabled only if a user is selected
        }

        private void ExecutePlay(object parameter)
        {
            // ... (Logic for Play) ...
        }

        private bool CanExecutePlay(object parameter)
        {
            return SelectedUser != null; // Enabled only if a user is selected
        }

        private void ExecuteCancel(object parameter)
        {
            // ... (Logic for Cancel) ...
        }

        // ... (Commands and other logic will be added) ...
    }
}
