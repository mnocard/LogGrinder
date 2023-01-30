using System.Collections.Generic;
using System.Threading;

using LogGrinder.Models;

namespace LogGrinder.Interfaces
{
    /// <summary>
    /// Сервис обработки файлов
    /// </summary>
    public interface IFileHandler
    {
        /// <summary>
        /// Извлечение из файла логов всех данных или только новых данных, если полное извлечение было произведено ранее, 
        /// и конвертация в формат отображения для ViewModel
        /// </summary>
        /// <param name="filePath">Расположение файла логов</param>
        /// <returns>Список моделей логов, предназначенная для отображения данных</returns>
        IAsyncEnumerable<LogModel> ConvertFileToView(string filePath, CancellationToken _token = default);
        /// <summary>
        /// Заполнить дополнительные атрибуты
        /// </summary>
        /// <param name="model">Модель строки лога, в которую необходимо внести дополнительные атрибуты</param>
        /// <param name="jsonString">Необработанная json-строка</param>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        void AddCustomAttributes(ref LogModel model, string jsonString, string filePath);
    }
}
