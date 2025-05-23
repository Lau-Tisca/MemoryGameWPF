﻿using MemoryGameWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace MemoryGameWPF.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private const string StatsDataFileName = "game_stats.json";
        private readonly string _statsDataFilePath;

        private ObservableCollection<StatisticsEntryViewModel> _statisticsEntries;
        public ObservableCollection<StatisticsEntryViewModel> StatisticsEntries
        {
            get => _statisticsEntries;
            set { _statisticsEntries = value; OnPropertyChanged(); }
        }

        public StatisticsViewModel()
        {
            _statsDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatsDataFileName);
            StatisticsEntries = new ObservableCollection<StatisticsEntryViewModel>();
            LoadAndPrepareStatistics();
        }

        private void LoadAndPrepareStatistics()
        {
            Dictionary<string, UserStats> allUserStats = null;

            if (!File.Exists(_statsDataFilePath))
            {
                allUserStats = new Dictionary<string, UserStats>();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(_statsDataFilePath);
                    allUserStats = JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json);
                    if (allUserStats == null) allUserStats = new Dictionary<string, UserStats>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading game statistics:\n{ex.Message}", "Statistics Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    allUserStats = new Dictionary<string, UserStats>();
                }
            }

            var sortedEntries = allUserStats
                .Select(kvp => new StatisticsEntryViewModel
                {
                    UserName = kvp.Key,
                    GamesPlayed = kvp.Value.GamesPlayed,
                    GamesWon = kvp.Value.GamesWon
                })
                .OrderByDescending(entry => entry.GamesWon)
                .ThenByDescending(entry => entry.GamesPlayed)
                .ThenBy(entry => entry.UserName);

            foreach (var entry in sortedEntries)
            {
                StatisticsEntries.Add(entry);
            }

            if (!StatisticsEntries.Any())
            {
                System.Diagnostics.Debug.WriteLine("No statistics data found to display.");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}