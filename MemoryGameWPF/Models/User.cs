using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF.Models
{
    public class User:INotifyPropertyChanged
    {
        public string UserName { get; set; }
        private string imagePath;
        public string ImagePath { get { return imagePath; } set { imagePath = value; OnPropertyChanged(nameof(ImagePath)); } }

        public User(string userName, string imagePath) // Constructor for easier User creation
        {
            UserName = userName;
            ImagePath = imagePath;
        }

        public User() // Default constructor (needed for some serialization)
        {

        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

}
