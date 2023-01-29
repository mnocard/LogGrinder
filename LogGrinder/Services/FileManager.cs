using System.IO;

using LogGrinder.Interfaces;

namespace LogGrinder.Services
{
    public class FileManager : IFileManager
    {
        public StreamReader StreamReader(string path) => new StreamReader(path);

        public StreamReader StreamReader(string path, FileStreamOptions fileReadingOptions) => new StreamReader(path, fileReadingOptions);
    }
}
