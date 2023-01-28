using LogGrinder.Models;

namespace LogGrinder.Interfaces
{
    /// <summary>
    /// Сервис обработки логов
    /// </summary>
    public interface ILineHandler
    {
        /// <summary>
        /// Преобразование модели в человекочитаемое текстовое сообщение
        /// </summary>
        /// <param name="logModel">Модель логов, предназначенная для работы с базой данных</param>
        /// <returns>Обработанное сообщение</returns>
        string ProcessSelectedLine(LogModel logModel);
    }
}
