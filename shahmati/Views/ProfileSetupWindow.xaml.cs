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
        private readonly string _defaultAvatarPath = @"C:\Users\Acer\source\repos\shahmati\shahmati\ChessPieces\default_avatar.png";

        public ProfileSetupWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _apiService = new ApiService();

            // Загружаем дефолтную аватарку сразу
            LoadDefaultAvatar();
            Loaded += async (s, e) => await LoadExistingProfile();
        }

        private void LoadDefaultAvatar()
        {
            try
            {
                if (File.Exists(_defaultAvatarPath))
                {
                    _photoPath = _defaultAvatarPath;
                    AvatarImage.Source = new BitmapImage(new Uri(_defaultAvatarPath));
                }
                else
                {
                    // Если файл не найден, создаем пустую картинку
                    AvatarImage.Source = null;
                    Console.WriteLine("⚠️ Дефолтная аватарка не найдена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки дефолтной аватарки: {ex.Message}");
            }
        }

        private async Task LoadExistingProfile()
        {
            try
            {
                Console.WriteLine($"🔄 Загрузка профиля пользователя ID={_userId}");
                var profile = await _apiService.GetProfileAsync(_userId);

                if (profile != null)
                {
                    Console.WriteLine($"✅ Профиль загружен: {profile.Nickname}");

                    if (!string.IsNullOrEmpty(profile.Nickname))
                    {
                        NicknameTextBox.Text = profile.Nickname;
                    }

                    if (!string.IsNullOrEmpty(profile.PhotoPath) && profile.PhotoPath != _defaultAvatarPath)
                    {
                        if (File.Exists(profile.PhotoPath))
                        {
                            _photoPath = profile.PhotoPath;
                            AvatarImage.Source = new BitmapImage(new Uri(profile.PhotoPath));
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Фото профиля не найдено: {profile.PhotoPath}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Профиль не найден, используем дефолтные значения");
                    NicknameTextBox.Text = $"Игрок_{_userId}";
                }

                ValidateInputs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки профиля: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NicknameTextBox.Text = $"Игрок_{_userId}";
                ValidateInputs();
            }
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Выберите фото профиля"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedPath = openFileDialog.FileName;

                    // Проверяем размер файла (максимум 5MB)
                    FileInfo fileInfo = new FileInfo(selectedPath);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB
                    {
                        MessageBox.Show("Размер файла не должен превышать 5MB",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _photoPath = selectedPath;
                    AvatarImage.Source = new BitmapImage(new Uri(selectedPath));
                    Console.WriteLine($"✅ Выбрано фото: {selectedPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка выбора фото: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                string finalPhotoPath = ""; // Пустая строка вместо null

                // Если фото выбрано пользователем
                if (!string.IsNullOrEmpty(_photoPath) && _photoPath != _defaultAvatarPath && File.Exists(_photoPath))
                {
                    try
                    {
                        Console.WriteLine($"Загружаем фото: {_photoPath}");
                        // Загружаем фото на сервер
                        var uploadedPath = await _apiService.UploadAvatarAsync(_userId, _photoPath);

                        if (!string.IsNullOrEmpty(uploadedPath))
                        {
                            // Формируем полный URL
                            finalPhotoPath = $"https://localhost:7259/{uploadedPath.TrimStart('/')}";
                            Console.WriteLine($"✅ Фото загружено на сервер: {finalPhotoPath}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Не удалось загрузить фото");
                            finalPhotoPath = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Ошибка загрузки фото: {ex.Message}");
                        finalPhotoPath = "";
                    }
                }
                else
                {
                    finalPhotoPath = "";
                }

                Console.WriteLine($"🔄 Сохранение профиля для ID={_userId}");
                Console.WriteLine($"📝 Никнейм: {nickname}");
                Console.WriteLine($"📸 Фото: {finalPhotoPath}");

                var updateRequest = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    PhotoPath = finalPhotoPath // Передаем пустую строку вместо null
                };

                bool success = await _apiService.UpdateProfileAsync(_userId, updateRequest);

                if (success)
                {
                    MessageBox.Show($"✅ Профиль успешно сохранен!\nНикнейм: {nickname}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переходим к главному экрану
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
                Console.WriteLine($"❌ Exception details: {ex}");
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