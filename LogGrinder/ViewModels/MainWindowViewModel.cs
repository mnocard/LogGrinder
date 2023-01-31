using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using LogGrinder.Core;
using LogGrinder.Interfaces;
using LogGrinder.Models;
using LogGrinder.Views;

using Serilog;

namespace LogGrinder.ViewModels
{
    internal class MainWindowViewModel : ViewModelCore
    {
        #region MainWindowViewModel : Конструктор
        public MainWindowViewModel(ILineHandler lineHandler,
                                   ISearcher searcher,
                                   IFileHandler fileHandler,
                                   IFileManager fileManager)
        {
            _lineHandler = lineHandler;
            _searcher = searcher;
            _fileHandler = fileHandler;
            _fileManager = fileManager;
        }
        #endregion

        #region Свойства

        #region ButtonIcons

        #region ButtonOpenFolderIcon : string - Иконка кнопки "Папка"

        /// <summary>Иконка кнопки "Папка"</summary>
        private string _ButtonOpenFolderIcon = FolderIcon;

        /// <summary>Иконка кнопки "Папка"</summary>
        public string ButtonOpenFolderIcon
        {
            get => _ButtonOpenFolderIcon;
            set => Set(ref _ButtonOpenFolderIcon, value);
        }

        #endregion

        #region ButtonAdvancedSearch : string - Иконка кнопки расширенный поиск

        /// <summary>Иконка кнопки расширенный поиск</summary>
        private string _ButtonAdvancedSearch = SearchIcon;

        /// <summary>Иконка кнопки расширенный поиск</summary>
        public string ButtonAdvancedSearch
        {
            get => _ButtonAdvancedSearch;
            set => Set(ref _ButtonAdvancedSearch, value);
        }

        #endregion

        #endregion

        #region Title : string - Заголовок

        /// <summary>Заголовок</summary>
        private string _Title = TitleLogSurfer;

        /// <summary>Заголовок</summary>
        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        #endregion

        #region Status : string - Сообщение в статусной строке

        /// <summary>Сообщение в статусной строке</summary>
        private string _Status = StatusHi;

        /// <summary>Сообщение в статусной строке</summary>
        public string Status
        {
            get => _Status;
            set => Set(ref _Status, value);
        }

        #endregion

        #region LogFileNames : ObservableCollection<string> - Список имен лог-файлов без путей расположения

        /// <summary>Список имен лог-файлов без путей расположения</summary>
        private ObservableCollection<string> _LogFileNames = new();

        /// <summary>Список имен лог-файлов без путей расположения</summary>
        public ObservableCollection<string> LogFileNames
        {
            get => _LogFileNames;
            set => Set(ref _LogFileNames, value);
        }

        #endregion

        #region CurrentLogFileItem : string - Текущий выбранный лог файл из списка имен лог-файлов

        /// <summary>Текущий выбранный лог файл</summary>
        private string _CurrentLogFileItem;

        /// <summary>Текущий выбранный лог файл</summary>
        public string CurrentLogFileItem
        {
            get => _CurrentLogFileItem;
            set
            {
                if (Set(ref _CurrentLogFileItem, value))
                {
                    ProcessLogFile(CurrentLogFileItem);
                    _IsFileOnView = true;
                }
            }
        }

        #endregion

        #region LogLines : ObservableCollection<LogModel> - Список строк выбранного лога

        /// <summary>Список строк выбранного лога</summary>
        private ObservableCollection<LogModel> _LogLines = new();

        /// <summary>Список строк выбранного лога</summary>
        public ObservableCollection<LogModel> LogLines
        {
            get => _LogLines;
            set => Set(ref _LogLines, value);
        }

        #endregion

        #region LogLinesBackup : List<LogModel> - Бэкап списка строк выбранного лога для обработанного файла

        /// <summary>Бэкап списка строк выбранного лога для обработанного файла</summary>
        private List<LogModel> _LogLinesBackup = new();

        /// <summary>Бэкап списка строк выбранного лога для обработанного файла</summary>
        public List<LogModel> LogLinesBackup
        {
            get => _LogLinesBackup;
            set => Set(ref _LogLinesBackup, value);
        }

        #endregion

        #region LogMessage : string - Подробное сообщение лога

        /// <summary>Подробное сообщение лога</summary>
        private string _LogMessage;

        /// <summary>Подробное сообщение лога</summary>
        public string LogMessage
        {
            get => _LogMessage;
            set => Set(ref _LogMessage, value);
        }

        #endregion

        #region SelectedLogLine : LogModel - Текущая выбранная строка лога

        /// <summary>Текущая выбранная строка лога</summary>
        private LogModel _SelectedLogLine;

        /// <summary>Текущая выбранная строка лога</summary>
        public LogModel SelectedLogLine
        {
            get => _SelectedLogLine;
            set
            {
                Set(ref _SelectedLogLine, value);
                LogMessage = _lineHandler.ProcessSelectedLine(SelectedLogLine);
            }
        }

        #endregion

        #region SearchModel : SearchModel - Настройка расширенного поиска

        /// <summary>Настройка расширенного поиска</summary>
        private SearchModel _SearchOption = new();

        /// <summary>Настройка расширенного поиска</summary>
        public SearchModel SearchOption
        {
            get => _SearchOption;
            set => Set(ref _SearchOption, value);
        }

        #endregion

        #region SearchResults : ObservableCollection<SearchResult> - Список результатов поиска в файлах

        /// <summary>Список названий файлов по результатам поиска в файлах</summary>
        private ObservableCollection<SearchResult> _SearchResults = new();

        /// <summary>Список названий файлов по результатам поиска в файлах</summary>
        public ObservableCollection<SearchResult> SearchResults
        {
            get => _SearchResults;
            set => Set(ref _SearchResults, value);
        }

        #endregion

        #region CurrentSearchResult : SearchResult - Текущий выбранный результат поиска

        /// <summary>Текущий выбранный результат поиска</summary>
        private SearchResult _CurrentSearchResult;

        /// <summary>Текущий выбранный результат поиска</summary>
        public SearchResult CurrentSearchResult
        {
            get => _CurrentSearchResult;
            set
            {
                _IsFileOnView = !Set(ref _CurrentSearchResult, value);

                if (_IsNeedToShowSearchResults
                    && _CurrentSearchResult != null
                    && _CurrentSearchResult.ClearResults.Count > 0)
                    CreateLogLinesUI(_CurrentSearchResult.ClearResults);
            }
        }

        #endregion

        #region UpdateButtonColor : string - Цвет кнопки обновления файла

        /// <summary>Цвет кнопки обновления файла</summary>
        private string _UpdateButtonColor = ColorOrange;

        /// <summary>Цвет кнопки обновления файла</summary>
        public string UpdateButtonColor
        {
            get => _UpdateButtonColor;
            set => Set(ref _UpdateButtonColor, value);
        }

        #endregion

        #endregion

        #region Команды

        #region OpenFile : Открыть лог файлы
        /// <summary>
        /// Открыть лог файлы
        /// </summary>
        /// <returns></returns>
        public async Task OpenFile()
        {
            if (ButtonOpenFolderIcon == StopIcon)
            {
                _TokenSource.Cancel();
                StatusChanging(StatusProcessingFileStopped);
                return;
            }

            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Title = ChooseLogFiles,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Extensions = new List<string> { LogExt },
                        Name = DlgJsonFilter,
                    },
                    new FileDialogFilter
                    {
                        Extensions = new List<string> { AllExt },
                        Name = DlgExtFilter,
                    }
                }
            };

            var paths = await dialog.ShowAsync(new Window());
            if (paths is null || !paths.Any())
                return;

            try
            {
                foreach (var path in paths)
                {
                    if (!_LogPaths.Contains(path))
                    {
                        _LogPaths.Add(path);
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        LogFileNames.Add(fileName);
                    }
                }

                if (paths.Length == 1)
                {
                    CurrentLogFileItem = Path.GetFileNameWithoutExtension(paths[0]);
                    _IsFileOnView = true;
                }
            }
            catch (Exception e) { StatusChanging(e); }
        }

        #endregion

        #region StartSearch : Поиск в текущем открытом файле
        /// <summary>
        /// Поиск в текущем открытом файле
        /// </summary>
        /// <returns></returns>
        public async Task StartSearch(object parameter)
        {
            if (!LogLinesBackup.Any())
            {
                StatusChanging(StatusChooseLogFile);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchOption.SearchLine)
                && !(parameter is string searchContext
                && searchContext == FromAdvancedSearch))
                return;

            ButtonAdvancedSearch = StopIcon;

            try
            {
                CurrentSearchResult = null;

                StatusChanging(StatusSearchingMatchWith + SearchOption.SearchLine);

                var searchResult = new SearchResult();

                try
                {
                    _TokenSource = new CancellationTokenSource();
                    var token = _TokenSource.Token;

                    searchResult = await _searcher.SearchInOpenedFile(LogLinesBackup, SearchOption, token);
                    searchResult.FileName = CurrentLogFileItem;
                    searchResult.FilePath = _LogPaths.FirstOrDefault(f => f.Contains(CurrentLogFileItem));

                }
                catch (OperationCanceledException) { }
                finally { _TokenSource.Dispose(); }

                if (!string.IsNullOrEmpty(SearchOption.SearchLine))
                    searchResult.SearchString = SearchOption.SearchLine.Length > 100
                        ? SearchOption.SearchLine[..100]
                        : SearchOption.SearchLine;
                else
                    searchResult.SearchString = AdvancedSearch;

                if (searchResult.ClearResults.Count <= 0)
                {
                    StatusChanging(StatusNotFound);
                    return;
                }

                SearchResults.Add(searchResult);

                _IsNeedToShowSearchResults = false;
                CurrentSearchResult = searchResult;
                _IsNeedToShowSearchResults = true;

                _CurrentSearchIndex = LogLines.IndexOf(searchResult.ClearResults.FirstOrDefault());
                SelectedLogLine = LogLines[_CurrentSearchIndex];

                if (parameter is DataGrid datagrid && LogLines.Contains(SelectedLogLine))
                    datagrid.ScrollIntoView(SelectedLogLine, null);

                StatusChanging(string.Format(StatusFoundResultsCount, searchResult.ClearResults.Count));
            }
            catch (Exception e) { StatusChanging(e); }

            ButtonAdvancedSearch = SearchIcon;
        }

        #endregion

        #region ShowSearchResult : Отобразить выбранный результат поиска в датагриде

        /// <summary>
        /// Отобразить выбранный результат поиска в датагриде
        /// </summary>
        /// <param name="searchResult">Результат поиска</param>
        /// <returns></returns>
        public async Task ShowSearchResult(object parameter)
        {
            if (parameter is not DataGrid datagrid || CurrentSearchResult is null)
            {
                StatusChanging(StatusChooseSearchResultFromList);
                return;
            }

            var logLine = SelectedLogLine;

            LogLines = new(CurrentSearchResult.ClearResults);
            _IsFileOnView = false;

            if (logLine == null)
                return;

            SelectedLogLine = logLine;
            if (LogLines.Contains(SelectedLogLine))
                datagrid.ScrollIntoView(SelectedLogLine, null);
        }

        #endregion

        #region ShowNearestRows : показать ближайшие строки
        public async Task ShowNearestRows(object parameter)
        {
            if (parameter is not DataGrid datagrid || CurrentSearchResult is null)
            {
                StatusChanging(StatusChooseSearchResultFromList);
                return;
            }

            if (CurrentSearchResult.ResultsWithNearestLines.Count <= 0)
                return;

            try
            {
                var logLine = SelectedLogLine;

                CreateLogLines(CurrentSearchResult.ResultsWithNearestLines);
                _IsFileOnView = false;

                if (logLine == null)
                    return;

                SelectedLogLine = logLine;
                if (LogLines.Contains(SelectedLogLine))
                    datagrid.ScrollIntoView(SelectedLogLine, null);
            }
            catch (Exception e) { StatusChanging(e); }
        }

        #endregion

        #region PreviousSearchResult : Предыдущий результат поиска
        /// <summary>
        /// Предыдущая строка лога
        /// </summary>
        /// <returns></returns>
        public async Task PreviousSearchResult(object parameter)
        {
            if (CurrentSearchResult is null || CurrentSearchResult.ClearResults.Count == 0)
            {
                StatusChanging(StatusChooseSearchResultFromList);
                return;
            }

            if (CurrentSearchResult.ClearResults.Count == 1)
            {
                SelectedLogLine = CurrentSearchResult.ClearResults[0];
                return;
            }

            if (parameter is not DataGrid datagrid)
                return;

            try
            {
                if (CurrentSearchResult.ClearResults.Contains(SelectedLogLine))
                {
                    _CurrentSearchIndex = CurrentSearchResult.ClearResults.IndexOf(SelectedLogLine) - 1;
                    SelectedLogLine = _CurrentSearchIndex < 0
                        ? CurrentSearchResult.ClearResults[^1]
                        : CurrentSearchResult.ClearResults[_CurrentSearchIndex];
                }
                else
                    SelectedLogLine = _CurrentSearchIndex > CurrentSearchResult.ClearResults.Count && _CurrentSearchIndex < 0
                        ? CurrentSearchResult.ClearResults[_CurrentSearchIndex]
                        : CurrentSearchResult.ClearResults.FirstOrDefault()!;

                if (LogLines.Contains(SelectedLogLine))
                    datagrid.ScrollIntoView(SelectedLogLine, null);
            }
            catch (Exception e) { StatusChanging(e); }
        }
        #endregion

        #region NextSearchResult : Следующий результат поиска
        /// <summary>
        /// Следующая строка лога
        /// </summary>
        /// <returns></returns>
        public async Task NextSearchResult(object parameter)
        {
            if (CurrentSearchResult is null || CurrentSearchResult.ClearResults.Count == 0)
            {
                StatusChanging(StatusChooseSearchResultFromList);
                return;
            }

            if (CurrentSearchResult.ClearResults.Count == 1)
            {
                SelectedLogLine = CurrentSearchResult.ClearResults[0];
                return;
            }

            if (parameter is not DataGrid datagrid)
                return;

            try
            {
                if (CurrentSearchResult.ClearResults.Contains(SelectedLogLine))
                {
                    _CurrentSearchIndex = CurrentSearchResult.ClearResults.IndexOf(SelectedLogLine) + 1;
                    SelectedLogLine = _CurrentSearchIndex > CurrentSearchResult.ClearResults.Count - 1
                        ? CurrentSearchResult.ClearResults[0]
                        : CurrentSearchResult.ClearResults[_CurrentSearchIndex];
                }
                else
                    SelectedLogLine = _CurrentSearchIndex > CurrentSearchResult.ClearResults.Count && _CurrentSearchIndex < 0
                        ? CurrentSearchResult.ClearResults[_CurrentSearchIndex]
                        : CurrentSearchResult.ClearResults.FirstOrDefault()!;

                if (LogLines.Contains(SelectedLogLine))
                    datagrid.ScrollIntoView(SelectedLogLine, null);
            }
            catch (Exception e) { StatusChanging(e); }
        }

        #endregion

        #region ShowAdvancedSearchWindow : Показать окно расширенного поиска
        /// <summary>
        /// Показать окно расширенного поиска
        /// </summary>
        /// <returns></returns>
        public async Task ShowAdvancedSearchWindow()
        {
            if (ButtonAdvancedSearch == StopIcon)
            {
                _TokenSource.Cancel();
                StatusChanging(StatusSearchStopped);
                ButtonAdvancedSearch = SearchIcon;
                return;
            }

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dataContext = new SearchWindowViewModel(SearchOption)
                {
                    LogFiles = new(LogFileNames),
                    FileSelected = CurrentLogFileItem != null && LogLinesBackup.Any()
                };

                var window = new SearchWindow
                {
                    DataContext = dataContext,
                    Width = 800,
                    Height = 800,
                };

                var result = await window.ShowDialog<int>(desktop.MainWindow);
                SearchOption = dataContext.SearchOption;

                if (result == 1)
                {
                    StatusChanging(StatusSearchInOpenedFile);
                    await StartSearch(FromAdvancedSearch);
                }
                else if (result == 2)
                {
                    StatusChanging(StatusSearchInSelectedFiles);
                    await SearchInFiles(dataContext.SelectedLogFiles);
                }
            }
        }

        #endregion

        #region ShowLogLinesBackup : Показать загруженный файл в датагриде
        /// <summary>
        /// Показать загруженный файл в датагриде
        /// </summary>
        /// <returns></returns>
        public async Task ShowLogLinesBackup()
        {
            if (LogLinesBackup.Any() && !_IsFileOnView)
            {
                CreateLogLines(LogLinesBackup);
                _IsFileOnView = true;
            }
            else if (!LogLinesBackup.Any())
                StatusChanging(StatusChooseLogFile);
        }

        #endregion

        #region UpdateFile : Добавление новых строк в датагрид при изменении размера файла

        /// <summary>
        /// Добавление новых строк в датагрид при изменении размера файла
        /// </summary>
        /// <returns></returns>
        public async Task UpdateFile()
        {
            if (string.IsNullOrEmpty(CurrentLogFileItem))
            {
                StatusChanging(StatusChooseLogFile);
                return;
            }

            if (UpdateButtonColor != ColorGreen
                && _LogWatcher != null
                && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && Path.GetFileNameWithoutExtension(_LogWatcher.Filter) == CurrentLogFileItem)
            {
                var dataContext = new InfoWindowViewModel(
                    InfoTitleAttention,
                    InfoMessageThereIsNoChanges,
                    InfoButtonNameYes,
                    InfoButtonNameNo,
                    true);

                var window = new InfoWindow
                {
                    DataContext = dataContext,
                    Width = 400,
                    Height = 250,
                };

                var result = await window.ShowDialog<int>(desktop.MainWindow);

                if (result == 1)
                {
                    _fileManager.ResetState();
                    await ProcessLogFile(CurrentLogFileItem);
                }
                else if (result == 2)
                    return;
            }
            else
            {
                try
                {
                    await ProcessLogFile(CurrentLogFileItem, true);
                }
                catch (Exception e) { StatusChanging(e); }
            }
            UpdateButtonColor = ColorOrange;
        }

        #endregion

        #region OpenInFile : Открыть файл, содержащий выбранную строку поиска

        /// <summary>
        /// Открыть файл, содержащий выбранную строку поиска
        /// </summary>
        /// <returns></returns>
        public async Task OpenInFile(object parameter)
        {
            if (!LogLines.Any() || CurrentSearchResult == null || _IsFileOnView)
            {
                StatusChanging(StatusChooseSearchResultFromList);
                return;
            }

            if (parameter is not DataGrid datagrid
                || SelectedLogLine == null
                || string.IsNullOrEmpty(SelectedLogLine.FileName)
                || !CurrentSearchResult.ClearResults.Contains(SelectedLogLine))
                return;

            var logLine = SelectedLogLine;

            if (SelectedLogLine.FileName == CurrentLogFileItem)
                await ShowLogLinesBackup();
            else
                CurrentLogFileItem = SelectedLogLine.FileName;

            SelectedLogLine = logLine;
            if (LogLines.Contains(SelectedLogLine))
                datagrid.ScrollIntoView(SelectedLogLine, datagrid.Columns[0]);
        }

        #endregion

        #endregion

        #region Прочие методы

        #region ProcessLogFile : Обработка лог-файла
        /// <summary>
        /// Обработка лог-файла
        /// </summary>
        /// <param name="filePath">Название файла лога или полный путь к нему.</param>
        /// <returns></returns>
        private async Task ProcessLogFile(string filePath, bool isUpdate = false)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            ButtonOpenFolderIcon = StopIcon;

            try
            {
                var currentLogFile = _LogPaths.FirstOrDefault(f => f.Contains(filePath));
                if (new FileInfo(currentLogFile).Length > bigFileSize
                    && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var dataContext = new InfoWindowViewModel(
                        InfoTitleAttention,
                        InfoMessageBigFileSize,
                        InfoButtonNameYes,
                        InfoButtonNameNo,
                        true);

                    var window = new InfoWindow
                    {
                        DataContext = dataContext,
                        Width = 400,
                        Height = 250,
                    };

                    var result = await window.ShowDialog<int>(desktop.MainWindow);

                    if (result == 2)
                    {
                        ButtonOpenFolderIcon = FolderIcon;
                        return;
                    }
                }

                var newLines = new List<LogModel>();
                try
                {
                    _TokenSource = new CancellationTokenSource();
                    var token = _TokenSource.Token;
                    await Task.Run(async () =>
                    {
                        await foreach (var line in _fileHandler.ConvertFileToView(currentLogFile, token))
                            newLines.Add(line);
                    });
                }
                catch (OperationCanceledException) { }
                finally { _TokenSource.Dispose(); }

                if (newLines.Any())
                {
                    if (isUpdate)
                    {
                        LogLinesBackup.AddRange(newLines);
                        UpdateLogLinesUI(newLines);
                    }
                    else
                    {
                        LogLinesBackup = new(newLines);
                        CreateLogLinesUI(LogLinesBackup);
                    }

                    SelectedLogLine = newLines[0];
                    StartLogWatcher(currentLogFile);
                }
            }
            catch (Exception e) { StatusChanging(e); }

            ButtonOpenFolderIcon = FolderIcon;
        }

        #endregion

        #region SearchInFiles : Поиск строки в файле лога
        /// <summary>
        /// Поиск строки в файлах лога
        /// </summary>
        /// <param name="fileNames">Список имен файлов, в которых необходимо произвести поиск</param>
        /// <returns></returns>
        private async Task SearchInFiles(IEnumerable<string> fileNames)
        {
            ButtonAdvancedSearch = StopIcon;
            CurrentSearchResult = null;

            try
            {
                var results = new List<SearchResult> { new SearchResult() };

                foreach (var fileName in fileNames)
                {
                    await Task.Run(async () =>
                    {
                        var filePath = _LogPaths.FirstOrDefault(f => f.Contains(fileName));
                        if (filePath == null)
                            return;

                        var result = new SearchResult();
                        try
                        {
                            _TokenSource = new CancellationTokenSource();
                            var token = _TokenSource.Token;

                            result = await _searcher.SearchInFile(filePath, SearchOption, token);
                        }
                        catch (OperationCanceledException) { }
                        finally { _TokenSource.Dispose(); }

                        if (result.ClearResults.Count > 0)
                        {
                            result.FilePath = filePath;
                            result.FileName = fileName;
                            result.SearchString = SearchOption.SearchLine;

                            results.Add(result);
                            results[0].ClearResults.AddRange(result.ClearResults);
                            results[0].ResultsWithNearestLines.AddRange(result.ResultsWithNearestLines);
                        }
                    });
                }

                // Если найдено совпадений больше чем в одном файле, то обновляем данные результата Все результаты поиска
                // В противном случае удаляем Все результаты поиска
                if (results.Count == 2)
                    results.RemoveAt(0);
                else
                {
                    results[0].SearchString = SearchOption.SearchLine;
                    results[0].FileName = AllResults;
                }

                if (results.Count <= 0)
                {
                    StatusChanging(StatusNotFound);
                    return;
                }

                foreach (var result in results)
                    SearchResults.Add(result);

                CurrentSearchResult = results[0];

                StatusChanging(string.Format(
                    StatusFoundResultsInFilesCount,
                    results.FirstOrDefault()?.ClearResults.Count,
                    results.Count > 1 ? results.Count - 1 : results.Count));
            }
            catch (Exception e) { StatusChanging(e); }

            ButtonAdvancedSearch = SearchIcon;
        }
        #endregion

        #region OnClosingWindow : Закрытие программы
        /// <summary>
        /// Закрытие программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnClosingWindow(object sender, CancelEventArgs e)
        {
            DisposeLogWatcher();
            _log.Information(LogAppClosing);
            Log.CloseAndFlush();
        }
        #endregion

        #region OnOpeningWindow : Открытие программы
        /// <summary>
        /// Закрытие программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnOpeningWindow(object sender, EventArgs e) => _log.Information(LogAppOpening);
        #endregion

        #region CreateLogLines : Создать отображаемые строки логов в представлении
        /// <summary>
        /// Создать отображаемые строки логов в представлении
        /// </summary>
        /// <param name="lines">Новые строки логов</param>
        private void CreateLogLinesUI(IEnumerable<LogModel> lines)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(new Action(() => { CreateLogLines(lines); }));
        }

        private void CreateLogLines(IEnumerable<LogModel> lines)
        {
            LogLines = new ObservableCollection<LogModel>(lines);
        }

        #endregion

        #region UpdateLogLinesUI : Создать отображаемые строки логов в представлении
        /// <summary>
        /// Создать отображаемые строки логов в представлении
        /// </summary>
        /// <param name="lines">Новые строки логов</param>
        private void UpdateLogLinesUI(IEnumerable<LogModel> lines)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
                new Action(() =>
                {
                    foreach (var line in lines)
                        LogLines.Add(line);
                }));
        }
        #endregion

        #region DisposeLogWatcher : Освободить ресурсы наблюдателя за изменения лог-файлов
        /// <summary>
        /// Освободить ресурсы наблюдателя за изменения лог-файлов
        /// </summary>
        private void DisposeLogWatcher()
        {
            if (_LogWatcher == null)
                return;

            _LogWatcher.Dispose();
            _LogWatcher = null;
        }
        #endregion

        #region StartLogWatcher : Запустить наблюдателя за изменения лог-файлов
        /// <summary>
        /// Запустить наблюдателя за изменения лог-файлов
        /// </summary>
        /// <param name="filPath">Полный путь к лог-файлу, изменения которого нужно отслеживать</param>
        private void StartLogWatcher(string filPath)
        {
            if (string.IsNullOrEmpty(filPath))
                return;
            
            DisposeLogWatcher();

            _LogWatcher = new FileSystemWatcher(Path.GetDirectoryName(filPath))
            {
                NotifyFilter = NotifyFilters.Size,
                Filter = Path.GetFileName(filPath),
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _LogWatcher.Changed += new FileSystemEventHandler(OnFileChanged);
        }
        #endregion

        #region OnFileChanged : Изменение цвета кнопки обновления файла при изменении размера файла

        /// <summary>
        /// Изменение цвета кнопки обновления файла при изменении размера файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (UpdateButtonColor != ColorGreen)
                UpdateButtonColor = ColorGreen;
        }
        #endregion

        #region StatusChanging : Изменение статуса с записью в лог

        /// <summary>
        /// Изменение статуса с записью в лог.
        /// </summary>
        /// <param name="message">Сообщение для записи</param>
        /// <param name="isError">Если true, то статусное сообщение - об ошибке и логироваться будет отдельно. По умолчанию false.</param>
        public void StatusChanging(string message)
        {
            _log.Information(message);
            Status = message;
        }

        /// <summary>
        /// Изменение статуса с записью в лог.
        /// </summary>
        /// <param name="message">Сообщение для записи</param>
        /// <param name="isError">Если true, то статусное сообщение - об ошибке и логироваться будет отдельно. По умолчанию false.</param>
        public void StatusChanging(Exception exception, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                _log.Error(exception, StatusUnexpectedError);
                Status = StatusUnexpectedError + exception.Message;
            }
            else
            {
                _log.Error(exception, message);
                Status = message + exception.Message;
            }
        }
        #endregion

        #endregion

        #region Поля

        #region Константы
        /// <summary>Максимально допустимый размер файла для обработки системой - 300 мб</summary>
        private const long bigFileSize = 314572800;
        /// <summary>Сообщение диалогового окна открытия файлов</summary>
        private const string ChooseLogFiles = "Выберите один или несколько лог-файлов.";
        /// <summary>Расширение логов диалогового окна открытия файлов</summary>
        private const string LogExt = "log";
        /// <summary>Дополнительное расширение логов диалогового окна открытия файлов</summary>
        private const string AllExt = "*";
        /// <summary>Фильтр расширение логов диалогового окна открытия файлов</summary>
        private const string DlgJsonFilter = "Log files (.log)|*.log";
        /// <summary>Фильтр дополнительного расширения логов диалогового окна открытия файлов</summary>
        private const string DlgExtFilter = "All files (*.*)";

        /// <summary>Заголовок главного окна</summary>
        private const string TitleLogSurfer = "LogGrinder";

        /// <summary>Результат поиска, который содержит все строки вместе.</summary>
        private const string AllResults = "все результаты";

        /// <summary>Контекст запуска поиска в текущем файле</summary>
        private const string FromAdvancedSearch = "From advanced search";

        /// <summary>Консатнта для формирования имени результатов поиска</summary>
        private const string AdvancedSearch = "Расширенный поиск";

        #region Сообщения информационного окна

        private const string InfoTitleAttention = "Внимание!";
        private const string InfoMessageThereIsNoChanges = "Изменений в файле не обнаружено. Уверены, что хотите его перезагрузить?";
        private const string InfoMessageBigFileSize = "Размер выбранного файла больше 300 мегабайт. Обработка такого файла может занять продолжительное время (больше 10 секунд). Вы уверены, что хотите продолжить?";
        private const string InfoButtonNameYes = "Да";
        private const string InfoButtonNameNo = "Нет";

        #endregion

        #region Сообщения в статусной строке
        private const string StatusUnexpectedError = "Непредвиденная ошибка: ";
        private const string StatusHi = "Привет!";
        private const string StatusNotFound = "Ничего не найдено.";
        private const string StatusFoundResultsCount = "Найдено {0} результатов.";
        private const string StatusFoundResultsInFilesCount = "Найдено результатов: {0} в {1} файлах.";
        private const string StatusSearchInOpenedFile = "Начинаем поиск в текущем файле";
        private const string StatusSearchInSelectedFiles = "Начинаем поиск в выбранных файлах";
        private const string StatusProcessingFileStopped = "Обработка файла остановлена";
        private const string StatusSearchStopped = "Поиск остановлен";
        private const string StatusSearchingMatchWith = "Ищем совпадение со строкой ";
        private const string StatusChooseLogFile = "Сначала выберите файл логов.";
        private const string StatusChooseSearchResultFromList = "Выберите результат поиска из выпадающего списка.";
        #endregion

        #region Иконки кнопок
        private const string FolderIcon = "FolderDownloadOutline";
        private const string SearchIcon = "Search";
        private const string StopIcon = "CloseCircleOutline";
        #endregion

        #region Цвета
        private const string ColorOrange = "#ff9800";
        private const string ColorGreen = "Chartreuse";

        #endregion

        #region Сообщения для логгера текущего приложения
        private const string LogAppClosing = "Закрытие программы.";
        private const string LogAppOpening = "Запуск программы.";
        #endregion

        #endregion

        #region Сервисы

        private readonly IFileManager _fileManager;
        private readonly IFileHandler _fileHandler;
        private readonly ILineHandler _lineHandler;
        private readonly ISearcher _searcher;
        private readonly ILogger _log = Log.ForContext<MainWindowViewModel>();
        #endregion

        #region Прочие

        /// <summary>Список файлов с указанием полного пути</summary>
        private readonly List<string> _LogPaths = new();
        /// <summary>Индекс результата поиска</summary>
        private int _CurrentSearchIndex = -1;
        /// <summary>Наблюдатель изменений в файле</summary>
        private FileSystemWatcher _LogWatcher;
        /// <summary>True - в датагриде отображается обработанный файл. False - результаты поиска</summary>
        private bool _IsFileOnView = false;
        /// <summary>Необходимость показать в датагриде результаты поиска</summary>
        private bool _IsNeedToShowSearchResults = true;
        /// <summary>Опеератор токена для остановки асинхронных процессов</summary>
        private CancellationTokenSource _TokenSource;
        #endregion

        #endregion
    }
}
