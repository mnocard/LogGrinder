using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LogGrinder.Interfaces;

using LogGrinder.Models;

namespace LogGrinder.Services
{
    internal class SearchLineHandler : ISearchLineHandler
    {
        #region Constants
        private const string _dollar = "$";
        private const string _splitLinePattern = "([$][\\S]+=)";
        private const string _checkLinePattern = "[$].+(=\"+|=-\"+).+[\"]$";
        private const string _asterisk = "*";
        private const string _doubleAsterisk = "**";
        private const string _replacementMarks = "~!@#$%^&*()_+";
        private const string _anyChars = ".+";
        private const char _equals = '=';
        private const char _minus = '-';
        private const char _openBracket = '[';
        private const char _closedBracket = ']';
        private const char _startWith = '^';
        private const char _endWith = '$';

        private const string _dateBegin = "db";
        private const string _dateEnd = "de";
        private const string _linesCountAfter = "lca";
        private const string _linesCountBefore = "lcb";
        private const string _linesNumberStart = "lns";
        private const string _linesNumberEnd = "lne";
        #endregion

        /// <inheritdoc />
        public SearchModel ProcessSearchLine(string line)
        {
            var option = new SearchModel();
            line = line.Trim();

            CheckLine(line);

            var lines = SplitLine(line);
            foreach (var item in lines)
                option = SyncAttributes(item, option);

            return option;
        }

        /// <summary>
        /// Синхронизируем объединенные строку с атрибутами и искомым текстом с настройками поиска
        /// </summary>
        /// <param name="line">Объединенная строка с атрибутами и искомым текстом</param>
        /// <param name="option">Настройки поиска</param>
        /// <returns>Настройки поиска</returns>
        /// <exception cref="ArgumentException">Возникает, если пользователь указал в строке поиска некорректный атрибут</exception>
        private SearchModel SyncAttributes(string line, SearchModel option)
        {
            line = line.Trim();

            // Добавляем атрибуты в список атрибутов, попутно отрезая от начальной строки найденные атрибуты
            if (line.StartsWith(_dollar))
            {
                line = line.Substring(_dollar.Length);
                var separatedLine = line.Split(_equals);
                var attributesList = separatedLine[0].Split(_dollar);
                var condition = !separatedLine[1].StartsWith(_minus);
                var searchLine = separatedLine[1];

                // Если в начале стоит минус, то убираем его и первую кавычку.
                searchLine = condition ? searchLine.Substring(1) : searchLine.Substring(2);
                // Потом убираем последнюю кавычку
                searchLine = searchLine.Remove(searchLine.Length - 1);

                foreach (var item in attributesList)
                    switch (item)
                    {
                        case _dateBegin:
                            option.DateBegin = searchLine;
                            break;
                        case _dateEnd:
                            option.DateEnd = searchLine;
                            break;
                        case _linesNumberStart:
                            option.LineNumberStart = int.Parse(searchLine);
                            break;
                        case _linesNumberEnd:
                            option.LineNumberEnd = int.Parse(searchLine);
                            break;
                        case _linesCountBefore:
                            option.LinesCountBefore = int.Parse(searchLine);
                            break;
                        case _linesCountAfter:
                            option.LinesCountAfter = int.Parse(searchLine);
                            break;
                        default:
                            option.Attributes.Add(new SearchModel.Attribute
                            {
                                Name = item,
                                Condition = condition,
                                SearchLinePattern = ConvertSearchLineToPattern(searchLine),
                            });
                            break;
                    }
            }

            return option;
        }

        /// <summary>
        /// Преобразование строки поиска в паттерн для использования в Regex
        /// </summary>
        /// <param name="line">Строка поиска</param>
        /// <returns>Паттерн</returns>
        private string ConvertSearchLineToPattern(string line)
        {
            var isChanged = false;

            if (line.Contains(_doubleAsterisk))
            {
                line = line.Replace(_doubleAsterisk, _replacementMarks);
                isChanged = true;
            }

            var lines = line.Split(_asterisk);

            if (isChanged)
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i].Contains(_replacementMarks))
                        lines[i] = lines[i].Replace(_replacementMarks, _asterisk);

            var pattern = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    if (i == 0)
                        pattern += _startWith;

                    foreach (var letter in lines[i])
                    {
                        pattern += _openBracket;
                        pattern += letter;
                        pattern += _closedBracket;
                    }

                    if (i == lines.Length - 1)
                        pattern += _endWith;
                    else
                        pattern += _anyChars;
                }
                else if (!pattern.EndsWith(_anyChars))
                    pattern += _anyChars;
            }

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Ошибка при парсинге строки поиска.");

            return pattern;
        }

        /// <summary>
        /// Разделение аргументов на составные части по атрибутам
        /// </summary>
        /// <param name="line">Строка аргументов</param>
        /// <returns>Список аргументов, разделенных по атрибутам</returns>
        private List<string> SplitLine(string line)
        {
            var result = new List<string>();

            // Разделяем строки на массив по шаблону. В итогу получается массив, в котором чередуются имена атрибутов и искомая строка
            // Если строка в массиве начинается с доллара, то добавляем в результат. В противном случае, строка - искомый текст, значит
            // добавляем к предыдущему результату поиска.

            var lines = Regex.Split(line, _splitLinePattern).Where(s => !string.IsNullOrEmpty(s));

            foreach (string separatedLine in lines)
            {
                if (separatedLine.StartsWith(_dollar))
                    result.Add(separatedLine);
                else
                    result[result.IndexOf(result.Last())] += separatedLine;
            }

            return result;
        }

        /// <summary>
        /// Проверка строки аргументов на корректность. При наличии ошибок в строке аргументов выбрасывает ошибку ArgumentException.
        /// </summary>
        /// <param name="line">Строка аргументов</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                throw new ArgumentException("Строка пустая.");

            if (!Regex.IsMatch(line, _checkLinePattern))
                throw new ArgumentException("Строка не соответствует шаблону.");

            var quotationMarksCount = line.Count(l => l == '"');
            if (quotationMarksCount % 2 != 0)
                throw new ArgumentException("Строка содержит неэкраннированную двойную кавычку.");
        }
    }
}
