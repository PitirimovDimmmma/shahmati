using System;
using System.Collections.Generic;

namespace shahmati.Models.Admin
{
    // Статистика для админ-панели
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int ActiveGames { get; set; }
        public int BlockedUsers { get; set; }
        public int Admins { get; set; }
        public int CreatedToday { get; set; }
    }

    // Пользователь для админ-панели
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Client";
        public bool IsBlocked { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }

        // Профиль пользователя (дополнительно)
        public string Nickname { get; set; } = string.Empty;
        public int Rating { get; set; }
    }

    // Запрос на обновление роли
    public class UpdateUserRoleRequest
    {
        public string Role { get; set; } = "Client"; // "Admin" или "Client"
        public bool? IsBlocked { get; set; }
        public bool? IsActive { get; set; }
    }
}