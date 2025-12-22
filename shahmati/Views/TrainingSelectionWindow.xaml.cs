using shahmati.Models;
using shahmati.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel;

namespace shahmati.Views
{
    public partial class TrainingSelectionWindow : Window
    {
        private readonly TrainingViewModel _viewModel;
        private readonly int _userId;

        public TrainingSelectionWindow(int userId)
        {
            try
            {
                InitializeComponent();
                _userId = userId;

                // Инициализируем ViewModel
                _viewModel = new TrainingViewModel(userId);
                if (_viewModel == null)
                {
                    throw new Exception("Не удалось создать ViewModel");
                }

                DataContext = _viewModel;

                // Устанавливаем плейсхолдер поиска
                InitializeSearchPlaceholder();

                Loaded += async (s, e) => await InitializeTrainings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void InitializeSearchPlaceholder()
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = "Поиск тренировок...";
                SearchTextBox.Foreground = Brushes.Gray;
                SearchTextBox.FontStyle = FontStyles.Italic;
            }
        }

        private async Task InitializeTrainings()
        {
            try
            {
                if (_viewModel == null)
                {
                    MessageBox.Show("ViewModel не инициализирован", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _viewModel.LoadTrainingsAsync();

                if (TrainingsList != null && _viewModel.FilteredTrainings != null)
                {
                    TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренировок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для кнопок навигации
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

        // Обработчик для карточек тренировок
        private void TrainingCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_viewModel == null || _viewModel.AllTrainings == null)
                {
                    MessageBox.Show("Данные тренировок не загружены", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (sender is Border border && border.Tag is int trainingId)
                {
                    var selectedTraining = _viewModel.AllTrainings.FirstOrDefault(t => t.Id == trainingId);
                    if (selectedTraining != null)
                    {
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

        // Обработчики для фильтров
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CategoryRadio_Checked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void DifficultyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void DifficultyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сброс радио-кнопок категорий
                if (AllCategoryRadio != null)
                    AllCategoryRadio.IsChecked = true;

                // Сброс чекбоксов сложности
                if (BeginnerCheckBox != null)
                    BeginnerCheckBox.IsChecked = true;
                if (MediumCheckBox != null)
                    MediumCheckBox.IsChecked = true;
                if (HardCheckBox != null)
                    HardCheckBox.IsChecked = true;
                if (ExpertCheckBox != null)
                    ExpertCheckBox.IsChecked = true;

                // Сброс поиска
                if (SearchTextBox != null)
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = Brushes.Gray;
                    SearchTextBox.FontStyle = FontStyles.Italic;
                }

                // Обновление фильтрации
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод фильтрации
        private void ApplyFilters()
        {
            try
            {
                if (_viewModel == null || _viewModel.AllTrainings == null)
                {
                    return;
                }

                string searchText = SearchTextBox?.Text?.ToLower() ?? "";
                if (searchText == "поиск тренировок..." || string.IsNullOrWhiteSpace(searchText))
                {
                    searchText = "";
                }

                // Получаем выбранную категорию
                string selectedCategory = GetSelectedCategory();

                // Получаем выбранные сложности
                List<string> selectedDifficulties = GetSelectedDifficulties();

                // Фильтруем тренировки
                var filtered = _viewModel.AllTrainings
                    .Where(t => MatchesSearch(t, searchText) &&
                               MatchesCategory(t, selectedCategory) &&
                               MatchesDifficulty(t, selectedDifficulties))
                    .ToList();

                _viewModel.FilteredTrainings = new ObservableCollection<TrainingTypeDto>(filtered);

                if (TrainingsList != null)
                {
                    TrainingsList.ItemsSource = _viewModel.FilteredTrainings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private string GetSelectedCategory()
        {
            if (AllCategoryRadio?.IsChecked == true) return "Все";
            if (TacticsCategoryRadio?.IsChecked == true) return "Tactics";
            if (OpeningCategoryRadio?.IsChecked == true) return "Opening";
            if (EndgameCategoryRadio?.IsChecked == true) return "Endgame";
            if (StrategyCategoryRadio?.IsChecked == true) return "Strategy";
            return "Все";
        }

        private List<string> GetSelectedDifficulties()
        {
            var difficulties = new List<string>();

            if (BeginnerCheckBox?.IsChecked == true)
                difficulties.Add("Beginner");
            if (MediumCheckBox?.IsChecked == true)
                difficulties.Add("Medium");
            if (HardCheckBox?.IsChecked == true)
                difficulties.Add("Hard");
            if (ExpertCheckBox?.IsChecked == true)
                difficulties.Add("Expert");

            return difficulties;
        }

        private bool MatchesSearch(TrainingTypeDto training, string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return true;
            if (training == null) return false;

            return (training.Name?.ToLower()?.Contains(searchText) ?? false) ||
                   (training.Description?.ToLower()?.Contains(searchText) ?? false) ||
                   (training.Category?.ToLower()?.Contains(searchText) ?? false) ||
                   (training.Difficulty?.ToLower()?.Contains(searchText) ?? false);
        }

        private bool MatchesCategory(TrainingTypeDto training, string selectedCategory)
        {
            if (training == null) return false;
            if (selectedCategory == "Все") return true;
            return training.Category == selectedCategory;
        }

        private bool MatchesDifficulty(TrainingTypeDto training, List<string> selectedDifficulties)
        {
            if (training == null) return false;
            if (selectedDifficulties.Count == 0) return true;
            return selectedDifficulties.Contains(training.Difficulty);
        }

        // Обработчики для плейсхолдера поиска
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox?.Text == "Поиск тренировок...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
                SearchTextBox.FontStyle = FontStyles.Normal;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null && string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск тренировок...";
                SearchTextBox.Foreground = Brushes.Gray;
                SearchTextBox.FontStyle = FontStyles.Italic;
            }
        }

        // Старый обработчик для обратной совместимости
        private void CategoryRadio_Click(object sender, RoutedEventArgs e)
        {
            CategoryRadio_Checked(sender, e);
        }

        // Обработчик закрытия окна
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите выйти из выбора тренировок?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
            catch { }
        }
    }
}