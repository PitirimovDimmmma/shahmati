namespace shahmati.models
{
    public class Player
    {
        public string Name { get; set; }
        public PieceColor Color { get; set; }
        public bool IsHuman { get; set; } = true;
        public int Rating { get; set; } = 0;

        public Player(string name, PieceColor color)
        {
            Name = name;
            Color = color;
        }
    }
}