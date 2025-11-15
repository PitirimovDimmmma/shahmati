namespace shahmati.models
{
    public class Move
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public ChessPiece Piece { get; set; }
        public ChessPiece CapturedPiece { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public string Notation { get; set; }

        public Move(Position from, Position to, ChessPiece piece)
        {
            From = from;
            To = to;
            Piece = piece;
        }

        public override string ToString() => $"{Piece} from {From} to {To}";
    }
}