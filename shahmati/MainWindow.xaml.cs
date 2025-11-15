using System.Windows;
using shahmati.ViewModels;
using Npgsql;
using System.Windows.Media.Imaging;

namespace shahmati
{
    public partial class MainWindow : Window
    {
        private int _userId;
        private string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserData();
            DataContext = new MainViewModel();
        }

        public MainWindow() // Конструктор для дизайнера
        {
            InitializeComponent();
            DataContext = new MainViewModel();
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
                                        UserAvatar.Source = new BitmapImage(new Uri(photoPath));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}");
            }
        }

        private void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция сохранения игры будет реализована позже");
        }

        private void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция загрузки игры будет реализована позже");
        }

        private void LearningModeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Режим обучения будет реализован позже");
        }
    }
}