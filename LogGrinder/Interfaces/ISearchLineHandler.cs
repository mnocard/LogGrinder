using LogGrinder.Models;

namespace LogGrinder.Interfaces
{
    /// <summary>
    /// Сервис обработки строки поиска в поиск по атрибутам
    /// </summary>
    public interface ISearchLineHandler
    {
        /// <summary>
        /// Парсинг строки поиска в модель поиска
        /// </summary>
        /// <param name="line">Строка поиска</param>
        /// <returns>Модель поиска</returns>
        SearchModel ProcessSearchLine(string line);
    }
}
