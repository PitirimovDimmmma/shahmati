using shahmati.Services;
using System.Threading.Tasks;

namespace shahmati.Helpers
{
    public static class DatabaseHelper
    {
        private static ApiService _apiService = new ApiService();

        public static async Task<bool> TestApiConnection()
        {
            return await _apiService.TestConnectionAsync();
        }

        // Удалите все методы работы с базой данных напрямую
        // Теперь всё работает через API
    }
}
/*{
    public static class DatabaseHelper
    {
        private static string connectionString = "Host=localhost;Port=5436;Database=kursovoi;Username=postgres;Password=2005";

        public static void InitializeDatabase()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Создаем таблицы, если они не существуют
                    using (var cmd = new NpgsqlCommand(@"
                        CREATE TABLE IF NOT EXISTS users (
                            id SERIAL PRIMARY KEY,
                            username VARCHAR(50) UNIQUE NOT NULL,
                            email VARCHAR(100) UNIQUE NOT NULL,
                            password_hash VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE TABLE IF NOT EXISTS profiles (
                            id SERIAL PRIMARY KEY,
                            user_id INTEGER REFERENCES users(id),
                            nickname VARCHAR(50) UNIQUE NOT NULL,
                            photo_path VARCHAR(255),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        );

                        CREATE TABLE IF NOT EXISTS saved_games (
                            id SERIAL PRIMARY KEY,
                            user_id INTEGER REFERENCES users(id),
                            game_data TEXT NOT NULL,
                            game_name VARCHAR(100),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        );", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}\n\n" +
                    "Убедитесь, что:\n" +
                    "1. PostgreSQL запущен\n" +
                    "2. База данных 'kursovoi' существует\n" +
                    "3. Пароль правильный");
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                return false;
            }
        }

        public static string ConnectionString => connectionString;
    }
}*/