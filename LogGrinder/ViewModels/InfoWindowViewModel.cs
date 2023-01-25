using System.Threading.Tasks;

using Avalonia.Controls;

using LogGrinder.Core;

namespace LogGrinder.ViewModels
{
    internal class InfoWindowViewModel : ViewModelCore
    {
        public InfoWindowViewModel(string title, string message, string buttonNameOne, string buttonNameTwo, bool isCancelButtonAvailable)
        {
            Title = title;
            InfoMessage = message;
            ButtonNameOne = buttonNameOne;
            ButtonNameTwo = buttonNameTwo;
            IsCancelButtonAvailable = isCancelButtonAvailable;
        }

        #region Свойтсва

        #region Title : string - Заголовок информационного окна

        /// <summary>Заголовок информационного окна</summary>
        private string _Title;

        /// <summary>Заголовок информационного окна</summary>
        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        #endregion

        #region InfoMessage : string - Сообщение информационного окна

        /// <summary>Сообщение информационного окна</summary>
        private string _InfoMessage;

        /// <summary>Сообщение информационного окна</summary>
        public string InfoMessage
        {
            get => _InfoMessage;
            set => Set(ref _InfoMessage, value);
        }

        #endregion

        #region IsCancelButtonAvailable : bool - Включение кнопки Отмена

        /// <summary>Включение кнопки Отмена</summary>
        private bool _IsCancelButtonAvailable;

        /// <summary>Включение кнопки Отмена</summary>
        public bool IsCancelButtonAvailable
        {
            get => _IsCancelButtonAvailable;
            set => Set(ref _IsCancelButtonAvailable, value);
        }

        #endregion

        #region ButtonNameOne : string - Текст первой кнопки

        /// <summary>Текст первой кнопки</summary>
        private string _ButtonNameOne;

        /// <summary>Текст первой кнопки</summary>
        public string ButtonNameOne
        {
            get => _ButtonNameOne;
            set => Set(ref _ButtonNameOne, value);
        }

        #endregion

        #region ButtonNameTwo : string - Текст второй кнопки

        /// <summary>Текст второй кнопки</summary>
        private string _ButtonNameTwo;

        /// <summary>Текст второй кнопки</summary>
        public string ButtonNameTwo
        {
            get => _ButtonNameTwo;
            set => Set(ref _ButtonNameTwo, value);
        }

        #endregion

        #endregion

        #region Команды

        public async Task FirstCommand(object parameter)
        {
            if (parameter is Window window)
                window.Close(1);
        }

        public async Task SecondCommand(object parameter)
        {
            if (parameter is Window window)
                window.Close(2);
        }

        #endregion
    }
}
