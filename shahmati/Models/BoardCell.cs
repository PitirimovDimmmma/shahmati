using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace shahmati.models
{
    public class BoardCell : INotifyPropertyChanged
    {
        public Position Position { get; set; }

        private ChessPiece _piece;
        public ChessPiece Piece
        {
            get => _piece;
            set
            {
                if (_piece != value)
                {
                    _piece = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPiece));
                    OnPropertyChanged(nameof(PieceImagePath));
                }
            }
        }

        public string BackgroundColor { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPossibleMove;
        public bool IsPossibleMove
        {
            get => _isPossibleMove;
            set
            {
                if (_isPossibleMove != value)
                {
                    _isPossibleMove = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAnimating { get; set; }
        public string AnimationColor { get; set; }

        public bool HasPiece => _piece != null;

        public string PieceImagePath => _piece?.ImagePath ?? string.Empty;

        public BoardCell(int row, int col, ChessPiece piece = null)
        {
            Position = new Position(row, col);
            _piece = piece;

            BackgroundColor = (row + col) % 2 == 0 ?
                "#F0E0B0" : "#C19A6B";

            _isSelected = false;
            _isPossibleMove = false;
            IsAnimating = false;
            AnimationColor = "#90EE90";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}