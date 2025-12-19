using System;
using System.Collections.Generic;

namespace shahmati.Models
{
    // DTO для аутентификации
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // DTO для пользователей
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserProfileDto
    {
        public string Nickname { get; set; }
        public string PhotoPath { get; set; }
        public int Rating { get; set; }
    }

    public class UserWithProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // ДОБАВЬТЕ ЭТИ СВОЙСТВА:
        public string Role { get; set; } = "Client";
        public bool IsBlocked { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }

        public UserProfileDto Profile { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string Nickname { get; set; }
        public string PhotoPath { get; set; }
    }

    // DTO для игр
    public class GameDto
    {
        public int Id { get; set; }
        public UserDto WhitePlayer { get; set; }
        public UserDto BlackPlayer { get; set; }
        public string GameState { get; set; }
        public string CurrentPlayer { get; set; }
        public string GameMode { get; set; }
        public string Difficulty { get; set; }
        public bool IsFinished { get; set; }
        public string Result { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public List<MoveDto> Moves { get; set; } = new();
    }

    // ЕДИНСТВЕННОЕ определение CreateGameDto
    public class CreateGameDto
    {
        public int WhitePlayerId { get; set; }
        public int? BlackPlayerId { get; set; }  // Используем nullable для гибкости
        public string GameMode { get; set; } = "HumanVsHuman";
        public string Difficulty { get; set; } = "Medium";
    }

    public class MoveDto
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int MoveNumber { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Piece { get; set; }
        public string CapturedPiece { get; set; }
        public string Notation { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MakeMoveRequest
    {
        public int PlayerId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Piece { get; set; }
        public string CapturedPiece { get; set; }
    }

    public class FinishGameRequest
    {
        public string Result { get; set; }
    }

    // DTO для сохраненных игр
    public class SavedGameDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameData { get; set; }
        public string GameName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SavedGameDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameData { get; set; }
        public string GameName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveGameRequest
    {
        public int UserId { get; set; }
        public string GameData { get; set; }
        public string GameName { get; set; }
    }

    public class UpdateSavedGameRequest
    {
        public string GameName { get; set; }
        public string GameData { get; set; }
    }

    // DTO для статистики
    public class GameStatsDto
    {
        public int TotalGames { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int CurrentRating { get; set; }
        public int HighestRating { get; set; }
        public double WinPercentage { get; set; }
    }

    public class PlayerStatsDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Rating { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
    }

    // DTO для обновления пользователя
    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string Nickname { get; set; }
        public string PhotoPath { get; set; }
    }

  

    // Класс для запроса обновления рейтинга
    public class UpdateRatingRequest
    {
        public int RatingChange { get; set; }
    }

    // В Models/FinishGameDto.cs добавьте UserId:
    public class FinishGameDto
    {
        public int GameId { get; set; }
        public string Result { get; set; } = string.Empty;
        public int RatingChange { get; set; }
        public int UserId { get; set; } // Добавьте это свойство
    }

    // Модель для обновления рейтинга
    public class UpdateRatingDto
    {
        public int UserId { get; set; }
        public int RatingChange { get; set; }
        public int GameId { get; set; }
        public string Reason { get; set; }
    }
}