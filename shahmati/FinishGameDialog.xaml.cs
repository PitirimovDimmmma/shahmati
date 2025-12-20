using System.Windows;

namespace shahmati
{
    public partial class FinishGameDialog : Window
    {
        public string SelectedResult { get; private set; } = "Draw";

        public FinishGameDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (WhiteWinRadio.IsChecked == true)
                SelectedResult = "White";
            else if (BlackWinRadio.IsChecked == true)
                SelectedResult = "Black";
            else
                SelectedResult = "Draw";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}