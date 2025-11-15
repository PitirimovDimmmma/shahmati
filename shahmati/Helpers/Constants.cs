using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shahmati.Helpers
{
    public static class Constants
    {
        public const int BoardSize = 8;

        // Режимы игры
        public static readonly string[] GameModes =
        {
            "Человек vs Человек",
            "Человек vs Компьютер",
            "Компьютер vs Компьютер"
        };

        // Уровни сложности
        public static readonly string[] DifficultyLevels =
        {
            "Новичок",
            "Лёгкий",
            "Средний",
            "Сложный",
            "Эксперт"
        };
    }
}
