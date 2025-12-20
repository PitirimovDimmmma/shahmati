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

            Console.WriteLine($"=== HISTORY WINDOW ===");
            Console.WriteLine($"UserId: {_userId}");

            Loaded += async (s, e) => await LoadGamesHistory();
        }

        private async Task LoadGamesHistory()
        {
            try
            {
                GamesContainer.Children.Clear();

                // Показываем загрузку
                var loadingText = new TextBlock
                {
                    Text = "Загрузка истории игр...",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                GamesContainer.Children.Add(loadingText);

                Console.WriteLine($"=== ЗАГРУЗКА ИГР ПОЛЬЗОВАТЕЛЯ ID={_userId} ===");

                // ПРЯМОЙ ЗАПРОС К API
                var userGames = await LoadUserGamesFromApi();

                GamesContainer.Children.Clear();

                if (userGames.Count == 0)
                {
                    ShowNoGamesMessage();
                    return;
                }

                Console.WriteLine($"✅ Загружено игр: {userGames.Count}");

                // Обновляем интерфейс
                TotalGamesText.Text = $"Всего игр: {userGames.Count}";

                // Показываем игры (новые сверху)
                foreach (var game in userGames.OrderByDescending(g => g.CreatedAt))
                {
                    var gameCard = CreateGameCard(game);
                    GamesContainer.Children.Add(gameCard);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки истории: {ex.Message}");
                ShowErrorMessage($"Ошибка загрузки: {ex.Message}");
            }
        }
        private async Task<List<GameHistoryDto>> LoadUserGamesFromApi()
        {
            try
            {
                // Используем новый эндпоинт
                string url = $"https://localhost:7259/api/games/user/{_userId}/history";

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Отключаем SSL проверку для разработки
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new HttpClient(handler);

                var response = await httpClient.GetAsync(url);
                Console.WriteLine($"Статус запроса: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Получен JSON длиной: {json.Length} символов");

                    if (!string.IsNullOrEmpty(json))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var games = JsonSerializer.Deserialize<List<GameHistoryDto>>(json, options);
                        return games ?? new List<GameHistoryDto>();
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка API: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка HTTP: {ex.Message}");
            }

            return new List<GameHistoryDto>();
        }

        private async Task<List<int>> FindUserGameIds()
        {
            var gameIds = new List<int>();

            try
            {
                Console.WriteLine($"=== ПОИСК ID ИГР ПОЛЬЗОВАТЕЛЯ {_userId} ===");

                // Пробуем диапазон ID (например, 1-50)
                for (int gameId = 1; gameId <= 50; gameId++)
                {
                    try
                    {
                        string url = $"https://localhost:7259/api/games/{gameId}";

                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(3);

                        // Отключаем SSL проверку для разработки
                        var handler = new HttpClientHandler();
                        handler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => true;

                        using var httpClient = new HttpClient(handler);

                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            if (!string.IsNullOrEmpty(json) && !json.Contains("not found"))
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var game = JsonSerializer.Deserialize<GameDto>(json, options);

                                if (game != null &&
                                    (game.WhitePlayer?.Id == _userId || game.BlackPlayer?.Id == _userId))
                                {
                                    Console.WriteLine($"✅ Найдена игра #{game.Id} для пользователя {_userId}");
                                    gameIds.Add(game.Id);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки для отдельных ID
                        continue;
                    }

                    // Небольшая пауза чтобы не перегружать сервер
                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска ID: {ex.Message}");
            }

            return gameIds;
        }

        private async Task<List<GameDto>> GetGamesByIds(List<int> gameIds)
        {
            var games = new List<GameDto>();

            try
            {
                Console.WriteLine($"=== ЗАГРУЗКА {gameIds.Count} ИГР ===");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // Отключаем SSL проверку
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new HttpClient(handler);

                foreach (int gameId in gameIds)
                {
                    try
                    {
                        string url = $"https://localhost:7259/api/games/{gameId}";
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var game = JsonSerializer.Deserialize<GameDto>(json, options);

                            if (game != null)
                            {
                                games.Add(game);
                                Console.WriteLine($"✅ Загружена игра #{game.Id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка загрузки игры #{gameId}: {ex.Message}");
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка GetGamesByIds: {ex.Message}");
            }

            return games;
        }

        private void ShowNoGamesMessage()
        {
            var messagePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
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
                Background = game.IsFinished ? Brushes.White : Brushes.LightYellow,
                BorderBrush = game.IsFinished ? Brushes.Gray : Brushes.Orange,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
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

            // Статус
            string status = game.IsFinished ? "✅ Завершена" : "⏳ В процессе";
            headerPanel.Children.Add(new TextBlock
            {
                Text = $" • {status}",
                FontSize = 12,
                Foreground = game.IsFinished ? Brushes.Green : Brushes.Blue,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            stackPanel.Children.Add(headerPanel);

            // Дата и время
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"🗓️ {game.GetFormattedDate()}",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            // Игроки
            var playersPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

            string whiteText = game.UserPlayedWhite
                ? $"⚪ Вы (белые)"
                : $"⚪ {game.WhitePlayerUsername}";

            string blackText = !game.UserPlayedWhite
                ? $"⚫ Вы (черные)"
                : $"⚫ {game.OpponentName}";

            playersPanel.Children.Add(new TextBlock
            {
                Text = whiteText,
                FontSize = 13,
                Foreground = Brushes.Black
            });

            playersPanel.Children.Add(new TextBlock
            {
                Text = blackText,
                FontSize = 13,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 2, 0, 0)
            });

            stackPanel.Children.Add(playersPanel);

            // Результат
            if (game.IsFinished)
            {
                var resultPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Brush resultColor = game.ResultForUser switch
                {
                    "Победа" => Brushes.Green,
                    "Поражение" => Brushes.Red,
                    "Ничья" => Brushes.Orange,
                    _ => Brushes.Gray
                };

                resultPanel.Children.Add(new TextBlock
                {
                    Text = "🏆 Результат: ",
                    FontSize = 13,
                    Foreground = Brushes.Black
                });

                resultPanel.Children.Add(new TextBlock
                {
                    Text = game.ResultForUser,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = resultColor,
                    Margin = new Thickness(5, 0, 0, 0)
                });

                stackPanel.Children.Add(resultPanel);
            }

            // Детали игры
            var detailsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"🎮 {game.GetGameModeDisplay()}",
                FontSize = 11,
                Foreground = Brushes.Gray
            });

            if (!string.IsNullOrEmpty(game.Difficulty))
            {
                detailsPanel.Children.Add(new TextBlock
                {
                    Text = $" • 📊 {game.Difficulty}",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(10, 0, 0, 0)
                });
            }

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $" • ⏱️ {game.GetFormattedDuration()}",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0)
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $" • 📝 {game.MoveCount} ходов",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(10, 0, 0, 0)
            });

            stackPanel.Children.Add(detailsPanel);

            border.Child = stackPanel;

            // Клик для деталей
            border.MouseDown += (s, e) => ShowGameDetails(game);
            border.Cursor = System.Windows.Input.Cursors.Hand;

            // Эффект при наведении
            border.MouseEnter += (s, e) =>
            {
                border.Background = game.IsFinished ? Brushes.WhiteSmoke : Brushes.LightGoldenrodYellow;
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = game.IsFinished ? Brushes.White : Brushes.LightYellow;
            };

            return border;
        }

        // В HistoryWindow.xaml.cs измените метод:
        private async Task<List<object>> LoadUserGameHistory()
        {
            try
            {
                string url = $"https://localhost:7259/api/games/user/{_userId}/history";

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;

                using var httpClient = new HttpClient(handler);

                var response = await httpClient.GetAsync(url);
                Console.WriteLine($"Статус запроса истории: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Получена история: {json.Length} символов");

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Десериализуем как массив объектов
                    var jsonDocument = JsonDocument.Parse(json);
                    var games = new List<object>();

                    foreach (var element in jsonDocument.RootElement.EnumerateArray())
                    {
                        games.Add(element);
                    }

                    return games;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Ошибка API истории: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка HTTP истории: {ex.Message}");
            }

            return new List<object>();
        }

        private void ShowGameDetails(GameHistoryDto game)
        {
            string details = $"Игра #{game.Id}\n\n" +
                           $"📅 Дата начала: {game.GetFormattedDate()}\n" +
                           $"⏰ Длительность: {game.GetFormattedDuration()}\n" +
                           $"⚪ Белые: {game.WhitePlayerUsername}\n" +
                           $"⚫ Чёрные: {game.OpponentName}\n" +
                           $"🏆 Результат: {game.ResultForUser}\n" +
                           $"🎮 Режим: {game.GetGameModeDisplay()}\n" +
                           $"📊 Сложность: {game.Difficulty}\n" +
                           $"📝 Ходов: {game.MoveCount}\n" +
                           $"🕐 Завершена: {(game.IsFinished ? "Да" : "Нет")}";

            if (game.IsFinished && game.FinishedAt.HasValue)
            {
                details += $"\n⏰ Время завершения: {game.FinishedAt.Value:dd.MM.yyyy HH:mm}";
            }

            MessageBox.Show(details, $"Игра #{game.Id}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetResultForUser(GameDto game, bool isWhitePlayer)
        {
            if (string.IsNullOrEmpty(game.Result))
                return "Не завершена";

            if (game.Result == "Draw")
                return "Ничья";

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
            string opponentName = game.BlackPlayer?.Username ?? "ИИ";
            bool isWhitePlayer = game.WhitePlayer?.Id == _userId;
            string resultText = GetResultForUser(game, isWhitePlayer);

            string details = $"Детали игры #{game.Id}\n\n" +
                           $"📅 Дата: {game.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                           $"⚪ Белые: {game.WhitePlayer?.Username ?? "Вы"}\n" +
                           $"⚫ Чёрные: {opponentName}\n" +
                           $"🏆 Результат: {resultText}\n" +
                           $"🎮 Режим: {game.GameMode}\n" +
                           $"📊 Сложность: {game.Difficulty}\n" +
                           $"📝 Статус: {(game.IsFinished ? "Завершена" : "В процессе")}";

            if (game.IsFinished && game.FinishedAt.HasValue)
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

        private async void FindGamesButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"=== РУЧНОЙ ПОИСК ИГР ===");

            // Показываем прогресс
            var progressText = new TextBlock
            {
                Text = "Поиск игр...",
                Foreground = Brushes.White,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            GamesContainer.Children.Clear();
            GamesContainer.Children.Add(progressText);

            // Ищем игры
            var gameIds = await FindUserGameIds();

            if (gameIds.Count > 0)
            {
                MessageBox.Show($"Найдено игр: {gameIds.Count}\nID: {string.Join(", ", gameIds)}",
                    "Результат поиска", MessageBoxButton.OK, MessageBoxImage.Information);

                // Загружаем найденные игры
                await LoadGamesHistory();
            }
            else
            {
                MessageBox.Show("Игры не найдены. Создайте новую игру.",
                    "Результат поиска", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }
    }
}