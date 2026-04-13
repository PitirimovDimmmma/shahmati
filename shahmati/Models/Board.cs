using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace shahmati.models
{
    public class Board : INotifyPropertyChanged
    {
        public ChessPiece[,] Squares { get; private set; }
        public BoardCell[,] Cells { get; private set; }

        public List<BoardCell> CellsFlat
        {
            get
            {
                var list = new List<BoardCell>();
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        list.Add(Cells[row, col]);
                    }
                }
                return list;
            }
        }

        public event Action<Position, Position> PieceMoved;
        public event PropertyChangedEventHandler PropertyChanged;

        public Board()
        {
            Squares = new ChessPiece[8, 8];
            Cells = new BoardCell[8, 8];
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // Создаем клетки доски
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Cells[row, col] = new BoardCell(row, col);
                }
            }

            // Расставляем черные фигуры
            Cells[0, 0].Piece = new Rook(PieceColor.Black);
            Cells[0, 1].Piece = new Knight(PieceColor.Black);
            Cells[0, 2].Piece = new Bishop(PieceColor.Black);
            Cells[0, 3].Piece = new Queen(PieceColor.Black);
            Cells[0, 4].Piece = new King(PieceColor.Black);
            Cells[0, 5].Piece = new Bishop(PieceColor.Black);
            Cells[0, 6].Piece = new Knight(PieceColor.Black);
            Cells[0, 7].Piece = new Rook(PieceColor.Black);

            // Черные пешки
            for (int col = 0; col < 8; col++)
            {
                Cells[1, col].Piece = new Pawn(PieceColor.Black);
            }

            // Белые фигуры
            Cells[7, 0].Piece = new Rook(PieceColor.White);
            Cells[7, 1].Piece = new Knight(PieceColor.White);
            Cells[7, 2].Piece = new Bishop(PieceColor.White);
            Cells[7, 3].Piece = new Queen(PieceColor.White);
            Cells[7, 4].Piece = new King(PieceColor.White);
            Cells[7, 5].Piece = new Bishop(PieceColor.White);
            Cells[7, 6].Piece = new Knight(PieceColor.White);
            Cells[7, 7].Piece = new Rook(PieceColor.White);

            // Белые пешки
            for (int col = 0; col < 8; col++)
            {
                Cells[6, col].Piece = new Pawn(PieceColor.White);
            }

            UpdateSquaresFromCells();
        }

        public void UpdateSquaresFromCells()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Squares[row, col] = Cells[row, col].Piece;
                }
            }
        }

        public void ForceUpdate()
        {
            UpdateSquaresFromCells();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellsFlat)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Squares)));
        }

        public ChessPiece GetPieceAt(Position position)
        {
            if (!position.IsValid()) return null;
            return Squares[position.Row, position.Column];
        }

        public void MovePiece(Position from, Position to)
        {
            var piece = GetPieceAt(from);
            if (piece == null) return;

            Console.WriteLine($"MovePiece: {GetSquareNotation(from)} -> {GetSquareNotation(to)}, Piece: {piece.Type} {piece.Color}");

            // Обновляем Cells
            Cells[to.Row, to.Column].Piece = piece;
            Cells[from.Row, from.Column].Piece = null;
            piece.HasMoved = true;

            UpdateSquaresFromCells();
            ForceUpdate();
            PieceMoved?.Invoke(from, to);
        }

        public bool IsValidMove(Position from, Position to, PieceColor currentPlayerColor)
        {
            var piece = GetPieceAt(from);
            if (piece == null || piece.Color != currentPlayerColor) return false;

            var possibleMoves = piece.GetPossibleMoves(from, this);

            Console.WriteLine($"IsValidMove: from={GetSquareNotation(from)} to={GetSquareNotation(to)}");
            Console.WriteLine($"Possible moves count: {possibleMoves.Count}");
            foreach (var move in possibleMoves)
            {
                Console.WriteLine($"  Possible: {GetSquareNotation(move)}");
            }

            return possibleMoves.Contains(to);
        }

        public void LoadFromFen(string fen)
        {
            try
            {
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Cells[row, col].Piece = null;
                    }
                }

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

                            Cells[row, col].Piece = piece;
                            col++;
                        }
                    }
                }

                UpdateSquaresFromCells();
                ForceUpdate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки FEN: {ex.Message}");
            }
        }

        // Вспомогательный метод для отладки
        private string GetSquareNotation(Position position)
        {
            char file = (char)('a' + position.Column);
            int rank = 8 - position.Row;
            return $"{file}{rank}";
        }
    }
}