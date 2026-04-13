using shahmati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace shahmati.Views
{
    public partial class HistoryWindow : Window
    {
        private readonly int _userId;

        public HistoryWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            Loaded += async (s, e) => await LoadGamesHistory();
        }

        private async Task LoadGamesHistory()
        {
            try
            {
                GamesContainer.Children.Clear();

                var loadingText = new TextBlock
                {
                    Text = "Загрузка истории игр...",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                GamesContainer.Children.Add(loadingText);

                var userGames = await LoadUserGamesFromApi();

                GamesContainer.Children.Clear();

                if (userGames.Count == 0)
                {
                    ShowNoGamesMessage();
                    return;
                }

                TotalGamesText.Text = $"Всего игр: {userGames.Count}";

                foreach (var game in userGames.OrderByDescending(g => g.CreatedAt))
                {
                    var gameCard = CreateGameCard(game);
                    GamesContainer.Children.Add(gameCard);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task<List<GameHistoryDto>> LoadUserGamesFromApi()
        {
            try
            {
                string url = $"https://localhost:7259/api/games/user/{_userId}/history";

                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new HttpClient(handler);
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var games = JsonSerializer.Deserialize<List<GameHistoryDto>>(json, options);
                    return games ?? new List<GameHistoryDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка HTTP: {ex.Message}");
            }

            return new List<GameHistoryDto>();
        }

        private void ShowNoGamesMessage()
        {
            var messagePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };

            messagePanel.Children.Add(new TextBlock
            {
                Text = "🎮 Игры не найдены",
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = "Сыграйте хотя бы одну партию,\nчтобы история появилась здесь",
                Foreground = Brushes.LightGray,
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            GamesContainer.Children.Add(messagePanel);
            TotalGamesText.Text = "Всего игр: 0";
        }

        private void ShowErrorMessage(string message)
        {
            GamesContainer.Children.Clear();

            var errorText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.Red,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };

            GamesContainer.Children.Add(errorText);
            TotalGamesText.Text = "Ошибка";
        }

        private Border CreateGameCard(GameHistoryDto game)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(10, 5, 10, 5),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Заголовок
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Игра #{game.Id}",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.Black
            });

            stackPanel.Children.Add(headerPanel);

            // Дата
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"🗓️ {game.GetFormattedDate()}",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            // Только информация о том, за кого играл пользователь
            var userInfoPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

            string userRole = game.UserPlayedWhite ? "Белые" : "Черные";

            userInfoPanel.Children.Add(new TextBlock
            {
                Text = $"Вы играли за: {userRole}",
                FontSize = 13,
                Foreground = Brushes.Black
            });

            stackPanel.Children.Add(userInfoPanel);

            border.Child = stackPanel;

            border.MouseDown += (s, e) => ShowGameDetails(game);
            border.Cursor = System.Windows.Input.Cursors.Hand;

            border.MouseEnter += (s, e) => border.Background = Brushes.WhiteSmoke;
            border.MouseLeave += (s, e) => border.Background = Brushes.White;

            return border;
        }

        private void ShowGameDetails(GameHistoryDto game)
        {
            string details = $"Игра #{game.Id}\n\n" +
                           $"📅 Дата начала: {game.GetFormattedDate()}\n" +
                           $"🎨 Вы играли за: {(game.UserPlayedWhite ? "Белые" : "Черные")}";

            if (game.FinishedAt.HasValue)
            {
                details += $"\n⏰ Завершена: {game.FinishedAt.Value:dd.MM.yyyy HH:mm}";
            }

            MessageBox.Show(details, $"Игра #{game.Id}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadGamesHistory();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            Close();
        }
    }
}