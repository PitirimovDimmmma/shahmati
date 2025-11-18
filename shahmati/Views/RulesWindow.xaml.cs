using System.Windows;

namespace shahmati.Views
{
    public partial class RulesWindow : Window
    {
        private int _userId;

        // Конструктор с параметром userId
        public RulesWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        // Конструктор без параметров для дизайнера
        public RulesWindow()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на главный экран
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }
    }
}