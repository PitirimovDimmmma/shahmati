using shahmati.Models;
using shahmati.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace shahmati.Views
{
    public partial class UserProfileWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly int _userId;
        private string _photoPath;
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

                // Загружаем данные пользователя
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

                // Загружаем аватар
                LoadAvatar();

                Console.WriteLine("✅ Профиль загружен");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }

        private void LoadAvatar()
        {
            try
            {
                if (_user?.Profile != null && !string.IsNullOrEmpty(_user.Profile.PhotoPath))
                {
                    if (File.Exists(_user.Profile.PhotoPath))
                    {
                        ProfileAvatarImage.Source = new BitmapImage(new Uri(_user.Profile.PhotoPath));
                        _photoPath = _user.Profile.PhotoPath;
                    }
                    else
                    {
                        SetDefaultAvatar();
                    }
                }
                else
                {
                    SetDefaultAvatar();
                }
            }
            catch
            {
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                ProfileAvatarImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/default_avatar.png"));
            }
            catch
            {
                ProfileAvatarImage.Source = null;
            }
        }

        private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Выберите изображение профиля"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _photoPath = openFileDialog.FileName;
                ProfileAvatarImage.Source = new BitmapImage(new Uri(_photoPath));
                _dataChanged = true;
                SaveButton.IsEnabled = true;
            }
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

                string finalPhotoPath = _user?.Profile?.PhotoPath;

                // Если выбрано новое фото
                if (!string.IsNullOrEmpty(_photoPath) && _photoPath != finalPhotoPath)
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(appDataPath, "ChessTrainer", "Avatars");
                    Directory.CreateDirectory(appFolder);

                    string fileName = $"{_userId}_{Guid.NewGuid()}{Path.GetExtension(_photoPath)}";
                    finalPhotoPath = Path.Combine(appFolder, fileName);

                    File.Copy(_photoPath, finalPhotoPath, true);
                }

                // Обновляем профиль
                var updateRequest = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    PhotoPath = finalPhotoPath
                };

                bool success = await _apiService.UpdateProfileAsync(_userId, updateRequest);

                if (success)
                {
                    MessageBox.Show("Профиль успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    _dataChanged = false;
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Не удалось обновить профиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "💾 Сохранить изменения";
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция смены пароля будет реализована в следующем обновлении",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить аккаунт?\n\nЭто действие нельзя отменить!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Функция удаления аккаунта будет реализована в следующем обновлении",
                    "В разработке",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}