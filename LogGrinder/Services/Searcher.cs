using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using LogGrinder.Interfaces;

using LogGrinder.Models;

namespace LogGrinder.Services
{
    /// <inheritdoc />
    public class Searcher : ISearcher
    {
        private readonly IFileManager _fileManager;
        private readonly IFileHandler _fileHandler;
        private readonly ISearchLineHandler _searchLineHandler;

        public Searcher(ISearchLineHandler searchLineHandler, IFileHandler fileHandler, IFileManager fileManager)
        {
            _searchLineHandler = searchLineHandler;
            _fileHandler = fileHandler;
            _fileManager = fileManager;
        }

        /// <inheritdoc />
        public async Task<SearchResult> SearchInOpenedFile(IEnumerable<LogModel> models, SearchModel option, CancellationToken token = default)
        {
            var result = new SearchResult();
            var linesBefore = new Queue<LogModel>();
            var linesAfter = new Queue<LogModel>();
            var startCollectLinesAfter = false;

            if (models == null || !models.Any() || option == null)
                return result;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var model in models)
                    {
                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        SearchWithNearesLines(option, linesBefore, linesAfter, result, ref startCollectLinesAfter, model);
                    }

                    AddAfterQueue(option.LinesCountAfter, result, linesAfter);
                });
            }
            catch (OperationCanceledException)
            {
                AddAfterQueue(option.LinesCountAfter, result, linesAfter);

                throw new CancelWithResultException(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<SearchResult> SearchInFile(string filePath, SearchModel option, CancellationToken token = default)
        {
            var result = new SearchResult();
            var linesBefore = new Queue<LogModel>();
            var linesAfter = new Queue<LogModel>();
            var startCollectLinesAfter = false;
            var counter = 0;
            string? jsonString;

            if (string.IsNullOrEmpty(filePath))
                return result;

            var fileReadingOptionns = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite,
            };

            try
            {
                using var file = _fileManager.StreamReader(filePath, fileReadingOptionns);
                while ((jsonString = file.ReadLine()) != null)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
                    using MemoryStream openStream = new(bytes);

                    var model = await JsonSerializer.DeserializeAsync<LogModel>(openStream, cancellationToken: token);
                    if (model == null)
                        continue;

                    counter++;
                    model.Id = counter;
                    SearchWithNearesLines(option, linesBefore, linesAfter, result, ref startCollectLinesAfter, model);

                    _fileHandler.AddCustomAttributes(ref model, jsonString, filePath);
                }

                AddAfterQueue(option.LinesCountAfter, result, linesAfter);
            }
            catch (OperationCanceledException)
            {
                AddAfterQueue(option.LinesCountAfter, result, linesAfter);

                throw new CancelWithResultException(result);
            }

            return result;
        }

        /// <summary>
        /// Если ПОСТочередь не заполнилась, то всё равно надо слить остатки в результаты
        /// </summary>
        /// <param name="linesCountAfter"></param>
        /// <param name="result"></param>
        /// <param name="linesAfter"></param>
        private void AddAfterQueue(int linesCountAfter, SearchResult result, Queue<LogModel> linesAfter)
        {
            if (linesCountAfter > 0 && linesAfter.Any())
                result.ResultsWithNearestLines.AddRange(linesAfter);

            result.ResultsWithNearestLines = result.ResultsWithNearestLines.Distinct().ToList();
        }

        /// <summary>
        /// Поиск с учетом ближайших строк
        /// </summary>
        /// <param name="option">Опция поиска, содержащая искомую строку и другие настройки</param>
        /// <param name="linesBefore">Список строк ДО искомой строки</param>
        /// <param name="linesAfter">Список строк ПОСЛЕ искомой строки</param>
        /// <param name="result">Результат поиска</param>
        /// <param name="startCollectLinesAfter">Необходимость собирать строки после найденной строки</param>
        /// <param name="model">Строка лога, в которой происходит поиск</param>
        private void SearchWithNearesLines(
            SearchModel option,
            Queue<LogModel> linesBefore,
            Queue<LogModel> linesAfter,
            SearchResult result,
            ref bool startCollectLinesAfter,
            LogModel model)
        {
            // Очень сложный поиск со сбором строк ДО и ПОСЛЕ найденного совпадения, поэтому много комментариев
            // Во-первых делаем пометку, найдено ли совпадение в текущей строке лога
            bool isFound;
            // Если строка начинается с символа доллара, то начинаем парсить строку в Опцию поиска, а затем
            // начинаем поиск по атрибутам независимо от других свойств.
            // Иначе, проводим поиск с учетом свойств Опции поиска.
            if (!string.IsNullOrEmpty(option.SearchLine) && option.SearchLine.StartsWith(_dollar))
            {
                option = _searchLineHandler.ProcessSearchLine(option.SearchLine);
                isFound = AdvancedSearchInModel(model, option);
            }
            else
                isFound = SearchInModel(model, option);

            // если не найдено и задано количество строк, которые надо загрузить ДО найденного результата, то добавляем в ПРЕочередь
            if (!isFound && option.LinesCountBefore > 0)
            {
                linesBefore.Enqueue(model);
                // если ПРЕочередь превысила допустимый размер, то начинаем удалять старые значения
                if (linesBefore.Count > option.LinesCountBefore)
                    linesBefore.Dequeue();
            }

            // если найдено совпадение
            if (isFound)
            {
                // cливаем ПРЕочередь в конечный список, а потом очищаем ПРЕочередь
                if (option.LinesCountBefore > 0 && linesBefore.Any())
                {
                    result.ResultsWithNearestLines.AddRange(linesBefore);
                    linesBefore.Clear();
                }

                // добавляем само найденное значение в конечный список
                result.ResultsWithNearestLines.Add(model);
                result.ClearResults.Add(model);

                // если ранее ПОСТочередь собиралась и найденое ещё одно совпадение, которое попадает в ПОСТочередь,
                // то сливаем ПОСТочередь в конечный список и обнуляем
                if (startCollectLinesAfter && linesAfter.Any())
                {
                    result.ResultsWithNearestLines.AddRange(linesAfter);
                    linesAfter.Clear();
                }
                else
                    // если не собирали раньше ПОСТочередь, то начинаем собирать
                    startCollectLinesAfter = true;
            }

            // если надо собирать значения в ПОСТочередь
            if (startCollectLinesAfter)
            {
                linesAfter.Enqueue(model);
                // если ПОСТочередь заполнилась, то сливаем её в конечный список и устанавливаем значение,
                // что не надо больше собирать ПОСТочередь
                if (linesAfter.Count > option.LinesCountAfter)
                {
                    result.ResultsWithNearestLines.AddRange(linesAfter);
                    linesAfter.Clear();
                    startCollectLinesAfter = false;
                }
            }
        }

        /// <summary>
        /// Поиск указанной строки в определенной строке лога
        /// </summary>
        /// <param name="model">Модель, содержащая строку лога</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns>Результат поиска</returns>
        private bool SearchInModel(LogModel model, SearchModel option)
        {
            // Если лог содержит текст, который надо исключить, то сразу возвращаем false
            if (!string.IsNullOrWhiteSpace(option.ExcludeLine)
                && (SearchInAttribute(model.mt, option.ExcludeLine)
                || SearchInAttribute(model.ex, option.ExcludeLine)
                || SearchInAttribute(model.cust, option.ExcludeLine)
                || SearchInAttribute(model.args, option.ExcludeLine)
                || SearchInAttribute(model.span, option.ExcludeLine)
                || SearchInAttribute(model.l, option.ExcludeLine)
                || SearchInAttribute(model.t, option.ExcludeLine)
                || SearchInAttribute(model.tr, option.ExcludeLine)
                || SearchInAttribute(model.un, option.ExcludeLine)
                || SearchInAttribute(model.lg, option.ExcludeLine)
                || SearchInAttribute(model.bn, option.ExcludeLine)
                || SearchInAttribute(model.tab, option.ExcludeLine)
                || SearchInAttribute(model.bv, option.ExcludeLine)
                || SearchInAttribute(model.v, option.ExcludeLine)
                || SearchInAttribute(model.tn, option.ExcludeLine)
                || SearchInAttribute(model.pid, option.ExcludeLine)))
                return false;

            var checkingResult = CheckLinesAndDates(string.IsNullOrEmpty(option.SearchLine), model, option);
            if (checkingResult != null)
                return checkingResult == true;

            return !string.IsNullOrEmpty(option.SearchLine)
                && (SearchInAttribute(model.mt, option.SearchLine)
                || SearchInAttribute(model.ex, option.SearchLine)
                || SearchInAttribute(model.cust, option.SearchLine)
                || SearchInAttribute(model.args, option.SearchLine)
                || SearchInAttribute(model.span, option.SearchLine)
                || SearchInAttribute(model.l, option.SearchLine)
                || SearchInAttribute(model.t, option.SearchLine)
                || SearchInAttribute(model.tr, option.SearchLine)
                || SearchInAttribute(model.un, option.SearchLine)
                || SearchInAttribute(model.lg, option.SearchLine)
                || SearchInAttribute(model.bn, option.SearchLine)
                || SearchInAttribute(model.tab, option.SearchLine)
                || SearchInAttribute(model.bv, option.SearchLine)
                || SearchInAttribute(model.v, option.SearchLine)
                || SearchInAttribute(model.tn, option.SearchLine)
                || SearchInAttribute(model.pid, option.SearchLine));
        }

        /// <summary>
        /// Расширенный поиск с использованием атрибутов
        /// </summary>
        /// <param name="model">Модель, содержащая строку лога, в которой осуществляется поиск.</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns>Результат поиска. True, если строка лога удовлетворяет условиям поиска. Иначе, false.</returns>
        private bool AdvancedSearchInModel(LogModel model, SearchModel option)
        {
            var propertyList = model.GetType().GetProperties();

            var isAttributesEmpty = !option.Attributes.Any();
            var checkingResult = CheckLinesAndDates(isAttributesEmpty, model, option);
            if (checkingResult == false)
                return false;
            else if (checkingResult == true && isAttributesEmpty)
                return true;

            foreach (var item in option.Attributes)
            {
                var value = string.Empty;

                // Поиск по всем атрибутам
                if (item.Name == _any)
                {
                    var results = new List<bool>();
                    foreach (var property in propertyList.Select(p => p.GetValue(model)?.ToString()))
                        results.Add(CheckConditions(property, item));

                    return results.Contains(true);
                }

                value = propertyList.FirstOrDefault(p => p.Name == item.Name)?.GetValue(model)?.ToString();

                // Поиск по конкретным атрибутам
                if (option.Attributes.Count(a => a.Name == item.Name) > 1)
                {
                    var results = new List<bool>();
                    foreach (var attribute in option.Attributes.Where(a => a.Name == item.Name))
                        results.Add(CheckConditions(value, attribute));

                    return results.Contains(true);
                }
                else
                    return CheckConditions(value, item);
            }

            throw new Exception(LogUnhandledError);
        }

        /// <summary>
        /// Поиск по номерам строк и датам. Если в опциях поиска указаны номера строк и/или даты, то осуществляет поиск указанного текста в них. Если текст для поиска не указан, возвращает все строки, удовлетворяющие введенным номерам/датам.
        /// </summary>
        /// <param name="isAttributesEmpty">True, если строка поиска не пустая.</param>
        /// <param name="model">Модель, содержащая строку лога, в которой осуществляется поиск.</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns><Результат поиска. True, если строка лога удовлетворяет условиям поиска. Иначе, false./returns>
        private bool? CheckLinesAndDates(bool isAttributesEmpty, LogModel model, SearchModel option)
        {
            bool? result = null;

            // Если указан номер начальной строки поиска, а текущий номер текущей строки меньше, то сразу возвращаем false
            if (option.LineNumberStart > 0 && model.Id < option.LineNumberStart)
                result = false;

            // Если указан номер конечной строки поиска, а текущий номер текущей строки больше, то сразу возвращаем false
            else if (option.LineNumberEnd > 0 && model.Id > option.LineNumberEnd)
                result = false;

            // Если указано время начала поиска и время в логе меньше указанного, то сразу возвращаем false
            else if (!string.IsNullOrWhiteSpace(option.DateBegin) && string.Compare(model.t, option.DateBegin) < 0)
                result = false;

            // Если указано время окончания поиска и время в логе больше указанного, то сразу возвращаем false
            else if (!string.IsNullOrWhiteSpace(option.DateEnd) && string.Compare(model.t, option.DateEnd) > 0)
                result = false;

            // Если строка поиска пустая и не надо учитываять номера строк, но указано время начала поиска и время в логе меньше указанного или
            // указано время окончания поиска и время в логе больше указанного, то возвращаем true
            else if (isAttributesEmpty
                && SearchInDates(option.DateBegin, option.DateEnd, model.t!)
                && option.LineNumberStart <= 0
                && option.LineNumberEnd <= 0)
                result = true;

            // Если строка поиска пустая и не надо учитываять даты строк, но указан номер строки начала поиска и номер строки в логе больше или равен указанного
            // или указан номер строки окончания поиска и номер строки в логе меньше или равен указанного, то возвращаем true
            else if (isAttributesEmpty
                && SearchInLineNumbers(option.LineNumberStart, option.LineNumberEnd, model.Id)
                && string.IsNullOrEmpty(option.DateBegin)
                && string.IsNullOrEmpty(option.DateEnd))
                result = true;

            // Если строка пустая, но есть дата или номера поиска, то ищет совпадение в них, если совпадение найдено, то возвращает true
            else if (isAttributesEmpty
                && (!string.IsNullOrEmpty(option.DateBegin)
                || !string.IsNullOrEmpty(option.DateEnd)
                && (option.LineNumberStart > 0
                || option.LineNumberEnd > 0)
                && SearchInDates(option.DateBegin, option.DateEnd, model.t!)
                && SearchInLineNumbers(option.LineNumberStart, option.LineNumberEnd, model.Id)))
                result = true;

            return result;
        }

        /// <summary>
        /// Совпадение в текущеем атрибуте со значением атрибута из модели, содержащую строку лога, в которой осуществляется поиск.
        /// </summary>
        /// <param name="value">Значение атрибута в модели.</param>
        /// <param name="attribute">Атрибут поиска.</param>
        /// <returns>True, если в текущем логе найдено совпадение с искомой строкой.</returns>
        /// <exception cref="Exception">Ошибка обработки атрибута поиска.</exception>
        private bool CheckConditions(string value, SearchModel.Attribute attribute)
        {
            if (string.IsNullOrEmpty(value) && attribute.SearchLinePattern == _anyChars)
                return !attribute.Condition;
            else if (string.IsNullOrEmpty(value) && attribute.SearchLinePattern != _anyChars)
                return !attribute.Condition;
            else if (!string.IsNullOrEmpty(value) && attribute.SearchLinePattern == _anyChars)
                return attribute.Condition;
            else if (!string.IsNullOrEmpty(value) && attribute.SearchLinePattern != _anyChars)
                return attribute.Condition
                    ? Regex.IsMatch(value, attribute.SearchLinePattern)
                    : !Regex.IsMatch(value, attribute.SearchLinePattern);
            else
                throw new Exception(LogUnhandledError);
        }

        /// <summary>
        /// Искать совпадение в указанном промежутке дат
        /// </summary>
        /// <param name="dateBegin">Начало промежутка дат. null если не учитывать начало промежутка.</param>
        /// <param name="dateEnd">Окончание промежутка дат. null если не учитывать конец промежутка.</param>
        /// <param name="currentDate">Текущая дата строки.</param>
        /// <returns>Результат поиска. True, если дата текущей строки попадает в указанный промежуток.</returns>
        private bool SearchInDates(string dateBegin, string dateEnd, string currentDate)
        {
            if (string.IsNullOrWhiteSpace(currentDate)
                || (string.IsNullOrWhiteSpace(dateBegin)
                && string.IsNullOrWhiteSpace(dateEnd)))
                return false;

            if (!string.IsNullOrWhiteSpace(dateBegin)
                && string.IsNullOrWhiteSpace(dateEnd)
                && string.Compare(currentDate, dateBegin) >= 0)
                return true;

            if (!string.IsNullOrWhiteSpace(dateEnd)
                && string.IsNullOrWhiteSpace(dateBegin)
                && string.Compare(currentDate, dateEnd) <= 0)
                return true;

            if (!string.IsNullOrWhiteSpace(dateEnd)
                && !string.IsNullOrWhiteSpace(dateBegin)
                && string.Compare(currentDate, dateBegin) >= 0
                && string.Compare(currentDate, dateEnd) <= 0)
                return true;

            return false;
        }

        /// <summary>
        /// Искать совпадение в указанном промежутке номеров строк
        /// </summary>
        /// <param name="lineNumberBegin">Начало промежутка номеров. -1 если не учитывать начало промежутка.</param>
        /// <param name="lineNumberEnd">Окончание промежутка номеров. -1 если не учитывать конец промежутка.</param>
        /// <param name="currentLineNumber">Номер текущей строки.</param>
        /// <returns>Результат поиска. True, если номер текущей строки попадает в указанный промежуток.</returns>
        private bool SearchInLineNumbers(int lineNumberBegin, int lineNumberEnd, int currentLineNumber)
        {
            if (lineNumberBegin <= 0 && lineNumberEnd <= 0)
                return false;

            if (lineNumberBegin >= 0
                && lineNumberEnd <= 0
                && currentLineNumber >= lineNumberBegin)
                return true;

            if (lineNumberEnd >= 0
                && lineNumberBegin <= 0
                && currentLineNumber <= lineNumberEnd)
                return true;

            if (lineNumberBegin >= 0
                && lineNumberEnd >= 0
                && lineNumberEnd >= lineNumberBegin
                && currentLineNumber >= lineNumberBegin
                && currentLineNumber <= lineNumberEnd)
                return true;

            return false;
        }

        /// <summary>
        /// Поиск указанной строки в определенном сегменте лога
        /// </summary>
        /// <param name="attribute">Модель, содержащая строку лога</param>
        /// <param name="searchLine">Искомая строка</param>
        /// <param name="isEnabled">Настройки поиска</param>
        /// <returns>Результат поиска</returns>
        private bool SearchInAttribute(string attribute, string searchLine)
        {
            return (!string.IsNullOrEmpty(attribute)
                    && attribute.Length >= searchLine.Length
                    && attribute.Contains(searchLine, StringComparison.InvariantCultureIgnoreCase));
        }

        private bool SearchInAttribute(object attribute, string searchLine) => attribute != null && SearchInAttribute(attribute.ToString(), searchLine);

        #region Константы
        private const string LogUnhandledError = "Непредвиденная ошибка при попытке обработать файл.";
        private const char _dollar = '$';
        private const string _anyChars = ".+";

        private const string _any = "any";
        #endregion
    }
}
