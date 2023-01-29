using Moq;

using System.Text;

namespace LogGrinder.Tests.Services
{
    public partial class FileHandlerTest
    {
        private readonly MemoryStream _fakeMemoryStream;
        private readonly byte[] _fakeFileBytes;
        private const string _fakeFileContents = "{\"t\":\"2022-05-25 00:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 1\"}\r\n{\"t\":\"2022-05-25 00:02:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 2\"}\r\n{\"t\":\"2022-05-25 00:03:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 3\"}\r\n{\"t\":\"2022-05-25 00:04:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 4\"}\r\n{\"t\":\"2022-05-25 00:05:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 5\"}\r\n{\"t\":\"2022-05-25 00:06:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 6\"}\r\n{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\", \"m\":\"Line 7.\", \"stack\":\"System.NullReferenceException.\\r\\n\"}}\r\n{\"t\":\"2022-05-25 00:08:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\", \"m\":\"Line 8.\", \"stack\":\"System.NullReferenceException.\\r\\n\"}}\r\n";

        public FileHandlerTest()
        {
            _fakeFileBytes = Encoding.UTF8.GetBytes(_fakeFileContents);
            _fakeMemoryStream = new MemoryStream(_fakeFileBytes);
        }

        [Theory]
        [AutoDomainData]
        public async Task FileHandler_AggregateException_IncorrectContent(
            [Frozen] Mock<IFileManager> _mockFileManager,
            FileHandler sut)
        {
            var incorrectLogFile = "Incorrect log file content.";
            var fakeFileBytes = Encoding.UTF8.GetBytes(incorrectLogFile);
            var fakeMemoryStream = new MemoryStream(fakeFileBytes);
            // Arrange
            _mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(fakeMemoryStream));

            const string exceptionText = "One or more errors occurred. ('I' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.)";
            const string line = "Path";

            // Act
            Action act = () => sut.ConvertFileToView(line).Wait();
            var exception = Record.Exception(act);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<AggregateException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }

        [Theory]
        [AutoDomainData]
        public async Task FileHandler_EmptyPath_EmptyCollection(
            [Frozen] Mock<IFileManager> _mockFileManager,
            FileHandler sut)
        {
            // Arrange
            _mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(_fakeMemoryStream));

            const string line = "Path";

            // Act
            var result = sut.ConvertFileToView(string.Empty).Result;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.Empty(result);
        }
    }
}
