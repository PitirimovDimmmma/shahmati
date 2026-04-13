using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using shahmati.Services;
using shahmati.Models;

namespace shahmati.Views
{
    public partial class ProfileSetupWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;

        public ProfileSetupWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            Loaded += async (s, e) => await LoadExistingProfile();
        }

        private async Task LoadExistingProfile()
        {
            try
            {
                Console.WriteLine($"🔄 Загрузка профиля пользователя ID={_userId}");
                var profile = await _apiService.GetProfileAsync(_userId);

                if (profile != null && !string.IsNullOrEmpty(profile.Nickname))
                {
                    NicknameTextBox.Text = profile.Nickname;
                }
                else
                {
                    NicknameTextBox.Text = $"Игрок_{_userId}";
                }

                ValidateInputs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки профиля: {ex.Message}");
                NicknameTextBox.Text = $"Игрок_{_userId}";
                ValidateInputs();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string nickname = NicknameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(nickname) || nickname.Length < 3)
            {
                MessageBox.Show("Никнейм должен быть не менее 3 символов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NicknameTextBox.Focus();
                return;
            }

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            try
            {
                var updateRequest = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    PhotoPath = ""
                };

                bool success = await _apiService.UpdateProfileAsync(_userId, updateRequest);

                if (success)
                {
                    MessageBox.Show($"✅ Профиль успешно сохранен!\nНикнейм: {nickname}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DashboardWindow dashboardWindow = new DashboardWindow(_userId);
                    dashboardWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Не удалось сохранить профиль. Проверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка сохранения профиля:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Сохранить профиль";
            }
        }

        private void NicknameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ValidateInputs()
        {
            string nickname = NicknameTextBox.Text.Trim();
            bool isValid = !string.IsNullOrEmpty(nickname) && nickname.Length >= 3;
            SaveButton.IsEnabled = isValid;
        }
    }
}