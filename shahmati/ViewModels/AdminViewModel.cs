using shahmati.Helpers;
using shahmati.Models.Admin;
using shahmati.Services;
using shahmati.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace shahmati.ViewModels
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        private readonly AdminService _adminService;
        private readonly int _adminId;

        private List<AdminUserDto> _allUsers = new();
        private List<AdminUserDto> _filteredUsers = new();
        private AdminUserDto _selectedUser;
        private AdminStatsDto _stats;
        private string _searchText = "";
        private bool _isLoading;
        private string _statusMessage = "";

        public List<AdminUserDto> AllUsers
        {
            get => _allUsers;
            set
            {
                _allUsers = value;
                OnPropertyChanged(nameof(AllUsers));
                FilterUsers();
            }
        }

        public List<AdminUserDto> FilteredUsers
        {
            get => _filteredUsers;
            set
            {
                _filteredUsers = value;
                OnPropertyChanged(nameof(FilteredUsers));
            }
        }

        public AdminUserDto SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                OnPropertyChanged(nameof(IsUserSelected));
            }
        }

        public AdminStatsDto Stats
        {
            get => _stats;
            set
            {
                _stats = value;
                OnPropertyChanged(nameof(Stats));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterUsers();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool IsUserSelected => SelectedUser != null;

        // Команды
        public ICommand LoadDataCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand MakeAdminCommand { get; }
        public ICommand MakeClientCommand { get; }
        public ICommand ToggleBlockCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ExportUsersCommand { get; }
        public ICommand CloseCommand { get; }

        public AdminViewModel(int adminId)
        {
            _adminId = adminId;
            _adminService = new AdminService();

            // Инициализация команд
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            MakeAdminCommand = new RelayCommand(async () => await ChangeUserRoleAsync("Admin"));
            MakeClientCommand = new RelayCommand(async () => await ChangeUserRoleAsync("Client"));
            ToggleBlockCommand = new RelayCommand(async () => await ToggleBlockUserAsync());
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync());
            ExportUsersCommand = new RelayCommand(ExportUsers);
            CloseCommand = new RelayCommand(() => CloseWindow());

            // Загружаем данные при создании
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Загрузка данных...";

            try
            {
                // Параллельная загрузка пользователей и статистики
                var usersTask = _adminService.GetAllUsersAsync(_adminId);
                var statsTask = _adminService.GetAdminStatsAsync(_adminId);

                await Task.WhenAll(usersTask, statsTask);

                AllUsers = await usersTask;
                Stats = await statsTask;

                StatusMessage = $"Загружено {AllUsers.Count} пользователей";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterUsers()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredUsers = AllUsers;
                return;
            }

            var searchLower = SearchText.ToLower();
            FilteredUsers = AllUsers.Where(u =>
                u.Username.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower) ||
                u.Nickname.ToLower().Contains(searchLower) ||
                u.Role.ToLower().Contains(searchLower)
            ).ToList();
        }

        private async Task ChangeUserRoleAsync(string role)
        {
            if (SelectedUser == null) return;

            var confirm = MessageBox.Show(
                $"Изменить роль пользователя '{SelectedUser.Username}' на '{role}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var request = new UpdateUserRoleRequest
            {
                Role = role,
                IsBlocked = SelectedUser.IsBlocked,
                IsActive = SelectedUser.IsActive
            };

            IsLoading = true;
            bool success = await _adminService.UpdateUserRoleAsync(_adminId, SelectedUser.Id, request);
            IsLoading = false;

            if (success)
            {
                StatusMessage = $"Роль пользователя изменена на {role}";
                SelectedUser.Role = role;
                OnPropertyChanged(nameof(SelectedUser));
                await LoadDataAsync(); // Обновляем список
            }
        }

        private async Task ToggleBlockUserAsync()
        {
            if (SelectedUser == null) return;

            string action = SelectedUser.IsBlocked ? "разблокировать" : "заблокировать";
            var confirm = MessageBox.Show(
                $"{action.ToUpper()} пользователя '{SelectedUser.Username}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            bool success = await _adminService.ToggleBlockUserAsync(_adminId, SelectedUser.Id);
            IsLoading = false;

            if (success)
            {
                StatusMessage = $"Пользователь {action}ован";
                SelectedUser.IsBlocked = !SelectedUser.IsBlocked;
                OnPropertyChanged(nameof(SelectedUser));
                await LoadDataAsync();
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            var confirm = MessageBox.Show(
                $"УДАЛИТЬ пользователя '{SelectedUser.Username}'?\nЭто действие нельзя отменить!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            bool success = await _adminService.DeleteUserAsync(_adminId, SelectedUser.Id);
            IsLoading = false;

            if (success)
            {
                StatusMessage = $"Пользователь удален";
                await LoadDataAsync(); // Обновляем список
                SelectedUser = null;
            }
        }

        private void ExportUsers()
        {
            try
            {
                string csv = "ID,Username,Email,Role,Blocked,Active,Rating,Created\n";
                foreach (var user in AllUsers)
                {
                    csv += $"{user.Id},{user.Username},{user.Email},{user.Role}," +
                           $"{user.IsBlocked},{user.IsActive},{user.Rating},{user.CreatedAt:yyyy-MM-dd}\n";
                }

                string fileName = $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                File.WriteAllText(filePath, csv, Encoding.UTF8);

                MessageBox.Show($"Данные экспортированы в файл:\n{filePath}",
                    "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AdminWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}