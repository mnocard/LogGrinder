using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LogGrinder.Models;

namespace LogGrinder.Interfaces
{
    /// <summary>
    /// Сервис поиска
    /// </summary>
    public interface ISearcher
    {
        /// <summary>
        /// Поиск указанной строки в списке строк логов
        /// </summary>
        /// <param name="models">Список строк логов, в которых осуществляется поиск</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns>Результаты поиска</returns>
        Task<SearchResult> SearchInOpenedFile(IEnumerable<LogModel> models, SearchModel option, CancellationToken token = default);
        /// <summary>
        /// Поиск указанной строки в указанном файле лога
        /// </summary>
        /// <param name="filePath">Файл лога, в которых осуществляется поиск</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns>Результаты поиска</returns>
        Task<SearchResult> SearchInFile(string filePath, SearchModel option, CancellationToken token = default);
    }
}
