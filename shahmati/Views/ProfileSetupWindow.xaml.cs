using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Npgsql;
using System.Text.RegularExpressions;
using System.IO;

namespace shahmati.Views
{
    public partial class ProfileSetupWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";
        private int _userId;
        private string _photoPath;

        // Конструктор с параметром userId
        public ProfileSetupWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;

            // Добавляем обработчик изменения текста
            NicknameTextBox.TextChanged += NicknameTextBox_TextChanged;
        }

        // Конструктор без параметров для дизайнера
        public ProfileSetupWindow()
        {
            InitializeComponent();
            NicknameTextBox.TextChanged += NicknameTextBox_TextChanged;
        }

        private void NicknameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ValidateInputs()
        {
            bool isValid = true;

            string nickname = NicknameTextBox.Text;
            if (string.IsNullOrEmpty(nickname) || nickname.Length < 3)
            {
                NicknameError.Text = "Никнейм должен быть не менее 3 символов";
                NicknameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (!Regex.IsMatch(nickname, @"^[a-zA-Z0-9]+$"))
            {
                NicknameError.Text = "Только латинские буквы и цифры";
                NicknameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (IsNicknameExists(nickname))
            {
                NicknameError.Text = "Никнейм уже занят";
                NicknameError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                NicknameError.Visibility = Visibility.Collapsed;
            }

            SaveButton.IsEnabled = isValid;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nickname = NicknameTextBox.Text;

                // Если фото выбрано, копируем его в папку приложения
                string finalPhotoPath = null;
                if (!string.IsNullOrEmpty(_photoPath))
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(appDataPath, "ChessTrainer");
                    Directory.CreateDirectory(appFolder);
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(_photoPath);
                    finalPhotoPath = Path.Combine(appFolder, fileName);
                    File.Copy(_photoPath, finalPhotoPath, true);
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO profiles (user_id, nickname, photo_path) VALUES (@user_id, @nickname, @photo_path)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("user_id", _userId);
                        cmd.Parameters.AddWithValue("nickname", nickname);
                        cmd.Parameters.AddWithValue("photo_path", finalPhotoPath ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show("Профиль успешно создан!");

                        // Переходим к ГЛАВНОМУ ЭКРАНУ (Dashboard)
                        DashboardWindow dashboardWindow = new DashboardWindow(_userId);
                        dashboardWindow.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении профиля: {ex.Message}");
            }
        }

        private bool IsNicknameExists(string nickname)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM profiles WHERE nickname = @nickname", conn))
                    {
                        cmd.Parameters.AddWithValue("nickname", nickname);
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}