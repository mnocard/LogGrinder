using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogGrinder.Models
{
    internal class SearchModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        private string _SearchLine;
        /// <summary>Искомая строка</summary>
        public string SearchLine
        {
            get => _SearchLine;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.Trim();
                _SearchLine = value;

                OnPropertyChanged();
            }
        }
        /// <summary>Строка, которую необходимо игнорировать при поиске</summary>
        public string ExcludeLine { get; set; }
        /// <summary>Дата начала поиска</summary>
        public int LineNumberStart { get; set; } = 0;
        /// <summary>Дата окончания поиска</summary>
        public int LineNumberEnd { get; set; } = 0;
        /// <summary>Дата начала поиска</summary>
        public string DateBegin { get; set; }
        /// <summary>Дата окончания поиска</summary>
        public string DateEnd { get; set; }
        /// <summary>Загружать количество строк до найденной строки</summary>
        public int LinesCountBefore { get; set; } = 0;
        /// <summary>Загружать количество строк после найденной строки</summary>
        public int LinesCountAfter { get; set; } = 0;

        public List<Attribute> Attributes { get; set; } = new();

        internal class Attribute
        {
            public string Name { get; set; }
            public bool Condition { get; set; }
            public string SearchLinePattern { get; set; }
        }
    }
}
