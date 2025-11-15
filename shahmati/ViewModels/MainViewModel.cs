using System.ComponentModel;
using System.Windows.Input;
using shahmati.Helpers;
using shahmati.models;
using System.Collections.ObjectModel;

namespace shahmati.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Board _board;
        private PieceColor _currentPlayer;
        private ObservableCollection<string> _moveHistory;

        public MainViewModel()
        {
            _board = new Board();
            _currentPlayer = PieceColor.White;
            _moveHistory = new ObservableCollection<string>();
            StartNewGameCommand = new RelayCommand(StartNewGame);
        }

        public Board Board
        {
            get => _board;
            private set
            {
                _board = value;
                OnPropertyChanged(nameof(Board));
            }
        }

        public string CurrentPlayerText => _currentPlayer == PieceColor.White ? "⚪ БЕЛЫЕ" : "⚫ ЧЁРНЫЕ";

        public ObservableCollection<string> MoveHistory
        {
            get => _moveHistory;
            set
            {
                _moveHistory = value;
                OnPropertyChanged(nameof(MoveHistory));
            }
        }

        public ICommand StartNewGameCommand { get; }

        private void StartNewGame()
        {
            Board = new Board();
            _currentPlayer = PieceColor.White;
            MoveHistory.Clear();
            MoveHistory.Add("1. --- ---");
            OnPropertyChanged(nameof(CurrentPlayerText));
            System.Windows.MessageBox.Show("Новая игра начата!");
        }

        // Метод для добавления хода в историю
        public void AddMoveToHistory(string moveNotation)
        {
            MoveHistory.Add($"{MoveHistory.Count + 1}. {moveNotation}");
        }

        // Метод для смены игрока
        public void SwitchPlayer()
        {
            _currentPlayer = _currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
            OnPropertyChanged(nameof(CurrentPlayerText));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}