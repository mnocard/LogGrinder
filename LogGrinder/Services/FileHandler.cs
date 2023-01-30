using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using LogGrinder.Interfaces;

using LogGrinder.Models;

using Serilog;

namespace LogGrinder.Services
{
    /// <inheritdoc />
    public class FileHandler : IFileHandler
    {
        private readonly ILogger _log = Log.ForContext<FileHandler>();
        private readonly IFileManager _fileManager;

        public FileHandler(IFileManager fileManager) => _fileManager = fileManager;

        /// <inheritdoc />
        public async Task<List<LogModel>> ConvertFileToView(string filePath, CancellationToken token = default)
        {
            var rawLogLines = new List<LogModel>();
            string? jsonString;
            bool isSameFile = _fileManager.FileName == filePath;

            if (string.IsNullOrEmpty(filePath))
                return rawLogLines;

            try
            {
                var fileReadingOptions = new FileStreamOptions
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Share = FileShare.ReadWrite,
                };

                using var file = _fileManager.StreamReader(filePath, fileReadingOptions);

                // Повторная обработка того же самого файла
                if (isSameFile)
                {
                    var newData = file.BaseStream.Length - _fileManager.FileSize;
                    // Если размер файла увеличился, то устанавливаем позицию чтения на количество добавленных байт с конца.
                    // В общем, обрабатываем, только новые данные
                    // Если размер файла уменьшился, то запоминаем новый размер и прекращаем обработку файла
                    if (newData > 0)
                        file.BaseStream.Seek(-newData, SeekOrigin.End);
                    else
                    {
                        _fileManager.FileSize = file.BaseStream.Length;
                        return rawLogLines;
                    }
                }
                else
                    _fileManager.LineNumber = 0;

                var counter = _fileManager.LineNumber;
                while ((jsonString = file.ReadLine()) != null)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
                    using MemoryStream openStream = new(bytes);

                    var model = await JsonSerializer.DeserializeAsync<LogModel>(openStream);

                    if (model != null)
                    {
                        counter++;
                        model.Id = counter;

                        AddCustomAttributes(ref model, jsonString, filePath);

                        rawLogLines.Add(model);
                    }
                }

                _fileManager.LineNumber = counter;
                _fileManager.FileSize = file.BaseStream.Length;
                _fileManager.FileName = filePath;
                return rawLogLines;
            }
            catch (OperationCanceledException)
            {
                return rawLogLines;
            }
            catch (Exception ex)
            {
                _log.Error(ex, LogUnhandledError);
                throw;
            }
        }

        /// <inheritdoc />
        public void AddCustomAttributes(ref LogModel model, string jsonString, string filePath)
        {
            if (string.IsNullOrEmpty(model.Other))
            {
                if (!string.IsNullOrEmpty(model.mt))
                    model.Other = model.mt;
                else if (model.ex != null)
                    model.Other = _ex.ToString() + model.ex;
                else if (model.cust != null)
                    model.Other = _cust.ToString() + model.cust;
                else if (model.span != null)
                    model.Other = _span.ToString() + model.span;
                else if (model.args != null)
                    model.Other = _args.ToString() + model.args;
            }

            if (!string.IsNullOrEmpty(jsonString))
                model.RawLine = jsonString;

            if (!string.IsNullOrEmpty(filePath))
                model.FileName = Path.GetFileNameWithoutExtension(filePath);
        }

        #region Константы
        private const string LogUnhandledError = "Непредвиденная ошибка при попытке обработать файл.";
        private const string _args = "args: ";
        private const string _ex = "ex: ";
        private const string _cust = "cust: ";
        private const string _span = "span: ";
        #endregion
    }
}
