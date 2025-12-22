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

        public event PropertyChangedEventHandler PropertyChanged;

        public TrainingWindow(int userId, TrainingTypeDto training = null)
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
            LoadTraining();
        }

        private async void LoadTraining()
        {
            try
            {
                // Загружаем все тренировки
                await _viewModel.LoadTrainingsAsync();

                // Если передана конкретная тренировка, начинаем ее
                if (_viewModel.SelectedTraining != null)
                {
                    await _viewModel.StartTraining();
                }
                else
                {
                    // Если тренировка не выбрана, показываем сообщение
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
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                _viewModel.UpdateTimer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка таймера: {ex.Message}");
            }
        }

        // Обработчики событий кнопок
        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.NextPosition();
        }

        private void ShowSolutionButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowHint();
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowHint();
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

                e.Cancel = result != MessageBoxResult.Yes;
            }

            _timer?.Stop();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}