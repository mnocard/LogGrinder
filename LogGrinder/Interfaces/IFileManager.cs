using System.IO;

namespace LogGrinder.Interfaces
{
    public interface IFileManager
    {
        StreamReader StreamReader(string path);
        StreamReader StreamReader(string path, FileStreamOptions fileReadingOptions);
    }
}
