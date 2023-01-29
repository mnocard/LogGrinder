using System.IO;

using LogGrinder.Interfaces;

namespace LogGrinder.Services
{
    public class FileManager : IFileManager
    {
        public int LineNumber { get; set; } = 0;
        public long FileSize { get; set; } = 0;
        public string FileName { get; set; } = string.Empty;
        public StreamReader StreamReader(string path) => new(path);

        public StreamReader StreamReader(string path, FileStreamOptions fileReadingOptions) => new(path, fileReadingOptions);
        /// <inheritdoc />
        public void ResetState()
        {
            FileSize = 0;
            LineNumber = 0;
            FileName = string.Empty;
        }
    }
}
