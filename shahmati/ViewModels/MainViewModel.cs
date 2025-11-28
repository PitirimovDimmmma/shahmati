using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using shahmati.Helpers;
using shahmati.models;
using shahmati.Services;
using System.Threading.Tasks;

namespace shahmati.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Board _board;
        private GameManager _gameManager;
        private Position _selectedPosition;
        private DispatcherTimer _animationTimer;
        private double _animationProgress;
        private Position _animationFrom;
        private Position _animationTo;
        private ChessPiece _animatingPiece;
        private string _gameMode = "Человек vs Человек";
        private string _difficulty = "Средний";
        private bool _isAITurn;

        public MainViewModel()
        {
            _gameManager = new GameManager();
            _board = _gameManager.Board;
            _selectedPosition = Position.Invalid;

            InitializeAnimationTimer();
            StartNewGameCommand = new RelayCommand(StartNewGame);
            CellClickCommand = new RelayCommand<Position>(HandleCellClick);

            // Подписываемся на события GameManager
            _gameManager.PropertyChanged += GameManager_PropertyChanged;
        }

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public string CurrentPlayerText => _gameManager.CurrentPlayer == PieceColor.White ? "⚪ БЕЛЫЕ" : "⚫ ЧЁРНЫЕ";

        public bool IsAITurn
        {
            get => _isAITurn;
            private set
            {
                _isAITurn = value;
                OnPropertyChanged(nameof(IsAITurn));
            }
        }

        public string GameMode
        {
            get => _gameMode;
            set
            {
                _gameMode = value;
                OnPropertyChanged(nameof(GameMode));
            }
        }

        public string Difficulty
        {
            get => _difficulty;
            set
            {
                _difficulty = value;
                OnPropertyChanged(nameof(Difficulty));
            }
        }

        public Position SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                _selectedPosition = value;
                OnPropertyChanged(nameof(SelectedPosition));
            }
        }

        public ICommand StartNewGameCommand { get; }
        public ICommand CellClickCommand { get; }

        public double AnimationProgress => _animationProgress;
        public bool IsAnimating => _animationTimer?.IsEnabled ?? false;

        private void InitializeAnimationTimer()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void GameManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameManager.CurrentPlayer))
            {
                OnPropertyChanged(nameof(CurrentPlayerText));
                CheckAITurn();
            }
            else if (e.PropertyName == nameof(GameManager.Board))
            {
                Board = _gameManager.Board;
            }
        }

        private void CheckAITurn()
        {
            IsAITurn = (_gameMode == "Человек vs Компьютер" && _gameManager.CurrentPlayer == PieceColor.Black) ||
                       (_gameMode == "Компьютер vs Компьютер");

            if (IsAITurn && _gameManager.IsGameInProgress)
            {
                // Запускаем ход ИИ
                _ = MakeAIMoveAsync();
            }
        }

        private async Task MakeAIMoveAsync()
        {
            await Task.Delay(500); // Задержка для реалистичности

            // Используем GameManager для хода ИИ
            await _gameManager.MakeAIMoveAsync();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationProgress += 0.1;
            if (_animationProgress >= 1.0)
            {
                _animationProgress = 0;
                _animationTimer.Stop();
                CompleteAnimation();
            }
            OnPropertyChanged(nameof(AnimationProgress));
        }

        private void StartNewGame()
        {
            _gameManager.StartNewGame(GameMode, Difficulty);
            Board = _gameManager.Board;
            SelectedPosition = Position.Invalid;
            OnPropertyChanged(nameof(CurrentPlayerText));
            CheckAITurn(); // Проверяем, должен ли ИИ ходить первым
        }

        public async void HandleCellClick(Position position)
        {
            if (IsAnimating || !position.IsValid() || IsAITurn) return;

            var clickedPiece = Board.GetPieceAt(position);

            if (clickedPiece != null && clickedPiece.Color == _gameManager.CurrentPlayer)
            {
                SelectPiece(position);
                return;
            }

            if (SelectedPosition.IsValid() && clickedPiece?.Color != _gameManager.CurrentPlayer)
            {
                await TryMakeMove(SelectedPosition, position);
            }
        }

        private void SelectPiece(Position position)
        {
            ResetSelection();

            SelectedPosition = position;
            var piece = Board.GetPieceAt(position);

            if (piece != null)
            {
                var possibleMoves = piece.GetPossibleMoves(position, Board);
                foreach (var move in possibleMoves)
                {
                    var cell = GetCellAt(move);
                    if (cell != null)
                    {
                        cell.IsPossibleMove = true;
                    }
                }

                var selectedCell = GetCellAt(position);
                if (selectedCell != null)
                {
                    selectedCell.IsSelected = true;
                }
            }

            OnPropertyChanged(nameof(Board));
        }

        private void ResetSelection()
        {
            foreach (var cell in Board.CellsFlat)
            {
                cell.IsSelected = false;
                cell.IsPossibleMove = false;
            }
            SelectedPosition = Position.Invalid;
        }

        private async Task TryMakeMove(Position from, Position to)
        {
            bool moveMade = await _gameManager.MakeMove(from, to);
            if (moveMade)
            {
                StartAnimation(from, to);
            }
            else
            {
                ResetSelection();
            }
        }

        private void StartAnimation(Position from, Position to)
        {
            _animationFrom = from;
            _animationTo = to;
            _animatingPiece = Board.GetPieceAt(from);
            _animationProgress = 0;
            _animationTimer.Start();
            PlayMoveSound();
        }

        private void CompleteAnimation()
        {
            // Обновляем UI после анимации
            OnPropertyChanged(nameof(Board));
            OnPropertyChanged(nameof(CurrentPlayerText));
            ResetSelection();
        }

        private BoardCell GetCellAt(Position position)
        {
            if (!position.IsValid()) return null;
            return Board.Cells[position.Row, position.Column];
        }

        private void PlayMoveSound()
        {
            // System.Media.SystemSounds.Beep.Play();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}