using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using shahmati.Services;
using shahmati.Models;

namespace shahmati.Views
{
    public partial class ProfileSetupWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;
        private string _photoPath;

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
                var profile = await _apiService.GetProfileAsync(_userId);
                if (profile != null && !string.IsNullOrEmpty(profile.Nickname))
                {
                    NicknameTextBox.Text = profile.Nickname;

                    if (!string.IsNullOrEmpty(profile.PhotoPath))
                    {
                        _photoPath = profile.PhotoPath;
                        AvatarImage.Source = new BitmapImage(new Uri(_photoPath));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _photoPath = openFileDialog.FileName;
                AvatarImage.Source = new BitmapImage(new Uri(_photoPath));
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string nickname = NicknameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(nickname) || nickname.Length < 3)
            {
                MessageBox.Show("Никнейм должен быть не менее 3 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            try
            {
                string finalPhotoPath = null;

                // Если фото выбрано, копируем его в папку приложения
                if (!string.IsNullOrEmpty(_photoPath))
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(appDataPath, "ChessTrainer");
                    Directory.CreateDirectory(appFolder);
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(_photoPath);
                    finalPhotoPath = Path.Combine(appFolder, fileName);
                    File.Copy(_photoPath, finalPhotoPath, true);
                }

                var updateRequest = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    PhotoPath = finalPhotoPath
                };

                bool success = await _apiService.UpdateProfileAsync(_userId, updateRequest);

                if (success)
                {
                    MessageBox.Show("Профиль успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переходим к главному экрану
                    DashboardWindow dashboardWindow = new DashboardWindow(_userId);
                    dashboardWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Не удалось сохранить профиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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