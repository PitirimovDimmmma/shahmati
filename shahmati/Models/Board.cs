using System;
using System.Collections.Generic;

namespace shahmati.models
{
    public class Board
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

            // Обновляем Squares для обратной совместимости
            UpdateSquaresFromCells();
        }

        // ИЗМЕНИТЕ ЭТОТ МЕТОД С private НА internal
        internal void UpdateSquaresFromCells()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Squares[row, col] = Cells[row, col].Piece;
                }
            }
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

            // Обновляем Cells
            Cells[to.Row, to.Column].Piece = piece;
            Cells[from.Row, from.Column].Piece = null;
            piece.HasMoved = true;

            // Обновляем Squares
            UpdateSquaresFromCells();

            PieceMoved?.Invoke(from, to);
        }

        public bool IsValidMove(Position from, Position to, PieceColor currentPlayerColor)
        {
            var piece = GetPieceAt(from);
            if (piece == null || piece.Color != currentPlayerColor) return false;

            var possibleMoves = piece.GetPossibleMoves(from, this);
            return possibleMoves.Contains(to);
        }
    }
}