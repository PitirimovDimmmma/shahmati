using shahmati.Models;
using shahmati.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace shahmati.Views
{
    public partial class HistoryWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        public HistoryWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadGamesHistory();
        }

        private async Task LoadGamesHistory()
        {
            try
            {
                GamesContainer.Children.Clear();

                var games = await _apiService.GetUserGamesAsync(_userId);
                if (games == null || games.Count == 0)
                {
                    var noGamesText = new TextBlock
                    {
                        Text = "У вас пока нет завершенных игр",
                        Foreground = Brushes.White,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    GamesContainer.Children.Add(noGamesText);
                    TotalGamesText.Text = "Всего игр: 0";
                    return;
                }

                TotalGamesText.Text = $"Всего игр: {games.Count}";

                // Отображаем только завершенные игры
                var finishedGames = games.Where(g => g.IsFinished).ToList();

                foreach (var game in finishedGames.OrderByDescending(g => g.FinishedAt))
                {
                    var gameCard = CreateGameCard(game);
                    GamesContainer.Children.Add(gameCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории игр: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateGameCard(GameDto game)
        {
            var border = new Border
            {
                Style = (Style)FindResource("GameCard"),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stackPanel = new StackPanel();

            // Заголовок с ID и датой
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Игра #{game.Id}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.Black
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $" • {game.FinishedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Неизвестно"}",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(5, 0, 0, 0)
            });

            stackPanel.Children.Add(headerPanel);

            // Игроки
            var playersPanel = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };

            var whitePlayerText = new TextBlock
            {
                Text = $"⚪ Белые: {game.WhitePlayer.Username}",
                FontSize = 12,
                Foreground = Brushes.Black
            };

            var blackPlayerText = new TextBlock
            {
                Text = $"⚫ Чёрные: {(game.BlackPlayer?.Username ?? "ИИ")}",
                FontSize = 12,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 2, 0, 0)
            };

            playersPanel.Children.Add(whitePlayerText);
            playersPanel.Children.Add(blackPlayerText);
            stackPanel.Children.Add(playersPanel);

            // Результат
            var resultText = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };

            // Определяем результат для текущего пользователя
            bool isWhitePlayer = game.WhitePlayer.Id == _userId;
            string resultForUser = GetResultForUser(game, isWhitePlayer);

            resultText.Text = $"Результат: {resultForUser}";
            resultText.Foreground = GetResultColor(resultForUser);

            stackPanel.Children.Add(resultText);

            // Дополнительная информация
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };

            infoPanel.Children.Add(new TextBlock
            {
                Text = $"Режим: {game.GameMode}",
                FontSize = 11,
                Foreground = Brushes.Gray
            });

            if (!string.IsNullOrEmpty(game.Difficulty) && game.Difficulty != "Medium")
            {
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $" • Сложность: {game.Difficulty}",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(5, 0, 0, 0)
                });
            }

            stackPanel.Children.Add(infoPanel);

            border.Child = stackPanel;
            border.MouseDown += (s, e) => ShowGameDetails(game);

            return border;
        }

        private string GetResultForUser(GameDto game, bool isWhitePlayer)
        {
            if (game.Result == "Draw") return "Ничья";

            if ((game.Result == "White" && isWhitePlayer) ||
                (game.Result == "Black" && !isWhitePlayer))
                return "Победа";

            return "Поражение";
        }

        private Brush GetResultColor(string result)
        {
            return result switch
            {
                "Победа" => Brushes.Green,
                "Поражение" => Brushes.Red,
                "Ничья" => Brushes.Orange,
                _ => Brushes.Gray
            };
        }

        private void ShowGameDetails(GameDto game)
        {
            MessageBox.Show(
                $"Детали игры #{game.Id}\n\n" +
                $"Белые: {game.WhitePlayer.Username}\n" +
                $"Чёрные: {game.BlackPlayer?.Username ?? "ИИ"}\n" +
                $"Результат: {game.Result}\n" +
                $"Режим: {game.GameMode}\n" +
                $"Сложность: {game.Difficulty}\n" +
                $"Завершена: {game.FinishedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Неизвестно"}\n" +
                $"Ходов: {game.Moves?.Count ?? 0}",
                $"Игра #{game.Id}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadGamesHistory();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }
    }
}