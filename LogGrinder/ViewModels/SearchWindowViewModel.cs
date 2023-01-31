using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Avalonia.Controls;

using LogGrinder.Core;

using LogGrinder.Models;

namespace LogGrinder.ViewModels
{
    internal class SearchWindowViewModel : ViewModelCore
    {
        public SearchWindowViewModel(SearchModel searchOption)
        {
            _SearchOption = searchOption;
        }

        #region Свойства

        #region Title : string - Заголовок окна

        /// <summary>Заголовок окна</summary>
        private string _Title = TitleSearchWindow;

        /// <summary>Заголовок окна</summary>
        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        #endregion

        #region LogFiles : ObservableCollection<string> - Список файлов логов

        /// <summary>Список файлов логов</summary>
        private ObservableCollection<string> _LogFiles = new();

        /// <summary>Список файлов логов</summary>
        public ObservableCollection<string> LogFiles
        {
            get => _LogFiles;
            set => Set(ref _LogFiles, value);
        }

        #endregion

        #region SelectedLogFiles : ObservableCollection<string> - Список файлов логов, в которых необходимо осуществить поиск

        /// <summary>Список файлов логов, в которых необходимо осуществить поиск</summary>
        private ObservableCollection<string> _SelectedLogFiles = new();

        /// <summary>Список файлов логов, в которых необходимо осуществить поиск</summary>
        public ObservableCollection<string> SelectedLogFiles
        {
            get => _SelectedLogFiles;
            set => Set(ref _SelectedLogFiles, value);
        }

        #endregion

        #region SearchModel : SearchOption - Настройка поиска

        /// <summary>Настройка поиска</summary>
        private SearchModel _SearchOption;

        /// <summary>Настройка поиска</summary>
        public SearchModel SearchOption
        {
            get => _SearchOption;
            set => Set(ref _SearchOption, value);
        }

        #endregion

        #region ChangeAreaContent : string - Надпись на кнопке для смены области поиска

        /// <summary>Надпись на кнопке для смены области поиска</summary>
        private string _ChangeAreaContent;

        /// <summary>Надпись на кнопке для смены области поиска</summary>
        public string ChangeAreaContent
        {
            get => _ChangeAreaContent;
            set => Set(ref _ChangeAreaContent, value);
        }

        #endregion

        #region Status : string - Статусная строка

        /// <summary>Статусная строка</summary>
        private string _Status = StatusChooseSettings;

        /// <summary>Статусная строка</summary>
        public string Status
        {
            get => _Status;
            set => Set(ref _Status, value);
        }

        #endregion

        #region FileSelected : bool - Переключатель, отражающий наличие обработанного файла в главном окне

        /// <summary>Переключатель, отражающий наличие обработанного файла в главном окне</summary>
        private bool _FileSelected;

        /// <summary>Переключатель, отражающий наличие обработанного файла в главном окне</summary>
        public bool FileSelected
        {
            get => _FileSelected;
            set => Set(ref _FileSelected, value);
        }

        #endregion

        #endregion

        #region Команды

        /// <summary>
        /// Закрыть текущее окно с результатом Поиск в текущем файле
        /// </summary>
        /// <param name="parameter">Текущее окно</param>
        /// <returns></returns>
        public async Task CloseWithSearchInCurrentFile(object parameter)
        {
            if (!FileSelected)
                Status = StatusOpenFile;
            else if (parameter is Window window)
                window.Close(1);
        }

        /// <summary>
        /// Закрыть текущее окно с результатом Поиск в выбранных файлах
        /// </summary>
        /// <param name="parameter">Текущее окно</param>
        /// <returns></returns>
        public async Task CloseWithSearchInselectedFiles(object parameter)
        {
            if (SelectedLogFiles.Count < 1)
                Status = StatusChooseAtLeastOneFile;
            else if (parameter is Window window)
                window.Close(2);
        }

        /// <summary>
        /// Закрыть текущее окно с результатом Поиск в выбранных файлах
        /// </summary>
        /// <param name="parameter">Текущее окно</param>
        /// <returns></returns>
        public async Task CloseWindow(object parameter)
        {
            if (parameter is Window window)
                window.Close(0);
        }
        #endregion

        #region Константы

        private const string TitleSearchWindow = "Расширенные поиск";

        private const string StatusChooseAtLeastOneFile = "Нужно выбрать хотя бы один файл.";
        private const string StatusChooseSettings = "Настройте поиск.";
        private const string StatusOpenFile = "Сначала откройте файл в главном окне.";

        #endregion
    }
}
