using System;
using System.Windows;
using System.Windows.Controls;
using shahmati.ViewModels;

namespace shahmati.Views
{
    public partial class OnlineGamesWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly int _userId;

        public OnlineGamesWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _viewModel = new MainViewModel(userId);
            DataContext = _viewModel;

            Loaded += async (s, e) => await CheckApiConnection();
        }

        private async Task CheckApiConnection()
        {
            bool isConnected = await _viewModel.CheckApiConnection();
            if (!isConnected)
            {
                MessageBox.Show("❌ Не удалось подключиться к серверу игр.\nУбедитесь, что API запущен на localhost:7001",
                              "Ошибка подключения",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
            else
            {
                // Загружаем игры если подключение успешно
                await _viewModel.LoadActiveGamesAsync();
            }
        }

        private async void CreateGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string gameMode = ((ComboBoxItem)GameModeComboBox.SelectedItem)?.Content?.ToString() ?? "HumanVsHuman";
                string apiGameMode = gameMode == "Человек vs AI" ? "HumanVsAI" : "HumanVsHuman";

                var game = await _viewModel.CreateNewGame(apiGameMode, "Medium");
                if (game != null)
                {
                    MessageBox.Show($"Игра создана! ID: {game.Id}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await _viewModel.LoadActiveGamesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.LoadActiveGamesAsync();
                MessageBox.Show("Список игр обновлен!", "Обновлено", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении списка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void JoinGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ActiveGames.Count == 0)
            {
                MessageBox.Show("Нет доступных игр для присоединения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("Функция присоединения к игре будет реализована в следующем обновлении",
                          "В разработке",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void SpectateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ActiveGames.Count == 0)
            {
                MessageBox.Show("Нет доступных игр для наблюдения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("Функция наблюдения за игрой будет реализована в следующем обновлении",
                          "В разработке",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void GamesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is shahmati.Models.GameDto game)
            {
                _viewModel.SelectGame(game.Id);

                MessageBox.Show($"Выбрана игра:\n" +
                              $"ID: {game.Id}\n" +
                              $"Режим: {game.GameMode}\n" +
                              $"Текущий ход: {game.CurrentPlayer}\n" +
                              $"Ходов сделано: {game.Moves?.Count ?? 0}",
                              "Информация об игре",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardWindow dashboard = new DashboardWindow(_userId);
                dashboard.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе на главную: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}