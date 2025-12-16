using System;
using System.Collections.Generic;

namespace shahmati.Models
{
    // DTO для видов тренировок
    public class TrainingTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Difficulty { get; set; }
        public string Category { get; set; }
        public int MaxTime { get; set; }
        public int MaxMoves { get; set; }
    }

    // DTO для тренировочных позиций
    public class TrainingPositionDto
    {
        public int Id { get; set; }
        public int TrainingTypeId { get; set; }
        public string Fen { get; set; }
        public string Solution { get; set; }
        public string Hint { get; set; }
        public string Difficulty { get; set; }
        public string Theme { get; set; }
        public int Rating { get; set; }
        public List<string> SolutionMoves { get; set; } = new();
    }

    // DTO для прогресса в тренировках
    public class TrainingProgressDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TrainingTypeId { get; set; }
        public bool Completed { get; set; }
        public int Score { get; set; }
        public int TimeSpent { get; set; }
        public int Mistakes { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public TrainingTypeDto TrainingType { get; set; }
    }

    // Запрос для начала тренировки
    public class StartTrainingRequest
    {
        public int UserId { get; set; }
        public int TrainingTypeId { get; set; }
    }

    // Запрос для завершения тренировки
    public class CompleteTrainingRequest
    {
        public int UserId { get; set; }
        public int TrainingTypeId { get; set; }
        public int Score { get; set; }
        public int TimeSpent { get; set; }
        public int Mistakes { get; set; }
        public bool Completed { get; set; }
    }

    // Статистика тренировок
    public class TrainingStatsDto
    {
        public int TotalTrainings { get; set; }
        public int CompletedTrainings { get; set; }
        public int TotalScore { get; set; }
        public double AverageScore { get; set; }
        public int BestScore { get; set; }
        public int TotalTimeSpent { get; set; } // в секундах
        public string FavoriteCategory { get; set; }
        public List<CategoryStatsDto> CategoryStats { get; set; } = new();
    }

    public class CategoryStatsDto
    {
        public string Category { get; set; }
        public int CompletedCount { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
    }
}