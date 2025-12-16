using shahmati.Helpers;
using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace shahmati.ViewModels
{
    public class TrainingViewModel : INotifyPropertyChanged
    {
        private readonly TrainingService _trainingService;
        private readonly ApiService _apiService;
        private int _userId;
        private DispatcherTimer _trainingTimer;
        private TimeSpan _timeElapsed;
        private int _currentPositionIndex;
        private int _score;
        private int _mistakes;
        private bool _isTrainingActive;
        private List<TrainingTypeDto> _allTrainings = new();
        private List<TrainingTypeDto> _filteredTrainings = new();
        private List<TrainingPositionDto> _currentPositions = new();

        public List<TrainingTypeDto> AllTrainings
        {
            get => _allTrainings;
            private set
            {
                _allTrainings = value;
                OnPropertyChanged(nameof(AllTrainings));
            }
        }

        public List<TrainingTypeDto> FilteredTrainings
        {
            get => _filteredTrainings;
            set
            {
                _filteredTrainings = value;
                OnPropertyChanged(nameof(FilteredTrainings));
            }
        }

        public List<TrainingPositionDto> CurrentPositions
        {
            get => _currentPositions;
            private set
            {
                _currentPositions = value;
                OnPropertyChanged(nameof(CurrentPositions));
            }
        }

        public TrainingPositionDto CurrentPosition => CurrentPositions.Count > 0 && _currentPositionIndex < CurrentPositions.Count
            ? CurrentPositions[_currentPositionIndex] : null;

        public int CurrentPositionIndex => _currentPositionIndex;

        public TrainingTypeDto SelectedTraining { get; set; }

        public TrainingStatsDto TrainingStats { get; set; }

        // Статус тренировки
        public string TrainingStatus => _isTrainingActive ? $"Позиция {_currentPositionIndex + 1} из {CurrentPositions.Count}" : "Тренировка не начата";

        public string TimeElapsed => _timeElapsed.ToString(@"mm\:ss");

        public int Score
        {
            get => _score;
            private set
            {
                _score = value;
                OnPropertyChanged(nameof(Score));
            }
        }

        public int Mistakes
        {
            get => _mistakes;
            private set
            {
                _mistakes = value;
                OnPropertyChanged(nameof(Mistakes));
            }
        }

        public bool IsTrainingActive => _isTrainingActive;

        // Команды
        public ICommand StartTrainingCommand { get; }
        public ICommand NextPositionCommand { get; }
        public ICommand ShowHintCommand { get; }
        public ICommand CompleteTrainingCommand { get; }
        public ICommand FilterByCategoryCommand { get; }

        public TrainingViewModel(int userId)
        {
            _userId = userId;
            _trainingService = new TrainingService();
            _apiService = new ApiService();

            InitializeTimer();

            // Инициализация команд
            StartTrainingCommand = new RelayCommand(async () => await StartTraining());
            NextPositionCommand = new RelayCommand(async () => await NextPosition());
            ShowHintCommand = new RelayCommand(() => ShowHint());
            CompleteTrainingCommand = new RelayCommand(async () => await CompleteTraining());
            FilterByCategoryCommand = new RelayCommand<string>(FilterByCategory);
        }

        private void InitializeTimer()
        {
            _trainingTimer = new DispatcherTimer();
            _trainingTimer.Interval = TimeSpan.FromSeconds(1);
            _trainingTimer.Tick += (s, e) =>
            {
                _timeElapsed = _timeElapsed.Add(TimeSpan.FromSeconds(1));
                OnPropertyChanged(nameof(TimeElapsed));
            };
        }

        public async Task LoadTrainingsAsync()
        {
            AllTrainings = await _trainingService.GetTrainingTypesAsync();
            FilteredTrainings = AllTrainings;
            await LoadTrainingStatsAsync();
            OnPropertyChanged(nameof(AllTrainings));
            OnPropertyChanged(nameof(FilteredTrainings));
        }

        private async Task LoadTrainingStatsAsync()
        {
            TrainingStats = await _trainingService.GetTrainingStatsAsync(_userId);
            OnPropertyChanged(nameof(TrainingStats));
        }

        public async Task StartTraining()
        {
            if (SelectedTraining == null)
            {
                MessageBox.Show("Выберите тип тренировки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Загружаем позиции для тренировки
            CurrentPositions = await _trainingService.GetTrainingPositionsAsync(SelectedTraining.Id);
            if (CurrentPositions.Count == 0)
            {
                MessageBox.Show("Нет доступных позиций для этой тренировки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Начинаем тренировку
            _currentPositionIndex = 0;
            Score = 0;
            Mistakes = 0;
            _timeElapsed = TimeSpan.Zero;
            _isTrainingActive = true;

            // Запускаем таймер
            _trainingTimer.Start();

            // Уведомляем UI об изменениях
            OnPropertyChanged(nameof(IsTrainingActive));
            OnPropertyChanged(nameof(TrainingStatus));
            OnPropertyChanged(nameof(CurrentPosition));

            // Начинаем тренировку на сервере
            await _trainingService.StartTrainingAsync(new StartTrainingRequest
            {
                UserId = _userId,
                TrainingTypeId = SelectedTraining.Id
            });
        }

        public async Task NextPosition()
        {
            if (!_isTrainingActive || CurrentPosition == null) return;

            // Проверяем решение пользователя (здесь должна быть логика проверки хода)
            // Пока просто переходим к следующей позиции

            _currentPositionIndex++;

            if (_currentPositionIndex >= CurrentPositions.Count)
            {
                await CompleteTraining();
                return;
            }

            OnPropertyChanged(nameof(CurrentPosition));
            OnPropertyChanged(nameof(TrainingStatus));
        }

        public void ShowHint()
        {
            if (CurrentPosition == null || string.IsNullOrEmpty(CurrentPosition.Hint)) return;

            MessageBox.Show(CurrentPosition.Hint, "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task CompleteTraining()
        {
            if (!_isTrainingActive) return;

            // Останавливаем таймер
            _trainingTimer.Stop();

            // Сохраняем результаты
            var request = new CompleteTrainingRequest
            {
                UserId = _userId,
                TrainingTypeId = SelectedTraining.Id,
                Score = Score,
                TimeSpent = (int)_timeElapsed.TotalSeconds,
                Mistakes = Mistakes,
                Completed = Score > SelectedTraining.MaxMoves * 0.7 // 70% правильных ответов
            };

            bool success = await _trainingService.CompleteTrainingAsync(request);

            if (success)
            {
                MessageBox.Show($"Тренировка завершена!\n\nОчки: {Score}\nВремя: {TimeElapsed}\nОшибки: {Mistakes}",
                    "Результаты", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Сбрасываем состояние
            _isTrainingActive = false;
            CurrentPositions.Clear();
            _currentPositionIndex = 0;

            // Обновляем статистику
            await LoadTrainingStatsAsync();

            // Уведомляем UI
            OnPropertyChanged(nameof(IsTrainingActive));
            OnPropertyChanged(nameof(TrainingStatus));
            OnPropertyChanged(nameof(CurrentPosition));
        }

        private void FilterByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "Все")
            {
                FilteredTrainings = AllTrainings;
            }
            else
            {
                FilteredTrainings = AllTrainings.Where(t => t.Category == category).ToList();
            }

            OnPropertyChanged(nameof(FilteredTrainings));
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void AddMistake()
        {
            Mistakes++;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}