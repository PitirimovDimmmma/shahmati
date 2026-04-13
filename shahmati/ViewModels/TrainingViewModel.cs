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
        private string? _hintText;
        private string? _statusText;
        private Board _board;
        private DateTime _startTime;
        private string? _positionTask;
        private bool _isTrainingCompleted;
        private ObservableCollection<TrainingTypeDto> _allTrainings;
        private ObservableCollection<TrainingTypeDto> _filteredTrainings;
        private ObservableCollection<TrainingProgressDto> _trainingProgress;
        private bool _enableMoveHighlighting = false;
        private List<string> _remainingMoves = new();
        private int _totalPositions;
        private int _completedPositions;
        private bool _isLoadingPosition = false;
        private string? _explanationText;
        private int _totalScore = 0;

        private PieceColor _currentPlayerColor = PieceColor.White;

        public event PropertyChangedEventHandler? PropertyChanged;
        public ICommand CellClickCommand { get; private set; }
        public ICommand ShowFullSolutionCommand { get; private set; }
        public ICommand ShowHintCommand { get; private set; }
        public ICommand NextMoveCommand { get; private set; }

        public TrainingViewModel(int userId)
        {
            _userId = userId;
            _apiService = new ApiService();

            _board = new Board();
            _currentPositions = new ObservableCollection<TrainingPositionDto>();
            _allTrainings = new ObservableCollection<TrainingTypeDto>();
            _filteredTrainings = new ObservableCollection<TrainingTypeDto>();
            _trainingProgress = new ObservableCollection<TrainingProgressDto>();

            CellClickCommand = new RelayCommand<Position>(HandleCellClick);
            ShowFullSolutionCommand = new RelayCommand(ShowFullSolution);
            ShowHintCommand = new RelayCommand(ShowHint);
            NextMoveCommand = new RelayCommand(async () => await MakeNextMoveAsync());
        }

        // ========== СВОЙСТВА ==========
        public Board Board
        {
            get => _board;
            set { _board = value; OnPropertyChanged(); OnPropertyChanged(nameof(Board.CellsFlat)); }
        }

        public bool EnableMoveHighlighting
        {
            get => _enableMoveHighlighting;
            set { _enableMoveHighlighting = value; OnPropertyChanged(); }
        }

        public TrainingTypeDto? SelectedTraining
        {
            get => _selectedTraining;
            set { _selectedTraining = value; OnPropertyChanged(); }
        }

        public string? HintText
        {
            get => _hintText;
            set { _hintText = value; OnPropertyChanged(); }
        }

        public string? StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string? PositionTask
        {
            get => _positionTask;
            set { _positionTask = value; OnPropertyChanged(); }
        }

        public string? ExplanationText
        {
            get => _explanationText;
            set { _explanationText = value; OnPropertyChanged(); }
        }

        public string PositionProgress => CurrentPositions.Count > 0
            ? $"{CurrentPositionIndex + 1}/{CurrentPositions.Count}"
            : "0/0";

        public string? TimeElapsed
        {
            get => _timeElapsed;
            set { _timeElapsed = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TrainingPositionDto> CurrentPositions { get; set; } = new();

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

        public bool IsTrainingCompleted
        {
            get => _isTrainingCompleted;
            set
            {
                _isTrainingCompleted = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TrainingTypeDto> AllTrainings { get; set; } = new();
        public ObservableCollection<TrainingTypeDto> FilteredTrainings { get; set; } = new();

        public string CurrentPlayerText => CurrentPlayerColor == PieceColor.White ? "Ход белых" : "Ход черных";
        public string CurrentPlayerSymbol => CurrentPlayerColor == PieceColor.White ? "♔" : "♚";

        private PieceColor CurrentPlayerColor
        {
            get => _currentPlayerColor;
            set
            {
                _currentPlayerColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPlayerText));
                OnPropertyChanged(nameof(CurrentPlayerSymbol));
            }
        }

        public TrainingPositionDto? CurrentPosition
        {
            get => _currentPosition;
            set { _currentPosition = value; OnPropertyChanged(); }
        }

        // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========
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
                StatusText = $"Загружено тренировок: {AllTrainings.Count}";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка: {ex.Message}";
            }
        }

        public async Task StartTraining()
        {
            try
            {
                if (SelectedTraining == null)
                {
                    StatusText = "Тренировка не выбрана";
                    return;
                }

                StatusText = "Загрузка тренировки...";
                await LoadTrainingPositions();

                if (CurrentPositions.Count == 0)
                {
                    StatusText = "Нет доступных позиций";
                    return;
                }

                _totalPositions = CurrentPositions.Count;
                _completedPositions = 0;
                _totalScore = 0;
                CurrentPositionIndex = 0;
                await LoadPosition(CurrentPositions[0]);

                _startTime = DateTime.Now;

                await _apiService.StartTrainingAsync(new StartTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining.Id
                });
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

                CurrentPositions.Clear();
                var positions = await _apiService.GetTrainingPositionsAsync(SelectedTraining.Id);

                if (positions != null && positions.Any())
                {
                    foreach (var position in positions)
                    {
                        position.SolutionMoves = ParseSolutionMoves(position.Solution);
                        CurrentPositions.Add(position);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки: {ex.Message}";
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
                    var cleanMove = part.Replace("+", "").Replace("#", "");
                    moves.Add(cleanMove);
                }
            }
            return moves;
        }

        private async Task LoadPosition(TrainingPositionDto position)
        {
            _isLoadingPosition = true;

            try
            {
                await LoadBoardFromFen(position.Fen);
                _remainingMoves = new List<string>(position.SolutionMoves);

                string[] fenParts = position.Fen.Split(' ');
                string turn = fenParts.Length > 1 ? fenParts[1] : "w";

                PositionTask = position.Theme;
                HintText = position.Hint;
                CurrentPosition = position;
                ExplanationText = position.Hint;

                bool isWhiteTurn = turn == "w";
                CurrentPlayerColor = isWhiteTurn ? PieceColor.White : PieceColor.Black;

                StatusText = $"Позиция {CurrentPositionIndex + 1} из {CurrentPositions.Count}. {position.Theme}. Нажмите 'Следующий ход' для продолжения.";
                ResetSelection();
            }
            finally
            {
                _isLoadingPosition = false;
            }
        }

        private async Task LoadBoardFromFen(string fen)
        {
            try
            {
                var newBoard = new Board();

                for (int row = 0; row < 8; row++)
                    for (int col = 0; col < 8; col++)
                        newBoard.Cells[row, col].Piece = null;

                string[] parts = fen.Split(' ');
                string boardPart = parts[0];
                string[] rows = boardPart.Split('/');

                for (int row = 0; row < 8; row++)
                {
                    string rowStr = rows[row];
                    int col = 0;

                    for (int i = 0; i < rowStr.Length; i++)
                    {
                        char c = rowStr[i];

                        if (char.IsDigit(c))
                        {
                            col += int.Parse(c.ToString());
                        }
                        else
                        {
                            PieceColor color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                            PieceType type = char.ToLower(c) switch
                            {
                                'k' => PieceType.King,
                                'q' => PieceType.Queen,
                                'r' => PieceType.Rook,
                                'b' => PieceType.Bishop,
                                'n' => PieceType.Knight,
                                'p' => PieceType.Pawn,
                                _ => PieceType.Pawn
                            };

                            ChessPiece piece = type switch
                            {
                                PieceType.King => new King(color),
                                PieceType.Queen => new Queen(color),
                                PieceType.Rook => new Rook(color),
                                PieceType.Bishop => new Bishop(color),
                                PieceType.Knight => new Knight(color),
                                _ => new Pawn(color)
                            };

                            newBoard.Cells[row, col].Piece = piece;
                            col++;
                        }
                    }
                }

                newBoard.UpdateSquaresFromCells();
                Board = newBoard;
                ForceUpdateBoard();
                ResetSelection();
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки: {ex.Message}";
            }
        }

        // КОНВЕРТЕР ШАХМАТНОЙ НОТАЦИИ В КООРДИНАТЫ
        private string ConvertAlgebraicToCoordinate(string move)
        {
            if (move.Length == 4 && move[0] >= 'a' && move[0] <= 'h' && move[2] >= 'a' && move[2] <= 'h')
            {
                return move;
            }

            if (move == "O-O" || move == "0-0") return "e1g1";
            if (move == "O-O-O" || move == "0-0-0") return "e1c1";

            if (move.StartsWith("Nx"))
            {
                string target = move.Substring(2, 2);
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var piece = Board.GetPieceAt(new Position(row, col));
                        if (piece != null && piece.Type == PieceType.Knight && piece.Color == PieceColor.White)
                        {
                            var moves = piece.GetPossibleMoves(new Position(row, col), Board);
                            if (moves.Any(p => ConvertToChessNotation(p) == target))
                            {
                                return $"{ConvertToChessNotation(new Position(row, col))}{target}";
                            }
                        }
                    }
                }
                return move;
            }

            if (move.StartsWith("N"))
            {
                string target = move.Substring(move.Length - 2);
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var piece = Board.GetPieceAt(new Position(row, col));
                        if (piece != null && piece.Type == PieceType.Knight && piece.Color == PieceColor.White)
                        {
                            var moves = piece.GetPossibleMoves(new Position(row, col), Board);
                            if (moves.Any(p => ConvertToChessNotation(p) == target))
                            {
                                return $"{ConvertToChessNotation(new Position(row, col))}{target}";
                            }
                        }
                    }
                }
                return move;
            }

            if (move.StartsWith("B"))
            {
                string target = move.Substring(move.Length - 2);
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var piece = Board.GetPieceAt(new Position(row, col));
                        if (piece != null && piece.Type == PieceType.Bishop && piece.Color == PieceColor.White)
                        {
                            var moves = piece.GetPossibleMoves(new Position(row, col), Board);
                            if (moves.Any(p => ConvertToChessNotation(p) == target))
                            {
                                return $"{ConvertToChessNotation(new Position(row, col))}{target}";
                            }
                        }
                    }
                }
                return move;
            }

            if (move.Length == 2 && move[0] >= 'a' && move[0] <= 'h')
            {
                char file = move[0];
                char toRank = move[1];
                int targetRow = 8 - (toRank - '0');
                int targetCol = file - 'a';
                int fromRow = 6;
                var piece = Board.GetPieceAt(new Position(fromRow, targetCol));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Color == PieceColor.White)
                {
                    var moves = piece.GetPossibleMoves(new Position(fromRow, targetCol), Board);
                    if (moves.Any(p => p.Row == targetRow && p.Column == targetCol))
                    {
                        return $"{ConvertToChessNotation(new Position(fromRow, targetCol))}{move}";
                    }
                }
                return move;
            }

            if (move.StartsWith("K"))
            {
                string target = move.Substring(move.Length - 2);
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var piece = Board.GetPieceAt(new Position(row, col));
                        if (piece != null && piece.Type == PieceType.King && piece.Color == PieceColor.White)
                        {
                            var moves = piece.GetPossibleMoves(new Position(row, col), Board);
                            if (moves.Any(p => ConvertToChessNotation(p) == target))
                            {
                                return $"{ConvertToChessNotation(new Position(row, col))}{target}";
                            }
                        }
                    }
                }
                return move;
            }

            return move;
        }

        private async Task MakeNextMoveAsync()
        {
            if (IsTrainingCompleted || _isLoadingPosition) return;

            if (_remainingMoves.Count == 0)
            {
                await NextPosition();
                return;
            }

            string nextMoveAlgebraic = _remainingMoves[0];
            string nextMove = ConvertAlgebraicToCoordinate(nextMoveAlgebraic);

            if (TryParseChessNotation(nextMove, out Position from, out Position to))
            {
                var piece = Board.GetPieceAt(from);
                if (piece != null)
                {
                    string pieceName = GetPieceName(piece);
                    ExplanationText = GetMoveExplanation(nextMoveAlgebraic, piece);

                    Board.MovePiece(from, to);
                    ForceUpdateBoard();

                    _remainingMoves.RemoveAt(0);

                    StatusText = $"Сделан ход: {nextMoveAlgebraic} ({pieceName}). {(_remainingMoves.Count > 0 ? $"Осталось ходов: {_remainingMoves.Count}" : "Позиция завершена!")}";

                    if (_remainingMoves.Count == 0)
                    {
                        StatusText = $"🎉 Позиция решена!";
                        await Task.Delay(1500);
                        await NextPosition();
                    }
                }
                else
                {
                    StatusText = $"Ошибка: нет фигуры на {nextMove.Substring(0, 2)} для хода {nextMoveAlgebraic}";
                    _remainingMoves.Clear();
                    await NextPosition();
                }
            }
            else
            {
                StatusText = $"Ошибка: не удалось распознать ход {nextMoveAlgebraic}";
                _remainingMoves.Clear();
                await NextPosition();
            }

            ResetSelection();
        }

        private string GetMoveExplanation(string move, ChessPiece piece)
        {
            string additional = CurrentPosition?.Hint ?? "Это правильный ход в данной позиции.";

            if (move == "O-O" || move == "0-0")
            {
                return $"РОКИРОВКА! Король идёт в безопасное место. {additional}";
            }

            return $"{additional}";
        }

        private void HandleCellClick(Position position)
        {
            StatusText = "Это обучающий режим. Нажимайте 'Следующий ход' для продолжения.";
        }

        private void ResetSelection() { }

        private void ForceUpdateBoard()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        var cell = Board.Cells[row, col];
                        cell.OnPropertyChanged(nameof(BoardCell.Piece));
                        cell.OnPropertyChanged(nameof(BoardCell.PieceImagePath));
                        cell.OnPropertyChanged(nameof(BoardCell.HasPiece));
                    }
                }

                OnPropertyChanged(nameof(Board));
                OnPropertyChanged(nameof(Board.CellsFlat));
            });
        }

        public async Task NextPosition()
        {
            if (CurrentPositionIndex < CurrentPositions.Count - 1)
            {
                CurrentPositionIndex++;
                await LoadPosition(CurrentPositions[CurrentPositionIndex]);
            }
            else
            {
                await CompleteTraining();
            }
        }

        public async Task CompleteTraining()
        {
            if (IsTrainingCompleted) return;

            IsTrainingCompleted = true;
            var elapsed = DateTime.Now - _startTime;

            // НАЧИСЛЯЕМ 10 ОЧКОВ ЗА ВСЮ ТРЕНИРОВКУ
            _totalScore = 10;

            try
            {
                await _apiService.CompleteTrainingAsync(new CompleteTrainingRequest
                {
                    UserId = _userId,
                    TrainingTypeId = SelectedTraining?.Id ?? 0,
                    Score = _totalScore,
                    TimeSpent = (int)elapsed.TotalSeconds,
                    Mistakes = 0,
                    Completed = true
                });
            }
            catch { }

            int finalScore = _totalScore;
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "🎉 ПОЗДРАВЛЯЕМ! 🎉\n\n" +
                    "✅ Тренировка успешно завершена!\n" +
                    $"📊 Вы заработали: {finalScore} очков\n" +
                    $"⏱ Время прохождения: {TimeElapsed}\n\n" +
                    "Отличная работа! Продолжайте в том же духе!",
                    "Тренировка завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        public async Task CompleteTrainingEarly()
        {
            if (IsTrainingCompleted) return;

            IsTrainingCompleted = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "🏁 ТРЕНИРОВКА ДОСРОЧНО ЗАВЕРШЕНА 🏁\n\n" +
                    "Вы прервали тренировку до её завершения.\n" +
                    "Очки за эту тренировку не начислены.\n\n" +
                    "Вы всегда можете пройти эту тренировку заново!",
                    "Тренировка прервана",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        public void ShowFullSolution()
        {
            if (CurrentPosition != null && CurrentPosition.SolutionMoves != null && CurrentPosition.SolutionMoves.Any())
            {
                var solutionMoves = string.Join(" → ", CurrentPosition.SolutionMoves);
                MessageBox.Show($"Правильная последовательность ходов:\n\n{solutionMoves}\n\nПодсказка: {CurrentPosition.Hint}",
                    "Решение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Решение недоступно для этой позиции.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ShowHint()
        {
            if (CurrentPosition != null && !string.IsNullOrEmpty(CurrentPosition.Hint))
            {
                var nextMove = _remainingMoves.Count > 0 ? $"\n\nСледующий ход: {_remainingMoves[0]}" : "";
                MessageBox.Show($"Подсказка:\n\n{CurrentPosition.Hint}{nextMove}", "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Подсказка недоступна для этой позиции.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void UpdateTimer()
        {
            if (!IsTrainingCompleted && _startTime != default)
            {
                var elapsed = DateTime.Now - _startTime;
                TimeElapsed = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
            }
        }

        private string ConvertToChessNotation(Position position) =>
            $"{(char)('a' + position.Column)}{8 - position.Row}";

        private bool TryParseChessNotation(string notation, out Position from, out Position to)
        {
            from = Position.Invalid;
            to = Position.Invalid;

            if (string.IsNullOrEmpty(notation) || notation.Length < 4) return false;

            try
            {
                char fromFile = notation[0];
                char fromRank = notation[1];
                int fromCol = fromFile - 'a';
                int fromRow = 8 - (fromRank - '0');

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

        private string GetPieceName(ChessPiece piece)
        {
            if (piece == null) return "фигура";
            return piece.Type switch
            {
                PieceType.King => "Король",
                PieceType.Queen => "Ферзь",
                PieceType.Rook => "Ладья",
                PieceType.Bishop => "Слон",
                PieceType.Knight => "Конь",
                PieceType.Pawn => "Пешка",
                _ => "Фигура"
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}