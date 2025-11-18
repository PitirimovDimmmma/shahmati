namespace shahmati.models
{
    public class BoardCell
    {
        public Position Position { get; set; }
        public ChessPiece Piece { get; set; }
        public string BackgroundColor { get; set; }
        public bool IsSelected { get; set; }
        public bool IsPossibleMove { get; set; }
        public bool IsAnimating { get; set; }
        public string AnimationColor { get; set; }

        public BoardCell(int row, int col, ChessPiece piece = null)
        {
            Position = new Position(row, col);
            Piece = piece;

            // НОВЫЕ ЦВЕТА: слоновая кость и светло-коричневый
            BackgroundColor = (row + col) % 2 == 0 ?
                "#F0E0B0" : "#C19A6B";

            IsSelected = false;
            IsPossibleMove = false;
            IsAnimating = false;
            AnimationColor = "#90EE90"; // Нежный зеленый для анимации
        }

        public bool HasPiece => Piece != null;
    }
}