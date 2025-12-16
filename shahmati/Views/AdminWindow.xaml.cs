using System.Windows;
using shahmati.ViewModels;

namespace shahmati.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow(int adminId)
        {
            InitializeComponent();
            DataContext = new AdminViewModel(adminId);
        }
    }
}