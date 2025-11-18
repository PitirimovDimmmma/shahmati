using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Npgsql;

namespace shahmati.Views
{
    public partial class DashboardWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";
        private int _userId;

        // Конструктор с параметром userId
        public DashboardWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserData();
        }

        // Конструктор без параметров для дизайнера
        public DashboardWindow()
        {
            InitializeComponent();
            // Для дизайнера используем тестовые данные
            UserNameText.Text = "Тестовый пользователь";
            UserRatingText.Text = "Рейтинг: 1200";
        }

        private void LoadUserData()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        @"SELECT u.username, p.nickname, p.photo_path 
                          FROM users u 
                          LEFT JOIN profiles p ON u.id = p.user_id 
                          WHERE u.id = @user_id", conn))
                    {
                        cmd.Parameters.AddWithValue("user_id", _userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nickname = reader.IsDBNull(1) ? reader.GetString(0) : reader.GetString(1);
                                UserNameText.Text = nickname;

                                if (!reader.IsDBNull(2))
                                {
                                    string photoPath = reader.GetString(2);
                                    if (System.IO.File.Exists(photoPath))
                                    {
                                        UserAvatarImage.Source = new BitmapImage(new Uri(photoPath));
                                    }
                                }
                                else
                                {
                                    // Установка дефолтного аватара
                                    SetDefaultAvatar();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                // Пытаемся загрузить дефолтный аватар
                UserAvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_avatar.png"));
            }
            catch
            {
                // Если ресурс не найден, просто оставляем пустым
            }
        }

        private void GameCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем главное окно с шахматами
            MainWindow gameWindow = new MainWindow(_userId);
            gameWindow.Show();
            this.Close();
        }

        private void RulesCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем окно с правилами
            RulesWindow rulesWindow = new RulesWindow(_userId);
            rulesWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к окну входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}