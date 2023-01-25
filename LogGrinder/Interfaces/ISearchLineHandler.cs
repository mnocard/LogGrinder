using LogGrinder.Models;

namespace LogGrinder.Interfaces
{
    internal interface ISearchLineHandler
    {
        /// <summary>
        /// Парсинг строки поиска в модель поиска
        /// </summary>
        /// <param name="line">Строка поиска</param>
        /// <returns>Модель поиска</returns>
        SearchModel ProcessSearchLine(string line);
    }
}
