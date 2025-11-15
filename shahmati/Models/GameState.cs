using System.Collections.Generic;

namespace shahmati.models
{
    public class GameState
    {
        public Board Board { get; set; }
        public PieceColor CurrentPlayer { get; set; }
        public List<Move> MoveHistory { get; set; } = new List<Move>();
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
        public string GameMode { get; set; }
        public string Difficulty { get; set; }

        public GameState()
        {
            Board = new Board();
            CurrentPlayer = PieceColor.White;
        }
    }
}