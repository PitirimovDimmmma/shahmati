using shahmati.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace shahmati.Views
{
    public partial class TrainingSelectionWindow : Window
    {
        private readonly TrainingViewModel _viewModel;
        private readonly int _userId;

        public TrainingSelectionWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _viewModel = new TrainingViewModel(userId);
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.LoadTrainingsAsync();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboardWindow = new DashboardWindow(_userId);
            dashboardWindow.Show();
            this.Close();
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Статистика тренировок будет показана здесь", "Статистика",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TrainingCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int trainingId)
            {
                var selectedTraining = _viewModel.AllTrainings.FirstOrDefault(t => t.Id == trainingId);
                if (selectedTraining != null)
                {
                    // Открываем окно тренировки
                    TrainingWindow trainingWindow = new TrainingWindow(_userId, selectedTraining);
                    trainingWindow.Show();
                    this.Close();
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == "Поиск тренировок...")
            {
                _viewModel.FilteredTrainings = _viewModel.AllTrainings;
            }
            else
            {
                var query = SearchTextBox.Text.ToLower();
                _viewModel.FilteredTrainings = _viewModel.AllTrainings
                    .Where(t => t.Name.ToLower().Contains(query) ||
                               t.Description.ToLower().Contains(query))
                    .ToList();
            }
            TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
        }

        private void CategoryRadio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                string category = radioButton.Content.ToString() switch
                {
                    "Тактика" => "Tactics",
                    "Дебюты" => "Opening",
                    "Эндшпиль" => "Endgame",
                    "Скорость" => "Speed",
                    "Специальные" => "Special",
                    _ => null
                };

                if (category == null)
                {
                    _viewModel.FilteredTrainings = _viewModel.AllTrainings;
                }
                else
                {
                    _viewModel.FilteredTrainings = _viewModel.AllTrainings
                        .Where(t => t.Category == category)
                        .ToList();
                }

                TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
            }
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            AllCategoryRadio.IsChecked = true;
            BeginnerCheckBox.IsChecked = true;
            MediumCheckBox.IsChecked = true;
            HardCheckBox.IsChecked = true;

            _viewModel.FilteredTrainings = _viewModel.AllTrainings;
            TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
        }

        // Дополнительно: обработка фокуса для плейсхолдера
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск тренировок...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
                SearchTextBox.FontStyle = FontStyles.Normal;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск тренировок...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                SearchTextBox.FontStyle = FontStyles.Italic;
            }
        }
    }
}