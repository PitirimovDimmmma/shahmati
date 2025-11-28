using shahmati.models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace shahmati.Services
{
    public class GameManager : INotifyPropertyChanged
    {
        private Board _board;
        private AIPlayer _aiPlayerWhite;
        private AIPlayer _aiPlayerBlack;
        private string _gameMode;
        private string _difficulty;

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

        public bool IsAITurn => (_gameMode == "Человек vs Компьютер" && CurrentPlayer == PieceColor.Black) ||
                               (_gameMode == "Компьютер vs Компьютер");

        public GameManager()
        {
            InitializeGame();
        }

        public void StartNewGame(string gameMode, string difficulty)
        {
            _gameMode = gameMode;
            _difficulty = difficulty;

            InitializeGame();

            // Инициализируем ИИ игроков
            int difficultyLevel = GetDifficultyLevel(difficulty);
            _aiPlayerWhite = new AIPlayer(PieceColor.White, difficultyLevel);
            _aiPlayerBlack = new AIPlayer(PieceColor.Black, difficultyLevel);

            OnPropertyChanged(nameof(CurrentPlayer));
            OnPropertyChanged(nameof(IsAITurn));

            // Если режим "Компьютер vs Компьютер", начинаем сразу ход ИИ
            if (_gameMode == "Компьютер vs Компьютер")
            {
                _ = MakeAIMoveAsync();
            }
        }

        private int GetDifficultyLevel(string difficulty)
        {
            return difficulty switch
            {
                "Новичок" => 0,
                "Лёгкий" => 1,
                "Средний" => 2,
                "Сложный" => 3,
                "Эксперт" => 4,
                _ => 2
            };
        }

        private void InitializeGame()
        {
            Board = new Board();
            CurrentPlayer = PieceColor.White;
            MoveHistory.Clear();
            IsGameInProgress = true;
        }

        public async Task<bool> MakeMove(Position from, Position to)
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
            SwitchPlayer();

            return true;
        }

        public async Task MakeAIMoveAsync()
        {
            if (!IsGameInProgress || !IsAITurn) return;

            var aiPlayer = CurrentPlayer == PieceColor.White ? _aiPlayerWhite : _aiPlayerBlack;
            var bestMove = aiPlayer?.GetBestMove(Board);

            if (bestMove != null)
            {
                // Небольшая задержка для реалистичности
                await Task.Delay(500);

                var piece = Board.GetPieceAt(bestMove.From);
                var capturedPiece = Board.GetPieceAt(bestMove.To);

                // Выполняем ход
                Board.MovePiece(bestMove.From, bestMove.To);

                // Сохраняем в историю
                var move = new Move(bestMove.From, bestMove.To, piece)
                {
                    CapturedPiece = capturedPiece
                };
                MoveHistory.Add(move);

                // Передаем ход следующему игроку
                SwitchPlayer();
            }
        }

        private void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
            OnPropertyChanged(nameof(CurrentPlayer));
            OnPropertyChanged(nameof(IsAITurn));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}