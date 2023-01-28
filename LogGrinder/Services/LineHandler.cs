using System;
using System.Text.Encodings.Web;
using System.Text.Json;

using LogGrinder.Interfaces;

using LogGrinder.Models;

namespace LogGrinder.Services
{
    /// <inheritdoc />
    public class LineHandler : ILineHandler
    {
        private const string _newLine = "\\r\\n";

        private const string _fn = "Название файла: ";
        private const string _n = "Номер строки: ";
        private const string _t = "Время (t): ";
        private const string _l = "Уровень (l): ";
        private const string _mt = "Сообщение (mt): ";
        private const string _args = "Аргументы сообщения (args):\n";
        private const string _tr = "Информация о трассировке (tr): ";
        private const string _un = "Учетная запись пользователя (un): ";
        private const string _tn = "Код системы (tn): ";
        private const string _v = "Версия (v): ";
        private const string _lg = "Название логгера (lg): ";
        private const string _bn = "Название браузера (bn): ";
        private const string _bv = "Версия браузера (bv): ";
        private const string _tab = "Идентификатор вкладки браузера (tab): ";
        private const string _pid = "Идентификатор процесса (pid): ";
        private const string _ex = "Информация об исключении (ex):\n";
        private const string _span = "Время выполнения действий (span):\n";
        private const string _cust = "Прочие сведения (cust):\n";
        private const string _raw = "Непреобразованная json строка:\n";


        /// <inheritdoc />
        public string ProcessSelectedLine(LogModel logModel)
        {
            if (logModel is null)
                return string.Empty;

            var logMessage = ProcessAttribute(_fn, logModel.FileName, false)
                           + ProcessAttribute(_n, logModel.Id, false)
                           + Environment.NewLine
                           + ProcessAttribute(_l, logModel.l, false)
                           + ProcessAttribute(_t, logModel.t, false)
                           + ProcessAttribute(_tr, logModel.tr, false)
                           + ProcessAttribute(_un, logModel.un, false)
                           + Environment.NewLine
                           + ProcessAttribute(_mt, logModel.mt, false)
                           + Environment.NewLine
                           + ProcessAttribute(_ex, logModel.ex, true)
                           + ProcessAttribute(_args, logModel.args, true)
                           + Environment.NewLine
                           + ProcessAttribute(_tn, logModel.tn, false)
                           + ProcessAttribute(_v, logModel.v, false)
                           + ProcessAttribute(_lg, logModel.lg, false)
                           + ProcessAttribute(_pid, logModel.pid, false)
                           + Environment.NewLine
                           + ProcessAttribute(_bn, logModel.bn, false)
                           + ProcessAttribute(_bv, logModel.bv, false)
                           + ProcessAttribute(_tab, logModel.tab, false)
                           + Environment.NewLine
                           + ProcessAttribute(_cust, logModel.cust, true)
                           + ProcessAttribute(_span, logModel.span, true)
                           + Environment.NewLine
                           + ProcessAttribute(_raw, logModel.RawLine, true);

            return logMessage;
        }

        /// <summary>
        /// Переформатирование json-строки в удобочитаемую структуру
        /// </summary>
        /// <param name="line">Входная json-строка</param>
        /// <returns>Отформатированная json-строка</returns>
        private string JsonBeautifier(string line)
        {
            using var jDoc = JsonDocument.Parse(line);
            var logMessage = JsonSerializer.Serialize(jDoc, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            });
            logMessage = logMessage.Replace(_newLine, Environment.NewLine);
            return logMessage;
        }

        /// <summary>
        /// Обработка атрибута лога
        /// </summary>
        /// <param name="attributePrefix">Ключ атрибута</param>
        /// <param name="attribute">Значение атрибута</param>
        /// <param name="useBeautifier">Необходимость преобразования в читаемую json-форматированную строку</param>
        /// <returns>Полученная строка</returns>
        private string ProcessAttribute(string attributePrefix, string? attribute, bool useBeautifier)
        {
            if (!string.IsNullOrEmpty(attribute))
                return attributePrefix + (useBeautifier ? (JsonBeautifier(attribute) + Environment.NewLine) : attribute) + Environment.NewLine;
            else return string.Empty;
        }

        /// <summary>
        /// Обработка атрибута лога
        /// </summary>
        /// <param name="attributePrefix">Ключ атрибута</param>
        /// <param name="attribute">Объект, содержащий значение атрибута</param>
        /// <param name="useBeautifier">Необходимость преобразования в читаемую json-форматированную строку</param>
        /// <returns>Полученная строка</returns>
        private string ProcessAttribute(string attributePrefix, object attribute, bool useBeautifier) => attribute != null ? ProcessAttribute(attributePrefix, attribute.ToString(), useBeautifier) : string.Empty;
    }
}
