using shahmati.Models;
using shahmati.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

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

            // Устанавливаем плейсхолдер
            SearchPlaceholder.Text = "Поиск тренировок...";

            Loaded += async (s, e) => await InitializeTrainings();
        }

        private async Task InitializeTrainings()
        {
            try
            {
                await _viewModel.LoadTrainingsAsync();
                TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренировок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardWindow dashboardWindow = new DashboardWindow(_userId);
                dashboardWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Статистика тренировок будет реализована в следующем обновлении",
                    "Статистика",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TrainingCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия тренировки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchTextBox.Text.ToLower();

                if (string.IsNullOrWhiteSpace(searchText) || searchText == "поиск тренировок...")
                {
                    _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(_viewModel.AllTrainings);
                }
                else
                {
                    var filtered = _viewModel.AllTrainings
                        .Where(t => t.Name.ToLower().Contains(searchText) ||
                                   t.Description.ToLower().Contains(searchText) ||
                                   t.Category.ToLower().Contains(searchText) ||
                                   t.Difficulty.ToLower().Contains(searchText))
                        .ToList();

                    _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(filtered);
                }

                TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска: {ex.Message}");
            }
        }

        private void CategoryRadio_Click(object sender, RoutedEventArgs e)
        {
            try
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
                        _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(_viewModel.AllTrainings);
                    }
                    else
                    {
                        var filtered = _viewModel.AllTrainings
                            .Where(t => t.Category == category)
                            .ToList();

                        _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(filtered);
                    }

                    TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка фильтрации по категории: {ex.Message}");
            }
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchTextBox.Text = "";
                AllCategoryRadio.IsChecked = true;
                BeginnerCheckBox.IsChecked = true;
                MediumCheckBox.IsChecked = true;
                HardCheckBox.IsChecked = true;

                _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(_viewModel.AllTrainings);
                TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchTextBox.Text == "Поиск тренировок...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
                    SearchTextBox.FontStyle = FontStyles.Normal;
                }
            }
            catch { }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    SearchTextBox.Text = "Поиск тренировок...";
                    SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                    SearchTextBox.FontStyle = FontStyles.Italic;
                }
            }
            catch { }
        }
    }
}