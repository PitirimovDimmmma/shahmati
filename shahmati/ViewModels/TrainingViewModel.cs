using shahmati.models;
using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace shahmati.ViewModels
{
    public class TrainingViewModel : INotifyPropertyChanged
    {
        private readonly int _userId;
        private readonly HttpClient _httpClient;
        private TrainingTypeDto? _selectedTraining;
        private ObservableCollection<TrainingPositionDto> _currentPositions;
        private TrainingPositionDto? _currentPosition;
        private int _currentPositionIndex;
        private string? _timeElapsed;
        private int _score;
        private int _mistakes;
        private string? _hintText;
        private string? _statusText;
        private Board _board;
        private DateTime _startTime;
        private DateTime _currentTime;
        private Position? _selectedCell;
        private List<Position> _possibleMoves;
        private string? _positionTask;
        private bool _isTrainingCompleted;
        private ObservableCollection<TrainingTypeDto> _allTrainings;
        private ObservableCollection<TrainingTypeDto> _filteredTrainings;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CellClickCommand { get; private set; }

        public TrainingViewModel(int userId)
        {
            _userId = userId;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5242/api/") // Ваш API URL
            };
            _board = new Board();
            _currentPositions = new ObservableCollection<TrainingPositionDto>();
            _possibleMoves = new List<Position>();
            _allTrainings = new ObservableCollection<TrainingTypeDto>();
            _filteredTrainings = new ObservableCollection<TrainingTypeDto>();
            _startTime = DateTime.Now;
            _currentTime = DateTime.Now;

            CellClickCommand = new RelayCommand<Position>(OnCellClicked);
        }

        // Основные свойства
        public Board Board
        {
            get => _board;
            set
            {
                _board = value;
                OnPropertyChanged();
            }
        }

        public TrainingTypeDto? SelectedTraining
        {
            get => _selectedTraining;
            set
            {
                _selectedTraining = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TrainingPositionDto> CurrentPositions
        {
            get => _currentPositions;
            set
            {
                _currentPositions = value;
                OnPropertyChanged();
            }
        }

        public TrainingPositionDto? CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged();
            }
        }

        public int CurrentPositionIndex
        {
            get => _currentPositionIndex;
            set
            {
                _currentPositionIndex = value;
                OnPropertyChanged();
            }
        }

        public string? TimeElapsed
        {
            get => _timeElapsed;
            set
            {
                _timeElapsed = value;
                OnPropertyChanged();
            }
        }

        public int Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
            }
        }

        public int Mistakes
        {
            get => _mistakes;
            set
            {
                _mistakes = value;
                OnPropertyChanged();
            }
        }

        public string? HintText
        {
            get => _hintText;
            set
            {
                _hintText = value;
                OnPropertyChanged();
            }
        }

        public string? StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string? PositionTask
        {
            get => _positionTask;
            set
            {
                _positionTask = value;
                OnPropertyChanged();
            }
        }

        public bool IsTrainingCompleted
        {
            get => _isTrainingCompleted;
            set
            {
                _isTrainingCompleted = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TrainingTypeDto> AllTrainings
        {
            get => _allTrainings;
            set
            {
                _allTrainings = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TrainingTypeDto> FilteredTrainings
        {
            get => _filteredTrainings;
            set
            {
                _filteredTrainings = value;
                OnPropertyChanged();
            }
        }

        // Методы для тренировки
        public async Task StartTraining()
        {
            try
            {
                StatusText = "Загрузка тренировки...";

                // Загружаем позиции для выбранной тренировки из API
                await LoadTrainingPositions();

                if (CurrentPositions.Count > 0)
                {
                    CurrentPosition = CurrentPositions[0];
                    CurrentPositionIndex = 0;
                    await LoadPositionFromFen(CurrentPosition.Fen);

                    PositionTask = $"Найдите лучший ход. Тема: {CurrentPosition.Theme}";
                    HintText = CurrentPosition.Hint;
                    StatusText = "Тренировка начата. Найдите лучший ход на доске.";
                }
                else
                {
                    StatusText = "Нет доступных позиций для тренировки";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
        }

        private async Task LoadTrainingPositions()
        {
            try
            {
                if (SelectedTraining == null) return;

                // Загружаем позиции из API
                var response = await _httpClient.GetAsync($"training/{SelectedTraining.Id}/positions");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var positions = JsonSerializer.Deserialize<List<TrainingPositionDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    CurrentPositions.Clear();
                    if (positions != null)
                    {
                        foreach (var position in positions)
                        {
                            // Парсим ходы решения
                            if (!string.IsNullOrEmpty(position.Solution))
                            {
                                position.SolutionMoves = position.Solution.Split(' ')
                                    .Where(m => !string.IsNullOrWhiteSpace(m))
                                    .ToList();
                            }
                            CurrentPositions.Add(position);
                        }
                    }

                    // Если нет позиций, создаем тестовые
                    if (CurrentPositions.Count == 0)
                    {
                        await CreateSamplePositions();
                    }
                }
                else
                {
                    // Создаем тестовые позиции если API недоступен
                    await CreateSamplePositions();
                }
            }
            catch
            {
                await CreateSamplePositions();
            }
        }

        private async Task CreateSamplePositions()
        {
            // Примерные позиции для тренировки
            var positions = new List<TrainingPositionDto>
            {
                new TrainingPositionDto
                {
                    Id = 1,
                    TrainingTypeId = SelectedTraining?.Id ?? 1,
                    Fen = "rnbqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3",
                    Theme = "Дебют - Открытая игра",
                    Solution = "Bc4 Nf6 d3 Bc5 O-O O-O",
                    Hint = "Развивайте фигуры, контролируйте центр. Слон на c4 атакует слабую пешку f7.",
                    Difficulty = "Beginner",
                    Rating = 1200
                },
                new TrainingPositionDto
                {
                    Id = 2,
                    TrainingTypeId = SelectedTraining?.Id ?? 1,
                    Fen = "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4",
                    Theme = "Дебют - Испанская партия",
                    Solution = "O-O Bc5 c3 O-O d3 d6",
                    Hint = "Защитите короля рокировкой. Белые готовы к атаке в центре.",
                    Difficulty = "Medium",
                    Rating = 1400
                }
            };

            foreach (var position in positions)
            {
                position.SolutionMoves = position.Solution.Split(' ')
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();
                CurrentPositions.Add(position);
            }

            await Task.CompletedTask;
        }

        public async Task LoadPositionFromFen(string fen)
        {
            try
            {
                if (string.IsNullOrEmpty(fen)) return;

                // Создаем новую доску с начальной позицией
                Board = new Board();

                // Здесь должна быть логика парсинга FEN и установки позиции
                // Временная реализация - просто очищаем и ставим стандартную позицию

                // Обновляем UI
                OnPropertyChanged(nameof(Board));

                // Сбрасываем выделение
                ClearSelection();
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки позиции: {ex.Message}";
            }
        }

        public async Task LoadTrainingsAsync()
        {
            try
            {
                StatusText = "Загрузка тренировок...";

                // Загружаем тренировки из API
                var response = await _httpClient.GetAsync("training/types");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var trainings = JsonSerializer.Deserialize<List<TrainingTypeDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    AllTrainings.Clear();
                    FilteredTrainings.Clear();

                    if (trainings != null && trainings.Any())
                    {
                        foreach (var training in trainings)
                        {
                            AllTrainings.Add(training);
                            FilteredTrainings.Add(training);
                        }
                    }
                    else
                    {
                        // Создаем тестовые тренировки если API пустой
                        CreateSampleTrainings();
                    }

                    StatusText = $"Загружено тренировок: {AllTrainings.Count}";
                }
                else
                {
                    CreateSampleTrainings();
                    StatusText = $"Загружено тренировок: {AllTrainings.Count} (тестовые)";
                }
            }
            catch (Exception ex)
            {
                CreateSampleTrainings();
                StatusText = $"Загружено тренировок: {AllTrainings.Count} (ошибка: {ex.Message})";
            }
        }

        private void CreateSampleTrainings()
        {
            var trainings = new List<TrainingTypeDto>
            {
                new TrainingTypeDto
                {
                    Id = 1,
                    Name = "Двойной удар (вилка)",
                    Description = "Найдите ход, который атакует две фигуры одновременно",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 120,
                    MaxMoves = 10
                },
                new TrainingTypeDto
                {
                    Id = 2,
                    Name = "Связка",
                    Description = "Используйте связку для выигрыша материала",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 180,
                    MaxMoves = 15
                },
                new TrainingTypeDto
                {
                    Id = 3,
                    Name = "Открытая атака",
                    Description = "Обнаружьте скрытые тактические возможности",
                    Difficulty = "Medium",
                    Category = "Tactics",
                    MaxTime = 240,
                    MaxMoves = 20
                },
                new TrainingTypeDto
                {
                    Id = 4,
                    Name = "Матовые комбинации",
                    Description = "Найдите путь к мату в несколько ходов",
                    Difficulty = "Medium",
                    Category = "Tactics",
                    MaxTime = 300,
                    MaxMoves = 25
                }
            };

            AllTrainings.Clear();
            FilteredTrainings.Clear();

            foreach (var training in trainings)
            {
                AllTrainings.Add(training);
                FilteredTrainings.Add(training);
            }
        }

        // Обработка кликов по клеткам
        private void OnCellClicked(Position position)
        {
            HandleCellClick(position.Row, position.Column);
        }

        private void HandleCellClick(int row, int col)
        {
            try
            {
                var position = new Position(row, col);
                var piece = Board.GetPieceAt(position);

                if (_selectedCell == null)
                {
                    // Выбор фигуры
                    if (piece != null && piece.Color == PieceColor.White) // Только белые фигуры в тренировке
                    {
                        _selectedCell = position;
                        _possibleMoves = piece.GetPossibleMoves(position, Board).ToList();

                        // Подсветка возможных ходов
                        foreach (var move in _possibleMoves)
                        {
                            Board.Cells[move.Row, move.Column].IsPossibleMove = true;
                        }

                        Board.Cells[row, col].IsSelected = true;
                        StatusText = $"Выбрана фигура: {GetPieceName(piece)}. Выберите клетку для хода.";
                    }
                    else if (piece != null && piece.Color == PieceColor.Black)
                    {
                        StatusText = "В этой тренировке играют только белые фигуры.";
                    }
                }
                else
                {
                    // Попытка сделать ход
                    if (_possibleMoves.Contains(position))
                    {
                        // Проверяем, является ли ход правильным
                        if (IsMoveCorrect(_selectedCell.Value, position))
                        {
                            Score += 10;
                            StatusText = "Отличный ход! +10 очков";

                            // Делаем ход
                            Board.MovePiece(_selectedCell.Value, position);

                            // Проверяем завершение позиции
                            CheckPositionCompletion();
                        }
                        else
                        {
                            Mistakes++;
                            HintText = "Неверный ход! Попробуйте еще раз.";
                            StatusText = $"Неверно! Ошибок: {Mistakes}";
                        }
                    }
                    else
                    {
                        StatusText = "Невозможный ход. Выберите другую клетку.";
                    }

                    // Сбрасываем выделение
                    ClearSelection();
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
        }

        private bool IsMoveCorrect(Position from, Position to)
        {
            // Здесь должна быть логика проверки правильности хода
            // Сравниваем с решением из тренировочной позиции

            if (CurrentPosition == null || CurrentPosition.SolutionMoves == null)
                return false;

            // Конвертируем позиции в шахматную нотацию (например, "e2e4")
            var moveNotation = $"{ConvertToChessNotation(from)}{ConvertToChessNotation(to)}";

            // Проверяем, есть ли такой ход в решении
            return CurrentPosition.SolutionMoves.Any(m =>
                m.Contains(moveNotation, StringComparison.OrdinalIgnoreCase));
        }

        private string ConvertToChessNotation(Position position)
        {
            // Конвертация row/col в шахматную нотацию (например, "e4")
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }

        private string GetPieceName(ChessPiece? piece)
        {
            if (piece == null) return "Пусто";

            return piece switch
            {
                King => "Король",
                Queen => "Ферзь",
                Rook => "Ладья",
                Bishop => "Слон",
                Knight => "Конь",
                Pawn => "Пешка",
                _ => "Фигура"
            };
        }

        private void CheckPositionCompletion()
        {
            // Проверяем, достигнут ли конец решения
            // В реальной реализации здесь должна быть сложная логика проверки
            StatusText = "Позиция решена! Переходите к следующей.";
        }

        private void ClearSelection()
        {
            if (_selectedCell.HasValue)
            {
                Board.Cells[_selectedCell.Value.Row, _selectedCell.Value.Column].IsSelected = false;
                _selectedCell = null;
            }

            foreach (var move in _possibleMoves)
            {
                Board.Cells[move.Row, move.Column].IsPossibleMove = false;
            }
            _possibleMoves.Clear();
        }

        public async Task NextPosition()
        {
            try
            {
                if (CurrentPositionIndex < CurrentPositions.Count - 1)
                {
                    CurrentPositionIndex++;
                    CurrentPosition = CurrentPositions[CurrentPositionIndex];
                    await LoadPositionFromFen(CurrentPosition?.Fen ?? string.Empty);

                    PositionTask = $"Найдите лучший ход. Тема: {CurrentPosition?.Theme ?? "Общая"}";
                    HintText = CurrentPosition?.Hint;
                    StatusText = $"Позиция {CurrentPositionIndex + 1} из {CurrentPositions.Count}";

                    ClearSelection();
                }
                else
                {
                    // Последняя позиция завершена
                    await CompleteTraining();
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
        }

        public void ShowHint()
        {
            if (CurrentPosition != null)
            {
                HintText = CurrentPosition.Hint ?? "Используйте тактические приемы для получения преимущества.";
                StatusText = "Подсказка показана";

                // Немного штрафуем за подсказку
                Score = Math.Max(0, Score - 5);
            }
        }

        public async Task CompleteTraining()
        {
            try
            {
                StatusText = "Завершение тренировки...";

                // Сохраняем результаты тренировки в базу данных через API
                var completeRequest = new CompleteTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining?.Id ?? 0,
                    Score = Score,
                    TimeSpent = (int)(DateTime.Now - _startTime).TotalSeconds,
                    Mistakes = Mistakes,
                    Completed = true
                };

                var json = JsonSerializer.Serialize(completeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("training/complete", content);

                if (response.IsSuccessStatusCode)
                {
                    StatusText = $"Тренировка завершена! Счет: {Score}, Время: {TimeElapsed}";
                    IsTrainingCompleted = true;

                    MessageBox.Show(
                        $"Тренировка завершена!\n\n" +
                        $"Результаты:\n" +
                        $"• Очки: {Score}\n" +
                        $"• Ошибок: {Mistakes}\n" +
                        $"• Время: {TimeElapsed}\n" +
                        $"• Пройдено позиций: {CurrentPositionIndex + 1}/{CurrentPositions.Count}",
                        "Тренировка завершена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    StatusText = "Тренировка завершена (результаты не сохранены)";
                    IsTrainingCompleted = true;
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Тренировка завершена (ошибка сохранения: {ex.Message})";
                IsTrainingCompleted = true;
            }
        }

        // Обновление таймера
        public void UpdateTimer()
        {
            _currentTime = DateTime.Now;
            var elapsed = _currentTime - _startTime;
            TimeElapsed = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // RelayCommand для обработки команд
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute((T)parameter!);
        }

        public void Execute(object? parameter)
        {
            _execute((T)parameter!);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    // DTO для запросов API
    public class CompleteTrainingRequest
    {
        public int UserId { get; set; }
        public int TrainingTypeId { get; set; }
        public int Score { get; set; }
        public int TimeSpent { get; set; }
        public int Mistakes { get; set; }
        public bool Completed { get; set; }
    }

    public class StartTrainingRequest
    {
        public int UserId { get; set; }
        public int TrainingTypeId { get; set; }
    }
}