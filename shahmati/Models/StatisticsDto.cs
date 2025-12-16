using System;
using System.Collections.Generic;

namespace shahmati.Models
{
    // DTO для статистики с фильтрацией
    public class ExtendedGameStatsDto
    {
        public GameStatsDto Overall { get; set; } = new GameStatsDto();
        public GameStatsDto VsAI { get; set; } = new GameStatsDto();
        public GameStatsDto VsHuman { get; set; } = new GameStatsDto();
        public List<RatingHistoryDto> RatingHistory { get; set; } = new List<RatingHistoryDto>();
        public PerformanceMetricsDto Performance { get; set; } = new PerformanceMetricsDto();
    }

    public class PerformanceMetricsDto
    {
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public double AverageAccuracy { get; set; }
        public int BestRating { get; set; }
        public int WorstRating { get; set; }
        public Dictionary<string, int> GamesByDifficulty { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> GamesByColor { get; set; } = new Dictionary<string, int>();
    }

    public class PlayerRatingDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int Rank { get; set; }
        public string RankTitle { get; set; } = "Новичок";
    }

    public class FilterStatsRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string GameMode { get; set; } = "All"; // "AI", "Human", "All"
        public string Difficulty { get; set; } = "All";
        public string Color { get; set; } = "All"; // "White", "Black", "All"
    }

    public class RatingHistoryDto
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public int RatingChange { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Result { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
    }
}