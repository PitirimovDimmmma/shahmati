using shahmati.Models;
using shahmati.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace shahmati.Views
{
    public partial class TrainingWindow : Window, INotifyPropertyChanged
    {
        private readonly TrainingViewModel _viewModel;
        private DispatcherTimer _timer;
        private int _userId;
        private string _trainingName;
        private string _trainingDescription;

        public event PropertyChangedEventHandler PropertyChanged;

        // Свойства для привязки
        public string TrainingName
        {
            get => _trainingName;
            set
            {
                _trainingName = value;
                OnPropertyChanged();
            }
        }

        public string TrainingDescription
        {
            get => _trainingDescription;
            set
            {
                _trainingDescription = value;
                OnPropertyChanged();
            }
        }

        public TrainingWindow(int userId, TrainingTypeDto training = null)
        {
            _userId = userId;
            InitializeComponent();

            if (training != null)
            {
                TrainingName = training.Name;
                TrainingDescription = training.Description;
            }

            _viewModel = new TrainingViewModel(userId);
            if (training != null)
            {
                _viewModel.SelectedTraining = training;
            }

            DataContext = _viewModel;
            InitializeTimer();
            LoadTraining();
        }

        private async void LoadTraining()
        {
            await _viewModel.LoadTrainingsAsync();
            if (_viewModel.SelectedTraining != null)
            {
                await _viewModel.StartTraining();
                UpdateUI();
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _viewModel.UpdateTimer();
        }

        private void UpdateUI()
        {
            if (_viewModel.SelectedTraining != null)
            {
                TrainingNameText.Text = _viewModel.SelectedTraining.Name;
                TrainingDescriptionText.Text = _viewModel.SelectedTraining.Description;
            }

            if (_viewModel.CurrentPosition != null)
            {
                PositionTitleText.Text = $"ПОЗИЦИЯ #{_viewModel.CurrentPositionIndex + 1}";
                PositionTaskText.Text = _viewModel.PositionTask;

                // Обновляем список ходов решения
                SolutionMovesList.Items.Clear();
                if (_viewModel.CurrentPosition?.SolutionMoves != null)
                {
                    foreach (var move in _viewModel.CurrentPosition.SolutionMoves)
                    {
                        SolutionMovesList.Items.Add(move);
                    }
                }
            }

            // Обновляем прогресс
            TrainingProgressBar.Value = _viewModel.CurrentPositionIndex;
            TrainingProgressBar.Maximum = Math.Max(1, _viewModel.CurrentPositions.Count);

            ProgressText.Text = $"{_viewModel.CurrentPositionIndex + 1}/{_viewModel.CurrentPositions.Count}";
            MistakesText.Text = _viewModel.Mistakes.ToString();
            HintText.Text = _viewModel.HintText;
            StatusText.Text = _viewModel.StatusText;
        }

        // Обработчики событий кнопок
        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.NextPosition();
            UpdateUI();
        }

        private void ShowSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowHint();
            UpdateUI();
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowHint();
            UpdateUI();
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.CompleteTraining();
            if (_viewModel.IsTrainingCompleted)
            {
                Close();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_viewModel.IsTrainingCompleted)
            {
                var result = MessageBox.Show(
                    "Тренировка не завершена. Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }

            _timer?.Stop();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}