using System;

namespace shahmati.models
{
    public struct Position : IEquatable<Position>
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public bool IsValid() => Row >= 0 && Row < 8 && Column >= 0 && Column < 8;

        public static bool operator ==(Position a, Position b)
            => a.Row == b.Row && a.Column == b.Column;

        public static bool operator !=(Position a, Position b)
            => !(a == b);

        public override bool Equals(object obj) => obj is Position position && this == position;

        public bool Equals(Position other) => this == other;

        public override int GetHashCode() => HashCode.Combine(Row, Column);

        public override string ToString() => $"{(char)('a' + Column)}{8 - Row}";

        public static Position Invalid => new Position(-1, -1);
    }
}