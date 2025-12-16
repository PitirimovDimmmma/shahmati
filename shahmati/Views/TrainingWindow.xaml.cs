using shahmati.ViewModels;
using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace shahmati.Views
{
    public partial class TrainingWindow : Window
    {
        private readonly TrainingViewModel _viewModel;
        private readonly int _userId;
        private readonly TrainingTypeDto _selectedTraining;
        private DispatcherTimer _timer;

        public TrainingWindow(int userId, TrainingTypeDto selectedTraining)
        {
            InitializeComponent();
            _userId = userId;
            _selectedTraining = selectedTraining;
            _viewModel = new TrainingViewModel(userId);
            DataContext = _viewModel;

            Loaded += async (s, e) => await InitializeTraining();
        }

        private async Task InitializeTraining()
        {
            // Устанавливаем выбранную тренировку
            _viewModel.SelectedTraining = _selectedTraining;

            // Загружаем данные тренировки
            TrainingNameText.Text = _selectedTraining.Name;
            TrainingDescriptionText.Text = _selectedTraining.Description;

            // Начинаем тренировку
            await _viewModel.StartTraining();

            // Запускаем таймер обновления UI
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Обновляем UI элементы
            TimerText.Text = _viewModel.TimeElapsed;
            ScoreText.Text = $"{_viewModel.Score} очков";
            MistakesText.Text = $"{_viewModel.Mistakes} ошибок";

            if (_viewModel.CurrentPosition != null)
            {
                // Используем CurrentPositionIndex и CurrentPositions
                PositionTitleText.Text = $"Позиция {_viewModel.CurrentPositionIndex + 1} из {_viewModel.CurrentPositions.Count}";
                PositionTaskText.Text = $"Найдите лучший ход... ({_viewModel.CurrentPosition.Theme})";

                // Обновляем прогресс
                int progress = 0;
                if (_viewModel.CurrentPositions.Count > 0)
                {
                    progress = (int)((double)(_viewModel.CurrentPositionIndex + 1) / _viewModel.CurrentPositions.Count * 100);
                }
                TrainingProgressBar.Value = progress;
                ProgressText.Text = $"{_viewModel.CurrentPositionIndex + 1}/{_viewModel.CurrentPositions.Count}";
            }
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowHint();
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Завершить тренировку досрочно?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.CompleteTraining();
                _timer?.Stop();

                // Возвращаемся к выбору тренировок
                TrainingSelectionWindow selectionWindow = new TrainingSelectionWindow(_userId);
                selectionWindow.Show();
                this.Close();
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.NextPosition();
        }

        private void ShowSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPosition != null && !string.IsNullOrEmpty(_viewModel.CurrentPosition.Solution))
            {
                SolutionMovesList.Items.Clear();
                var moves = _viewModel.CurrentPosition.Solution.Split(' ');
                foreach (var move in moves)
                {
                    if (!string.IsNullOrWhiteSpace(move))
                        SolutionMovesList.Items.Add(move);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            base.OnClosed(e);
        }

        // Свойства для удобства доступа
        public int CurrentPositionIndex => _viewModel.CurrentPositionIndex;
        public List<TrainingPositionDto> CurrentPositions => _viewModel.CurrentPositions;
    }
}