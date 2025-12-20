using shahmati.ViewModels;
using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Linq;

namespace shahmati.Views
{
    public partial class TrainingWindow : Window
    {
        private readonly TrainingViewModel _viewModel;
        private readonly int _userId;
        private readonly TrainingTypeDto _selectedTraining;
        private DispatcherTimer _timer;
        private DispatcherTimer _updateTimer;
        private bool _isSolutionShown;

        public TrainingWindow(int userId, TrainingTypeDto selectedTraining)
        {
            InitializeComponent();
            _userId = userId;
            _selectedTraining = selectedTraining;
            _viewModel = new TrainingViewModel(userId);
            DataContext = _viewModel;

            Loaded += async (s, e) => await InitializeTraining();
            Closed += (s, e) => Cleanup();
        }

        private async Task InitializeTraining()
        {
            try
            {
                // Устанавливаем выбранную тренировку
                _viewModel.SelectedTraining = _selectedTraining;

                // Загружаем данные тренировки
                TrainingNameText.Text = _selectedTraining.Name;
                TrainingDescriptionText.Text = _selectedTraining.Description;

                // Запускаем таймеры
                StartTimers();

                // Начинаем тренировку
                await _viewModel.StartTraining();

                // Загружаем решение если есть
                LoadSolutionMoves();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации тренировки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTimers()
        {
            // Таймер обновления UI
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Таймер обновления времени
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Обновляем UI элементы
                if (_viewModel != null)
                {
                    TimerText.Text = _viewModel.TimeElapsed ?? "00:00";
                    ScoreText.Text = $"{_viewModel.Score} очков";
                    MistakesText.Text = $"{_viewModel.Mistakes} ошибок";

                    if (_viewModel.CurrentPosition != null && _viewModel.CurrentPositions != null)
                    {
                        // Используем CurrentPositionIndex и CurrentPositions
                        PositionTitleText.Text = $"ПОЗИЦИЯ #{_viewModel.CurrentPositionIndex + 1}";

                        if (!string.IsNullOrEmpty(_viewModel.PositionTask))
                        {
                            PositionTaskText.Text = _viewModel.PositionTask;
                        }
                        else
                        {
                            PositionTaskText.Text = $"Найдите лучший ход. Тема: {_viewModel.CurrentPosition.Theme}";
                        }

                        // Обновляем прогресс
                        if (_viewModel.CurrentPositions.Count > 0)
                        {
                            int progress = (int)((double)(_viewModel.CurrentPositionIndex + 1) / _viewModel.CurrentPositions.Count * 100);
                            TrainingProgressBar.Value = progress;
                            ProgressText.Text = $"{_viewModel.CurrentPositionIndex + 1}/{_viewModel.CurrentPositions.Count}";
                        }

                        // Обновляем подсказку
                        if (!string.IsNullOrEmpty(_viewModel.HintText))
                        {
                            HintText.Text = _viewModel.HintText;
                        }
                    }

                    // Обновляем статус
                    if (!string.IsNullOrEmpty(_viewModel.StatusText))
                    {
                        StatusText.Text = _viewModel.StatusText;
                    }

                    // Обновляем кнопки в зависимости от состояния
                    NextButton.IsEnabled = _viewModel.CurrentPositions != null &&
                                          _viewModel.CurrentPositionIndex < _viewModel.CurrentPositions.Count - 1 &&
                                          !_viewModel.IsTrainingCompleted;

                    CompleteButton.IsEnabled = !_viewModel.IsTrainingCompleted;
                    HintButton.IsEnabled = !_viewModel.IsTrainingCompleted;
                    ShowSolutionButton.IsEnabled = !_viewModel.IsTrainingCompleted &&
                                                   _viewModel.CurrentPosition != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления UI: {ex.Message}");
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            _viewModel?.UpdateTimer();
        }

        private void LoadSolutionMoves()
        {
            try
            {
                SolutionMovesList.Items.Clear();
                _isSolutionShown = false;

                if (_viewModel.CurrentPosition != null &&
                    _viewModel.CurrentPosition.SolutionMoves != null &&
                    _viewModel.CurrentPosition.SolutionMoves.Any())
                {
                    foreach (var move in _viewModel.CurrentPosition.SolutionMoves.Take(10))
                    {
                        if (!string.IsNullOrWhiteSpace(move))
                        {
                            SolutionMovesList.Items.Add(move);
                        }
                    }
                }
                else
                {
                    SolutionMovesList.Items.Add("Решение будет показано после завершения позиции");
                }
            }
            catch (Exception ex)
            {
                SolutionMovesList.Items.Add($"Ошибка загрузки решения: {ex.Message}");
            }
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ShowHint();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка показа подсказки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Завершить тренировку досрочно?\n\nНезавершенные позиции не будут засчитаны.",
                    "Подтверждение завершения",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _viewModel.CompleteTraining();

                    // Возвращаемся к выбору тренировок
                    var selectionWindow = new TrainingSelectionWindow(_userId);
                    selectionWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка завершения тренировки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.NextPosition();

                // Перезагружаем решение для новой позиции
                LoadSolutionMoves();

                // Сбрасываем флаг показа решения
                _isSolutionShown = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода к следующей позиции: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.CurrentPosition != null &&
                    _viewModel.CurrentPosition.SolutionMoves != null &&
                    _viewModel.CurrentPosition.SolutionMoves.Any())
                {
                    SolutionMovesList.Items.Clear();

                    // Показываем все ходы решения с нумерацией
                    for (int i = 0; i < _viewModel.CurrentPosition.SolutionMoves.Count; i++)
                    {
                        var move = _viewModel.CurrentPosition.SolutionMoves[i];
                        SolutionMovesList.Items.Add($"{i + 1}. {move}");
                    }

                    _isSolutionShown = true;

                    // Штраф за просмотр решения
                    _viewModel.Score = Math.Max(0, _viewModel.Score - 20);
                    _viewModel.Mistakes++;
                    _viewModel.StatusText = "Решение показано (-20 очков)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка показа решения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cleanup()
        {
            _timer?.Stop();
            _updateTimer?.Stop();
            _viewModel?.CompleteTraining();
        }

        // Свойства для удобства доступа
        public int CurrentPositionIndex => _viewModel?.CurrentPositionIndex ?? 0;
        public List<TrainingPositionDto> CurrentPositions => _viewModel?.CurrentPositions?.ToList() ?? new List<TrainingPositionDto>();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_viewModel.IsTrainingCompleted)
            {
                var result = MessageBox.Show("Тренировка не завершена. Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void ChessBoard_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализация доски
            if (sender is ItemsControl itemsControl)
            {
                itemsControl.ItemsSource = _viewModel?.Board?.CellsFlat;
            }
        }
    }
}