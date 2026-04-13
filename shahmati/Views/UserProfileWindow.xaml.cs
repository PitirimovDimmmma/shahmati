using shahmati.Models;
using shahmati.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace shahmati.Views
{
    public partial class UserProfileWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;
        private UserWithProfileDto _user;
        private bool _dataChanged = false;

        public bool DataUpdated => _dataChanged;

        public UserProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadUserProfile();
        }

        private async Task LoadUserProfile()
        {
            try
            {
                Console.WriteLine($"=== Загрузка профиля пользователя ID={_userId} ===");

                _user = await _apiService.GetUserAsync(_userId);
                if (_user == null)
                {
                    MessageBox.Show("Не удалось загрузить данные пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Заполняем поля
                NicknameTextBox.Text = _user.Profile?.Nickname ?? _user.Username;
                EmailText.Text = _user.Email;
                RatingText.Text = (_user.Profile?.Rating ?? 0).ToString();
                RegistrationDateText.Text = _user.CreatedAt.ToString("dd.MM.yyyy");

                string roleText = GetRoleDisplayText(_user.Role);
                RoleText.Text = roleText;

                if (_user.Role == "Admin")
                {
                    AdminSection.Visibility = Visibility.Visible;
                }
                else
                {
                    AdminSection.Visibility = Visibility.Collapsed;
                }

                Console.WriteLine("✅ Профиль загружен");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }

        private string GetRoleDisplayText(string role)
        {
            return role switch
            {
                "Admin" => "👑 Администратор",
                "Moderator" => "🛡️ Модератор",
                _ => "👤 Пользователь"
            };
        }

        private void NicknameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _dataChanged = true;
            SaveButton.IsEnabled = !string.IsNullOrWhiteSpace(NicknameTextBox.Text);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nickname = NicknameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(nickname) || nickname.Length < 3)
                {
                    MessageBox.Show("Никнейм должен быть не менее 3 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveButton.IsEnabled = false;
                SaveButton.Content = "Сохранение...";

                var updateRequest = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    PhotoPath = "" // Путь к фото не используется
                };

                var success = await _apiService.UpdateProfileAsync(_userId, updateRequest);

                if (success)
                {
                    MessageBox.Show("Профиль успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    _dataChanged = false;
                    await LoadUserProfile();
                }
                else
                {
                    MessageBox.Show("Не удалось обновить профиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"❌ Exception details: {ex}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "💾 Сохранить изменения";
            }
        }



        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_user?.Role != "Admin")
            {
                MessageBox.Show("У вас нет прав администратора", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdminWindow adminWindow = new AdminWindow(_userId);
            adminWindow.Show();
            this.Close();
        }
    }
}