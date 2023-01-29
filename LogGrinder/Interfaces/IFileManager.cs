using System.IO;

namespace LogGrinder.Interfaces
{
    public interface IFileManager
    {
        public int LineNumber { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; }

        /// <summary>
        /// Сброс состояния сервиса
        /// </summary>
        public void ResetState();
        
        StreamReader StreamReader(string path);
        StreamReader StreamReader(string path, FileStreamOptions fileReadingOptions);
    }
}
