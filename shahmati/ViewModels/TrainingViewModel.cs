using shahmati.Helpers;
using shahmati.models;
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
using System.Windows.Input;

namespace shahmati.ViewModels
{
    public class TrainingViewModel : INotifyPropertyChanged
    {
        private readonly int _userId;
        private readonly ApiService _apiService;
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
        private Position? _selectedPosition;
        private List<Position> _possibleMoves;
        private string? _positionTask;
        private bool _isTrainingCompleted;
        private ObservableCollection<TrainingTypeDto> _allTrainings;
        private ObservableCollection<TrainingTypeDto> _filteredTrainings;
        private ObservableCollection<TrainingProgressDto> _trainingProgress;
        private bool _enableMoveHighlighting = true;
        private bool _isComputerThinking = false;

        // Для отслеживания текущего игрока
        private PieceColor _currentPlayerColor = PieceColor.White;
        private List<string> _remainingSolutionMoves = new();
        private Random _random = new Random();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CellClickCommand { get; private set; }

        public TrainingViewModel(int userId)
        {
            _userId = userId;
            _apiService = new ApiService();

            // Инициализируем доску
            _board = new Board();
            _selectedPosition = null;
            _possibleMoves = new List<Position>();

            _currentPositions = new ObservableCollection<TrainingPositionDto>();
            _allTrainings = new ObservableCollection<TrainingTypeDto>();
            _filteredTrainings = new ObservableCollection<TrainingTypeDto>();
            _trainingProgress = new ObservableCollection<TrainingProgressDto>();
            _startTime = DateTime.Now;

            // Инициализируем команду для кликов по клеткам
            CellClickCommand = new RelayCommand<Position>(HandleCellClick);
        }

        // Свойства для UI
        public PieceColor CurrentPlayerColor
        {
            get => _currentPlayerColor;
            set
            {
                if (_currentPlayerColor != value)
                {
                    _currentPlayerColor = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPlayerText));
                    OnPropertyChanged(nameof(CurrentPlayerSymbol));

                    // Если теперь ход черных - запускаем компьютерный ход
                    if (_currentPlayerColor == PieceColor.Black)
                    {
                        _ = MakeComputerMoveAsync();
                    }
                }
            }
        }

        public string CurrentPlayerText => CurrentPlayerColor == PieceColor.White ? "Ход белых" : "Ход черных";
        public string CurrentPlayerSymbol => CurrentPlayerColor == PieceColor.White ? "♔" : "♚";

        public Board Board
        {
            get => _board;
            set
            {
                _board = value;
                OnPropertyChanged();
            }
        }

        public Position? SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                _selectedPosition = value;
                OnPropertyChanged();
            }
        }

        public bool EnableMoveHighlighting
        {
            get => _enableMoveHighlighting;
            set
            {
                if (_enableMoveHighlighting != value)
                {
                    _enableMoveHighlighting = value;
                    OnPropertyChanged(nameof(EnableMoveHighlighting));
                    UpdateMoveHighlighting();
                }
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
                OnPropertyChanged(nameof(PositionProgress));
            }
        }

        public string PositionProgress => CurrentPositions.Count > 0
            ? $"{CurrentPositionIndex + 1}/{CurrentPositions.Count}"
            : "0/0";

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

        public ObservableCollection<TrainingProgressDto> TrainingProgress
        {
            get => _trainingProgress;
            set
            {
                _trainingProgress = value;
                OnPropertyChanged();
            }
        }

        // Основные методы
        public async Task LoadTrainingsAsync()
        {
            try
            {
                StatusText = "Загрузка тренировок...";

                var trainingTypes = await _apiService.GetTrainingTypesAsync();

                if (trainingTypes != null && trainingTypes.Any())
                {
                    AllTrainings.Clear();
                    FilteredTrainings.Clear();

                    foreach (var training in trainingTypes)
                    {
                        AllTrainings.Add(training);
                        FilteredTrainings.Add(training);
                    }
                }
                else
                {
                    CreateSampleTrainings();
                }

                await LoadUserTrainingProgress();
                StatusText = $"Загружено тренировок: {AllTrainings.Count}";
            }
            catch (Exception ex)
            {
                CreateSampleTrainings();
                StatusText = $"Загружено тренировок: {AllTrainings.Count} (ошибка: {ex.Message})";
            }
        }

        private async Task LoadUserTrainingProgress()
        {
            try
            {
                var progress = await _apiService.GetUserTrainingProgressAsync(_userId);
                if (progress != null)
                {
                    TrainingProgress.Clear();
                    foreach (var item in progress)
                    {
                        TrainingProgress.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
            }
        }

        public async Task StartTraining()
        {
            try
            {
                StatusText = "Загрузка тренировки...";

                if (SelectedTraining == null)
                {
                    StatusText = "Тренировка не выбрана";
                    return;
                }

                await LoadTrainingPositions();

                if (CurrentPositions.Count > 0)
                {
                    CurrentPosition = CurrentPositions[0];
                    CurrentPositionIndex = 0;

                    await LoadPositionFromFen(CurrentPosition.Fen);

                    PositionTask = $"Найдите лучший ход. Тема: {CurrentPosition.Theme}";
                    HintText = CurrentPosition.Hint ?? "Используйте тактические приемы.";
                    StatusText = "Тренировка начата. Найдите лучший ход на доске.";
                    Score = 0;
                    Mistakes = 0;
                    _startTime = DateTime.Now;

                    // Загружаем решение из базы
                    LoadSolutionFromDatabase();

                    await _apiService.StartTrainingAsync(new StartTrainingRequest
                    {
                        UserId = _userId,
                        TrainingTypeId = SelectedTraining.Id
                    });

                    // Если ход черных - запускаем компьютерный ход
                    if (CurrentPlayerColor == PieceColor.Black)
                    {
                        StatusText = "Ход черных. Компьютер думает...";
                        _ = MakeComputerMoveAsync();
                    }
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

        private void LoadSolutionFromDatabase()
        {
            _remainingSolutionMoves.Clear();

            if (CurrentPosition != null && CurrentPosition.SolutionMoves != null)
            {
                _remainingSolutionMoves = new List<string>(CurrentPosition.SolutionMoves);
                Console.WriteLine($"Загружено решений: {_remainingSolutionMoves.Count}");

                foreach (var move in _remainingSolutionMoves)
                {
                    Console.WriteLine($"Решение: {move}");
                }
            }
        }

        private void DeterminePlayerTurnFromFen(string fen)
        {
            if (string.IsNullOrEmpty(fen))
            {
                CurrentPlayerColor = PieceColor.White;
                return;
            }

            var parts = fen.Split(' ');
            if (parts.Length > 1)
            {
                string turn = parts[1].ToLower();
                CurrentPlayerColor = turn == "w" ? PieceColor.White : PieceColor.Black;
            }
            else
            {
                CurrentPlayerColor = PieceColor.White;
            }

            Console.WriteLine($"Текущий ход: {CurrentPlayerText}");
        }

        private async Task LoadTrainingPositions()
        {
            try
            {
                if (SelectedTraining == null) return;

                CurrentPositions.Clear();

                var positions = await _apiService.GetTrainingPositionsAsync(SelectedTraining.Id);

                if (positions != null && positions.Any())
                {
                    foreach (var position in positions)
                    {
                        if (!string.IsNullOrEmpty(position.Solution))
                        {
                            position.SolutionMoves = ParseSolutionMoves(position.Solution);
                        }

                        if (string.IsNullOrEmpty(position.Theme))
                        {
                            position.Theme = SelectedTraining.Name;
                        }

                        if (string.IsNullOrEmpty(position.Hint))
                        {
                            position.Hint = "Найдите лучший ход в данной позиции.";
                        }

                        CurrentPositions.Add(position);
                    }
                }
                else
                {
                    await CreateSamplePositions();
                }
            }
            catch (Exception ex)
            {
                await CreateSamplePositions();
                StatusText = $"Ошибка загрузки позиций: {ex.Message}";
            }
        }

        private List<string> ParseSolutionMoves(string solution)
        {
            var moves = new List<string>();
            if (string.IsNullOrEmpty(solution)) return moves;

            var parts = solution.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (!part.EndsWith(".") && !string.IsNullOrWhiteSpace(part))
                {
                    moves.Add(part.Trim());
                }
            }

            return moves;
        }

        private async Task CreateSamplePositions()
        {
            if (SelectedTraining == null) return;

            var positions = new List<TrainingPositionDto>
            {
                new TrainingPositionDto
                {
                    Id = 1,
                    TrainingTypeId = SelectedTraining.Id,
                    Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    Theme = "Начальная позиция",
                    Solution = "e2e4 e7e5 g1f3 b8c6",
                    Hint = "Начните с хода королевской пешкой e2-e4.",
                    Difficulty = SelectedTraining.Difficulty,
                    Rating = 1200
                },
                new TrainingPositionDto
                {
                    Id = 2,
                    TrainingTypeId = SelectedTraining.Id,
                    Fen = "r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3",
                    Theme = "Дебют - белые",
                    Solution = "f1c4",
                    Hint = "Развивайте слона на сильную диагональ f1-c4.",
                    Difficulty = SelectedTraining.Difficulty,
                    Rating = 1200
                },
                new TrainingPositionDto
                {
                    Id = 3,
                    TrainingTypeId = SelectedTraining.Id,
                    Fen = "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R b KQkq - 4 4",
                    Theme = "Ход черных",
                    Solution = "b8c6",
                    Hint = "Черные развивают коня на c6.",
                    Difficulty = SelectedTraining.Difficulty,
                    Rating = 1300
                }
            };

            foreach (var position in positions)
            {
                if (!string.IsNullOrEmpty(position.Solution))
                {
                    position.SolutionMoves = ParseSolutionMoves(position.Solution);
                }
                CurrentPositions.Add(position);
            }

            await Task.CompletedTask;
        }

        private void CreateSampleTrainings()
        {
            var trainings = new List<TrainingTypeDto>
            {
                new TrainingTypeDto
                {
                    Id = 1,
                    Name = "Тактика: Двойной удар",
                    Description = "Найдите ход, который атакует две фигуры одновременно",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 120,
                    MaxMoves = 10
                },
                new TrainingTypeDto
                {
                    Id = 2,
                    Name = "Тактика: Связка",
                    Description = "Используйте связку для выигрыша материала",
                    Difficulty = "Beginner",
                    Category = "Tactics",
                    MaxTime = 180,
                    MaxMoves = 15
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

        public async Task LoadPositionFromFen(string fen)
        {
            try
            {
                if (string.IsNullOrEmpty(fen))
                {
                    Board = new Board();
                    CurrentPlayerColor = PieceColor.White;
                }
                else
                {
                    // Пока используем стандартную доску
                    // TODO: Добавить парсер FEN
                    Board = new Board();
                    DeterminePlayerTurnFromFen(fen);
                }

                ResetSelection();
                StatusText = $"Позиция загружена. {CurrentPlayerText}. Ищите лучший ход!";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки позиции: {ex.Message}";
                Board = new Board();
            }
        }

        // Метод обработки клика по клетке (только для белых фигур)
        private void HandleCellClick(Position position)
        {
            try
            {
                if (_isComputerThinking) return;

                if (!position.IsValid()) return;

                Console.WriteLine($"=== CLICK ON CELL ===");
                Console.WriteLine($"Position: Row={position.Row}, Column={position.Column}");
                Console.WriteLine($"Current player: {CurrentPlayerText}");

                // Только белые фигуры могут быть выбраны пользователем
                if (CurrentPlayerColor != PieceColor.White)
                {
                    StatusText = "Сейчас ход черных. Подождите...";
                    return;
                }

                var clickedPiece = Board.GetPieceAt(position);
                Console.WriteLine($"Clicked piece: {(clickedPiece != null ? $"{clickedPiece.Type} ({clickedPiece.Color})" : "Empty")}");

                // Если ничего не выбрано и кликнули на белую фигуру
                if (SelectedPosition == null && clickedPiece != null && clickedPiece.Color == PieceColor.White)
                {
                    Console.WriteLine($"Selecting white piece: {clickedPiece.Type}");
                    SelectPiece(position);
                }
                // Если выбрана фигура и кликнули на клетку для хода
                else if (SelectedPosition != null)
                {
                    Console.WriteLine($"Trying to move from {SelectedPosition} to {position}");
                    TryMakeMove(SelectedPosition.Value, position);
                }
                // Если кликнули на пустую клетку или черную фигуру
                else
                {
                    if (clickedPiece != null && clickedPiece.Color == PieceColor.Black)
                    {
                        StatusText = "Вы не можете выбрать черную фигуру. Выберите белую фигуру.";
                    }
                    else
                    {
                        StatusText = "Выберите белую фигуру для хода.";
                    }
                    ResetSelection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleCellClick: {ex.Message}");
                StatusText = $"Ошибка: {ex.Message}";
            }
        }

        private void SelectPiece(Position position)
        {
            ResetSelection();

            SelectedPosition = position;
            var piece = Board.GetPieceAt(position);

            if (piece != null)
            {
                _possibleMoves = piece.GetPossibleMoves(position, Board).ToList();
                Console.WriteLine($"Possible moves: {_possibleMoves.Count}");

                if (EnableMoveHighlighting)
                {
                    foreach (var move in _possibleMoves)
                    {
                        var cell = GetCellAt(move);
                        if (cell != null)
                        {
                            cell.IsPossibleMove = true;
                        }
                    }
                }

                var selectedCell = GetCellAt(position);
                if (selectedCell != null)
                {
                    selectedCell.IsSelected = true;
                }

                StatusText = $"Выбрана фигура: {GetPieceName(piece)}. Выберите клетку для хода.";
            }

            OnPropertyChanged(nameof(Board));
        }

        private string GetPieceName(ChessPiece piece)
        {
            string color = piece.Color == PieceColor.White ? "Белый " : "Черный ";

            return piece.Type switch
            {
                PieceType.King => color + "Король",
                PieceType.Queen => color + "Ферзь",
                PieceType.Rook => color + "Ладья",
                PieceType.Bishop => color + "Слон",
                PieceType.Knight => color + "Конь",
                PieceType.Pawn => color + "Пешка",
                _ => "Фигура"
            };
        }

        private void ResetSelection()
        {
            foreach (var cell in Board.CellsFlat)
            {
                cell.IsSelected = false;
                cell.IsPossibleMove = false;
            }

            SelectedPosition = null;
            _possibleMoves.Clear();
        }

        private void UpdateMoveHighlighting()
        {
            if (!_enableMoveHighlighting)
            {
                foreach (var cell in Board.CellsFlat)
                {
                    cell.IsPossibleMove = false;
                }
            }
            else if (SelectedPosition != null)
            {
                var piece = Board.GetPieceAt(SelectedPosition.Value);
                if (piece != null && piece.Color == CurrentPlayerColor)
                {
                    var possibleMoves = piece.GetPossibleMoves(SelectedPosition.Value, Board);
                    foreach (var move in possibleMoves)
                    {
                        var cell = GetCellAt(move);
                        if (cell != null)
                        {
                            cell.IsPossibleMove = true;
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(Board));
        }

        private void TryMakeMove(Position from, Position to)
        {
            Console.WriteLine($"Trying move: {ConvertToChessNotation(from)} -> {ConvertToChessNotation(to)}");

            // Проверяем, является ли ход возможным
            if (!_possibleMoves.Contains(to))
            {
                StatusText = "Невозможный ход. Выберите другую клетку.";
                ResetSelection();
                return;
            }

            // Проверяем, является ли ход правильным
            if (IsMoveCorrect(from, to))
            {
                Console.WriteLine("Move is correct!");

                // Делаем ход
                Board.MovePiece(from, to);
                Score += 10;

                // Удаляем ход из списка решений
                RemoveMoveFromSolution(from, to);

                // Меняем игрока после успешного хода
                SwitchPlayer();

                StatusText = $"Отличный ход! +10 очков. Всего: {Score}. {CurrentPlayerText}.";

                ResetSelection();

                // Если теперь ход черных - запускаем компьютерный ход
                if (CurrentPlayerColor == PieceColor.Black)
                {
                    _ = MakeComputerMoveAsync();
                }
            }
            else
            {
                Console.WriteLine("Move is incorrect!");
                Mistakes++;
                HintText = CurrentPosition?.Hint ?? "Неверный ход! Попробуйте еще раз.";
                StatusText = $"Неверно! Ошибок: {Mistakes}";
                ResetSelection();
            }
        }

        private void RemoveMoveFromSolution(Position from, Position to)
        {
            var moveNotation = $"{ConvertToChessNotation(from)}{ConvertToChessNotation(to)}";

            for (int i = 0; i < _remainingSolutionMoves.Count; i++)
            {
                if (_remainingSolutionMoves[i].Contains(moveNotation, StringComparison.OrdinalIgnoreCase))
                {
                    _remainingSolutionMoves.RemoveAt(i);
                    Console.WriteLine($"Удален ход из решения: {moveNotation}");
                    Console.WriteLine($"Осталось ходов: {_remainingSolutionMoves.Count}");
                    break;
                }
            }
        }

        private void SwitchPlayer()
        {
            CurrentPlayerColor = CurrentPlayerColor == PieceColor.White ?
                PieceColor.Black : PieceColor.White;
        }

        private bool IsMoveCorrect(Position from, Position to)
        {
            if (CurrentPosition == null || _remainingSolutionMoves == null || !_remainingSolutionMoves.Any())
                return true;

            var moveNotation = $"{ConvertToChessNotation(from)}{ConvertToChessNotation(to)}";
            Console.WriteLine($"Checking move: {moveNotation}");

            foreach (var move in _remainingSolutionMoves)
            {
                Console.WriteLine($"Solution move: {move}");
                if (move.Contains(moveNotation, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Метод для автоматического хода черных
        private async Task MakeComputerMoveAsync()
        {
            if (_isComputerThinking) return;

            _isComputerThinking = true;

            try
            {
                StatusText = "Компьютер думает...";
                await Task.Delay(1000); // Задержка для реалистичности

                // Ищем следующий ход из решения
                string? nextMove = GetNextComputerMove();

                if (nextMove != null)
                {
                    // Парсим ход (например, "b8c6")
                    if (TryParseChessNotation(nextMove, out Position from, out Position to))
                    {
                        // Проверяем, что ход возможен
                        var piece = Board.GetPieceAt(from);
                        if (piece != null && piece.Color == PieceColor.Black)
                        {
                            // Делаем ход
                            Board.MovePiece(from, to);
                            RemoveMoveFromSolution(from, to);

                            StatusText = $"Компьютер сделал ход: {nextMove}";

                            // Переключаем на белых
                            SwitchPlayer();
                        }
                    }
                }
                else
                {
                    // Если нет решения, делаем случайный ход
                    MakeRandomBlackMove();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка компьютерного хода: {ex.Message}");
                StatusText = "Ошибка компьютерного хода";
                SwitchPlayer(); // Все равно переключаем на белых
            }
            finally
            {
                _isComputerThinking = false;
            }
        }

        private string? GetNextComputerMove()
        {
            if (_remainingSolutionMoves != null && _remainingSolutionMoves.Any())
            {
                // Ищем ход для черных (первый ход в решении, который начинается с a-h 1-8)
                foreach (var move in _remainingSolutionMoves)
                {
                    // Простая проверка: если ход содержит координаты a-h и 1-8
                    if (move.Length >= 4 &&
                        move[0] >= 'a' && move[0] <= 'h' &&
                        move[1] >= '1' && move[1] <= '8')
                    {
                        return move;
                    }
                }
            }

            return null;
        }

        private bool TryParseChessNotation(string notation, out Position from, out Position to)
        {
            from = Position.Invalid;
            to = Position.Invalid;

            if (notation.Length < 4) return false;

            try
            {
                // Парсим from (например, "b8" -> row=0, col=1)
                char fromFile = notation[0];
                char fromRank = notation[1];
                int fromCol = fromFile - 'a';
                int fromRow = 8 - (fromRank - '0');

                // Парсим to (например, "c6" -> row=2, col=2)
                char toFile = notation[2];
                char toRank = notation[3];
                int toCol = toFile - 'a';
                int toRow = 8 - (toRank - '0');

                from = new Position(fromRow, fromCol);
                to = new Position(toRow, toCol);

                return from.IsValid() && to.IsValid();
            }
            catch
            {
                return false;
            }
        }

        private void MakeRandomBlackMove()
        {
            // Собираем все черные фигуры
            var blackPieces = new List<(Position pos, ChessPiece piece)>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var pos = new Position(row, col);
                    var piece = Board.GetPieceAt(pos);
                    if (piece != null && piece.Color == PieceColor.Black)
                    {
                        blackPieces.Add((pos, piece));
                    }
                }
            }

            if (blackPieces.Count == 0) return;

            // Пытаемся найти возможный ход
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var randomPiece = blackPieces[_random.Next(blackPieces.Count)];
                var moves = randomPiece.piece.GetPossibleMoves(randomPiece.pos, Board);

                if (moves.Count > 0)
                {
                    var randomMove = moves[_random.Next(moves.Count)];
                    Board.MovePiece(randomPiece.pos, randomMove);

                    StatusText = $"Компьютер сделал случайный ход";
                    SwitchPlayer();
                    return;
                }
            }

            StatusText = "Компьютер не нашел возможных ходов";
            SwitchPlayer();
        }

        private string ConvertToChessNotation(Position position)
        {
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }

        private BoardCell GetCellAt(Position position)
        {
            if (!position.IsValid()) return null;
            return Board.Cells[position.Row, position.Column];
        }

        public async Task NextPosition()
        {
            try
            {
                ResetSelection();
                _isComputerThinking = false;

                if (CurrentPositionIndex < CurrentPositions.Count - 1)
                {
                    CurrentPositionIndex++;
                    CurrentPosition = CurrentPositions[CurrentPositionIndex];
                    await LoadPositionFromFen(CurrentPosition?.Fen ?? string.Empty);

                    // Загружаем новое решение
                    LoadSolutionFromDatabase();

                    PositionTask = $"Найдите лучший ход. Тема: {CurrentPosition?.Theme ?? "Общая"}";
                    HintText = CurrentPosition?.Hint ?? "Используйте тактические приемы.";
                    StatusText = $"Позиция {CurrentPositionIndex + 1} из {CurrentPositions.Count}. Очки: {Score}. {CurrentPlayerText}.";

                    // Если ход черных - запускаем компьютерный ход
                    if (CurrentPlayerColor == PieceColor.Black)
                    {
                        _ = MakeComputerMoveAsync();
                    }
                }
                else
                {
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
                HintText = CurrentPosition.Hint ?? "Используйте тактические приемы.";
                StatusText = "Подсказка показана";
                Score = Math.Max(0, Score - 5);
                OnPropertyChanged(nameof(Score));
            }
        }

        public void ShowFullSolution()
        {
            if (CurrentPosition != null && _remainingSolutionMoves != null && _remainingSolutionMoves.Any())
            {
                string solution = "🧩 ПОЛНОЕ РЕШЕНИЕ:\n\n";

                // Показываем все оставшиеся ходы решения
                for (int i = 0; i < _remainingSolutionMoves.Count; i++)
                {
                    var move = _remainingSolutionMoves[i];
                    solution += $"{i + 1}. {FormatMove(move)}\n";
                }

                HintText = solution;
                StatusText = "Показано полное решение";
                Score = Math.Max(0, Score - 15); // Больший штраф за полное решение
                OnPropertyChanged(nameof(Score));

                // Также очищаем список, чтобы игра продолжалась
                _remainingSolutionMoves.Clear();
            }
            else if (CurrentPosition != null)
            {
                HintText = CurrentPosition.Hint ?? "Решение не найдено.";
                StatusText = "Решение не найдено";
            }
        }

        private string FormatMove(string move)
        {
            if (move.Length >= 4)
            {
                var from = move.Substring(0, 2);
                var to = move.Substring(2, 2);
                return $"{from.ToUpper()} → {to.ToUpper()}";
            }
            return move;
        }

        public async Task CompleteTraining()
        {
            try
            {
                StatusText = "Завершение тренировки...";

                var completeRequest = new CompleteTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining?.Id ?? 0,
                    Score = Score,
                    TimeSpent = (int)(DateTime.Now - _startTime).TotalSeconds,
                    Mistakes = Mistakes,
                    Completed = true
                };

                var success = await _apiService.CompleteTrainingAsync(completeRequest);

                if (success)
                {
                    StatusText = $"Тренировка завершена! Счет: {Score}, Время: {TimeElapsed}";
                    IsTrainingCompleted = true;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"🎉 Тренировка завершена! 🎉\n\n" +
                            $"📊 Результаты:\n" +
                            $"• Очки: {Score}\n" +
                            $"• Ошибок: {Mistakes}\n" +
                            $"• Время: {TimeElapsed}\n" +
                            $"• Пройдено позиций: {CurrentPositionIndex + 1}/{CurrentPositions.Count}\n\n" +
                            $"🏆 Молодец! Продолжайте тренироваться!",
                            "Тренировка завершена",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                else
                {
                    StatusText = "Тренировка завершена (результаты не сохранены)";
                    IsTrainingCompleted = true;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Тренировка завершена, но результаты не сохранены на сервере.",
                            "Тренировка завершена",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Тренировка завершена (ошибка сохранения: {ex.Message})";
                IsTrainingCompleted = true;
            }
        }

        public void UpdateTimer()
        {
            try
            {
                var elapsed = DateTime.Now - _startTime;
                TimeElapsed = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления таймера: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}