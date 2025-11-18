using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using shahmati.Helpers;
using shahmati.models;

namespace shahmati.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Board _board;
        private PieceColor _currentPlayer;
        private Position _selectedPosition;
        private DispatcherTimer _animationTimer;
        private double _animationProgress;
        private Position _animationFrom;
        private Position _animationTo;
        private ChessPiece _animatingPiece;

        public MainViewModel()
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _selectedPosition = Position.Invalid;

            InitializeAnimationTimer();
            StartNewGameCommand = new RelayCommand(StartNewGame);
            CellClickCommand = new RelayCommand<Position>(HandleCellClick);
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

        public string CurrentPlayerText => _currentPlayer == PieceColor.White ? "⚪ БЕЛЫЕ" : "⚫ ЧЁРНЫЕ";

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
            Board = new Board();
            _currentPlayer = PieceColor.White;
            SelectedPosition = Position.Invalid;
            OnPropertyChanged(nameof(CurrentPlayerText));
        }

        public void HandleCellClick(Position position)
        {
            if (IsAnimating || !position.IsValid()) return;

            var clickedPiece = Board.GetPieceAt(position);

            if (clickedPiece != null && clickedPiece.Color == _currentPlayer)
            {
                SelectPiece(position);
                return;
            }

            if (SelectedPosition.IsValid() && clickedPiece?.Color != _currentPlayer)
            {
                TryMakeMove(SelectedPosition, position);
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

        private void TryMakeMove(Position from, Position to)
        {
            if (Board.IsValidMove(from, to, _currentPlayer))
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
            var piece = Board.GetPieceAt(_animationFrom);
            var capturedPiece = Board.GetPieceAt(_animationTo);

            Board.MovePiece(_animationFrom, _animationTo);
            SwitchPlayer();
            ResetSelection();
            OnPropertyChanged(nameof(Board));
        }

        private BoardCell GetCellAt(Position position)
        {
            if (!position.IsValid()) return null;
            return Board.Cells[position.Row, position.Column];
        }

        public void SwitchPlayer()
        {
            _currentPlayer = _currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
            OnPropertyChanged(nameof(CurrentPlayerText));
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