using shahmati.Models;
using shahmati.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.ViewModels
{
    public class TrainingViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        private ObservableCollection<TrainingTypeDto> _allTrainings;
        private ObservableCollection<TrainingTypeDto> _filteredTrainings;
        private TrainingTypeDto _selectedTraining;
        private List<TrainingPositionDto> _currentPositions;
        private TrainingPositionDto _currentPosition;
        private int _currentPositionIndex = 0;
        private int _score = 0;
        private int _mistakes = 0;
        private TimeSpan _timeElapsed = TimeSpan.Zero;
        private bool _isTrainingActive = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public ObservableCollection<TrainingTypeDto> AllTrainings
        {
            get => _allTrainings;
            set => SetField(ref _allTrainings, value);
        }

        public ObservableCollection<TrainingTypeDto> FilteredTrainings
        {
            get => _filteredTrainings;
            set => SetField(ref _filteredTrainings, value);
        }

        public TrainingTypeDto SelectedTraining
        {
            get => _selectedTraining;
            set => SetField(ref _selectedTraining, value);
        }

        public List<TrainingPositionDto> CurrentPositions
        {
            get => _currentPositions;
            set => SetField(ref _currentPositions, value);
        }

        public TrainingPositionDto CurrentPosition
        {
            get => _currentPosition;
            set => SetField(ref _currentPosition, value);
        }

        public int CurrentPositionIndex
        {
            get => _currentPositionIndex;
            set => SetField(ref _currentPositionIndex, value);
        }

        public int Score
        {
            get => _score;
            set => SetField(ref _score, value);
        }

        public int Mistakes
        {
            get => _mistakes;
            set => SetField(ref _mistakes, value);
        }

        public string TimeElapsed => $"{_timeElapsed:mm\\:ss}";

        public bool IsTrainingActive
        {
            get => _isTrainingActive;
            set => SetField(ref _isTrainingActive, value);
        }

        public TrainingViewModel(int userId)
        {
            _apiService = new ApiService();
            _userId = userId;
            AllTrainings = new ObservableCollection<TrainingTypeDto>();
            FilteredTrainings = new ObservableCollection<TrainingTypeDto>();
            CurrentPositions = new List<TrainingPositionDto>();
        }

        public async Task LoadTrainingsAsync()
        {
            try
            {
                Console.WriteLine("=== Загрузка тренировок ===");

                // Получаем тренировки через ApiService
                var trainings = await _apiService.GetTrainingTypesAsync();

                Console.WriteLine($"Получено тренировок: {trainings?.Count ?? 0}");

                if (trainings == null || trainings.Count == 0)
                {
                    Console.WriteLine("Тренировки не получены, используем демо-данные");
                    LoadMockTrainings();
                    return;
                }

                // Очищаем коллекции
                AllTrainings.Clear();
                FilteredTrainings.Clear();

                // Добавляем полученные тренировки
                foreach (var training in trainings)
                {
                    AllTrainings.Add(training);
                    FilteredTrainings.Add(training);
                }

                Console.WriteLine($"Успешно загружено {trainings.Count} тренировок");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки тренировок: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show($"Ошибка загрузки тренировок: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Используем демо-данные
                LoadMockTrainings();
            }
        }

        private void LoadMockTrainings()
        {
            AllTrainings.Clear();
            FilteredTrainings.Clear();

            var mockTrainings = new List<TrainingTypeDto>
            {
                new TrainingTypeDto
                {
                    Id = 1,
                    Name = "Двойной удар (Демо)",
                    Description = "Найдите ход, который атакует две фигуры одновременно",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 120,
                    MaxMoves = 10
                },
                new TrainingTypeDto
                {
                    Id = 2,
                    Name = "Связка (Демо)",
                    Description = "Используйте связку для выигрыша материала",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 180,
                    MaxMoves = 15
                },
                new TrainingTypeDto
                {
                    Id = 3,
                    Name = "Испанская партия (Демо)",
                    Description = "Освойте основные принципы испанской партии",
                    Difficulty = "Medium",
                    Category = "Opening",
                    MaxTime = 300,
                    MaxMoves = 20
                },
                new TrainingTypeDto
                {
                    Id = 4,
                    Name = "Пешечный эндшпиль (Демо)",
                    Description = "Техника проведения пешки в ферзи",
                    Difficulty = "Beginner",
                    Category = "Endgame",
                    MaxTime = 240,
                    MaxMoves = 30
                },
                new TrainingTypeDto
                {
                    Id = 5,
                    Name = "Блиц: мат в 1 ход (Демо)",
                    Description = "Найдите мат в один ход за ограниченное время",
                    Difficulty = "Beginner",
                    Category = "Speed",
                    MaxTime = 60,
                    MaxMoves = 30
                }
            };

            foreach (var training in mockTrainings)
            {
                AllTrainings.Add(training);
                FilteredTrainings.Add(training);
            }

            Console.WriteLine("Загружены демо-тренировки");
        }

        public async Task StartTraining()
        {
            if (SelectedTraining == null) return;

            try
            {
                Console.WriteLine($"=== Начало тренировки: {SelectedTraining.Name} ===");

                // Обнуляем статистику
                Score = 0;
                Mistakes = 0;
                CurrentPositionIndex = 0;
                _timeElapsed = TimeSpan.Zero;
                OnPropertyChanged(nameof(TimeElapsed));
                IsTrainingActive = true;

                // Начинаем тренировку на сервере
                var request = new StartTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining.Id
                };

                await _apiService.StartTrainingAsync(request);

                // Загружаем позиции для тренировки
                await LoadTrainingPositions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка начала тренировки: {ex.Message}");

                // Загружаем демо-позиции
                LoadMockPositions();
            }
        }

        private async Task LoadTrainingPositions()
        {
            try
            {
                Console.WriteLine($"Загрузка позиций для тренировки ID={SelectedTraining.Id}");

                var positions = await _apiService.GetTrainingPositionsAsync(SelectedTraining.Id);

                if (positions == null || positions.Count == 0)
                {
                    Console.WriteLine("Позиции не получены, используем демо-позиции");
                    LoadMockPositions();
                    return;
                }

                CurrentPositions = positions;
                CurrentPosition = positions.FirstOrDefault();

                Console.WriteLine($"Загружено {positions.Count} позиций");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки позиций: {ex.Message}");
                LoadMockPositions();
            }
        }

        private void LoadMockPositions()
        {
            CurrentPositions = new List<TrainingPositionDto>
            {
                new TrainingPositionDto
                {
                    Id = 1,
                    TrainingTypeId = SelectedTraining?.Id ?? 1,
                    Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    Solution = "e2e4 e7e5 g1f3",
                    Hint = "Конь на f3 атакует пешки на e5 и c7",
                    Difficulty = "Beginner",
                    Theme = "Knight Fork",
                    Rating = 0
                },
                new TrainingPositionDto
                {
                    Id = 2,
                    TrainingTypeId = SelectedTraining?.Id ?? 1,
                    Fen = "r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 0 1",
                    Solution = "d2d4 e5d4 f3d4",
                    Hint = "Примите жертву пешки и развивайте фигуры",
                    Difficulty = "Beginner",
                    Theme = "Pawn Sacrifice",
                    Rating = 1300
                }
            };

            CurrentPosition = CurrentPositions.FirstOrDefault();
            Console.WriteLine("Загружены демо-позиции");
        }

        public async Task NextPosition()
        {
            if (CurrentPositions == null || !CurrentPositions.Any()) return;

            CurrentPositionIndex++;

            if (CurrentPositionIndex >= CurrentPositions.Count)
            {
                // Тренировка завершена
                await CompleteTraining();
                return;
            }

            CurrentPosition = CurrentPositions[CurrentPositionIndex];

            // Добавляем очки за переход к следующей позиции
            Score += 10;
        }

        public void ShowHint()
        {
            if (CurrentPosition != null && !string.IsNullOrEmpty(CurrentPosition.Hint))
            {
                MessageBox.Show($"Подсказка: {CurrentPosition.Hint}",
                    "Подсказка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Штраф за использование подсказки
                Score = Math.Max(0, Score - 5);
            }
        }

        public async Task CompleteTraining()
        {
            try
            {
                IsTrainingActive = false;

                // Сохраняем результат тренировки
                var request = new CompleteTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining?.Id ?? 0,
                    Score = Score,
                    TimeSpent = (int)_timeElapsed.TotalSeconds,
                    Mistakes = Mistakes,
                    Completed = true
                };

                await _apiService.CompleteTrainingAsync(request);

                MessageBox.Show($"Тренировка завершена!\n\n" +
                               $"Результаты:\n" +
                               $"Очки: {Score}\n" +
                               $"Ошибки: {Mistakes}\n" +
                               $"Время: {TimeElapsed}",
                    "Тренировка завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка завершения тренировки: {ex.Message}");
            }
        }

        public void AddTime(TimeSpan time)
        {
            _timeElapsed = _timeElapsed.Add(time);
            OnPropertyChanged(nameof(TimeElapsed));
        }

        public void AddScore(int points)
        {
            Score += points;
        }

        public void AddMistake()
        {
            Mistakes++;
        }

        public bool CheckMove(string move)
        {
            if (CurrentPosition == null || string.IsNullOrEmpty(CurrentPosition.Solution))
                return false;

            var solutionMoves = CurrentPosition.Solution.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentMove = solutionMoves.Length > CurrentPositionIndex ?
                            solutionMoves[CurrentPositionIndex] : null;

            if (string.IsNullOrEmpty(currentMove))
                return false;

            // Упрощенная проверка хода
            bool isCorrect = move.Equals(currentMove, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                AddScore(20); // Награда за правильный ход
            }
            else
            {
                AddMistake();
            }

            return isCorrect;
        }

        public void ResetTraining()
        {
            Score = 0;
            Mistakes = 0;
            CurrentPositionIndex = 0;
            _timeElapsed = TimeSpan.Zero;
            OnPropertyChanged(nameof(TimeElapsed));
            CurrentPositions?.Clear();
            CurrentPosition = null;
            IsTrainingActive = false;
        }
    }
}