using shahmati.models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shahmati.Services
{
    public class GameManager : INotifyPropertyChanged
    {
        private Board _board;

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public PieceColor CurrentPlayer { get; private set; } = PieceColor.White;
        public bool IsGameInProgress { get; private set; }
        public List<Move> MoveHistory { get; } = new List<Move>();

        public GameManager()
        {
            InitializeGame();
        }

        public void StartNewGame(string gameMode, string difficulty)
        {
            InitializeGame();
            IsGameInProgress = true;
            // TODO: Реализовать логику для разных режимов игры и сложности
        }

        private void InitializeGame()
        {
            Board = new Board();
            CurrentPlayer = PieceColor.White;
            MoveHistory.Clear();
            IsGameInProgress = true;
        }

        public bool MakeMove(Position from, Position to)
        {
            if (!Board.IsValidMove(from, to, CurrentPlayer))
                return false;

            var piece = Board.GetPieceAt(from);
            var capturedPiece = Board.GetPieceAt(to);

            // Выполняем ход
            Board.MovePiece(from, to);

            // Сохраняем в историю
            var move = new Move(from, to, piece)
            {
                CapturedPiece = capturedPiece
            };
            MoveHistory.Add(move);

            // Передаем ход следующему игроку
            CurrentPlayer = CurrentPlayer == PieceColor.White ?
                PieceColor.Black : PieceColor.White;

            OnPropertyChanged(nameof(CurrentPlayer));
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
