using System;

namespace shahmati.Models
{
    public class GameHistoryDto
    {
        public int Id { get; set; }
        public int WhitePlayerId { get; set; }
        public string WhitePlayerUsername { get; set; } = string.Empty;
        public int? BlackPlayerId { get; set; }
        public string BlackPlayerUsername { get; set; } = string.Empty;
        public string GameMode { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int DurationMinutes { get; set; }
        public int MoveCount { get; set; }

        // Для удобства отображения
        public string ResultForUser { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public bool UserPlayedWhite { get; set; }

        // Методы для форматирования
        public string GetFormattedDate() => CreatedAt.ToString("dd.MM.yyyy HH:mm");

        public string GetFormattedDuration()
        {
            if (DurationMinutes <= 0) return "В процессе";

            if (DurationMinutes < 60)
                return $"{DurationMinutes} мин";

            int hours = DurationMinutes / 60;
            int minutes = DurationMinutes % 60;
            return $"{hours} ч {minutes} мин";
        }

        public string GetGameModeDisplay()
        {
            return GameMode switch
            {
                "HumanVsHuman" => "Человек vs Человек",
                "HumanVsAI" => "Человек vs ИИ",
                "AIvsAI" => "ИИ vs ИИ",
                _ => GameMode
            };
        }
    }
}