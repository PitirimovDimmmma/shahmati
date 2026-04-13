using shahmati.Models;
using shahmati.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace shahmati.Views
{
    public partial class TrainingWindow : Window, INotifyPropertyChanged
    {
        private readonly TrainingViewModel _viewModel;
        private DispatcherTimer _timer;
        private readonly int _userId;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TrainingWindow(int userId, TrainingTypeDto? training = null)
        {
            _userId = userId;
            InitializeComponent();

            _viewModel = new TrainingViewModel(userId);
            if (training != null)
            {
                _viewModel.SelectedTraining = training;
            }

            DataContext = _viewModel;
            InitializeTimer();

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += async (s, e) => await LoadTrainingAsync();
        }

        private async System.Threading.Tasks.Task LoadTrainingAsync()
        {
            try
            {
                await _viewModel.LoadTrainingsAsync();

                if (_viewModel.SelectedTraining != null)
                {
                    await _viewModel.StartTraining();
                }
                else
                {
                    _viewModel.StatusText = "Выберите тренировку для начала";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренировки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _viewModel.UpdateTimer();

            Dispatcher.Invoke(() =>
            {
                if (_viewModel.CurrentPositions.Count > 0)
                {
                    double progress = (double)(_viewModel.CurrentPositionIndex) / _viewModel.CurrentPositions.Count * 100;
                    TrainingProgressBar.Value = progress;
                    ProgressPercentageText.Text = $"{progress:F0}%";
                }
            });
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(TrainingViewModel.PositionProgress):
                        ProgressText.Text = _viewModel.PositionProgress;
                        break;
                    case nameof(TrainingViewModel.TimeElapsed):
                        TimerText.Text = _viewModel.TimeElapsed ?? "00:00";
                        break;
                    case nameof(TrainingViewModel.PositionTask):
                        PositionTaskText.Text = _viewModel.PositionTask;
                        break;
                    case nameof(TrainingViewModel.ExplanationText):
                        ExplanationTextBlock.Text = _viewModel.ExplanationText;
                        break;
                    case nameof(TrainingViewModel.StatusText):
                        StatusText.Text = _viewModel.StatusText;
                        break;
                    case nameof(TrainingViewModel.CurrentPosition):
                        if (_viewModel.CurrentPosition != null)
                        {
                            PositionTitleText.Text = $"ПОЗИЦИЯ #{_viewModel.CurrentPositionIndex + 1}";
                        }
                        break;
                }
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsTrainingCompleted)
            {
                var result = MessageBox.Show(
                    "Тренировка не завершена. Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            var trainingSelectionWindow = new TrainingSelectionWindow(_userId);
            trainingSelectionWindow.Show();
            Close();
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите досрочно завершить тренировку?",
                "Досрочное завершение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.CompleteTrainingEarly();
                var trainingSelectionWindow = new TrainingSelectionWindow(_userId);
                trainingSelectionWindow.Show();
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
                e.Cancel = result != MessageBoxResult.Yes;
            }
            _timer?.Stop();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}