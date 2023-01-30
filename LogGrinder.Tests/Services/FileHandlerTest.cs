using System.Text;

using Moq;

namespace LogGrinder.Tests.Services
{
    public partial class FileHandlerTest
    {
        private const string _path = "Path";
        private readonly MemoryStream _fakeMemoryStream;
        private readonly byte[] _fakeFileBytes;
        private const string _fakeFileContents =
            "{\"t\":\"2022-05-25 00:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 1\"}\r\n" +
            "{\"t\":\"2022-05-25 00:04:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 2\"}\r\n" +
            "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}\r\n";

        public FileHandlerTest()
        {
            _fakeFileBytes = Encoding.UTF8.GetBytes(_fakeFileContents);
            _fakeMemoryStream = new MemoryStream(_fakeFileBytes);
        }

        [Theory]
        [AutoDomainData]
        public async void ConvertFileToView_IncorrectContent_AggregateException(
            [Frozen] Mock<IFileManager> mockFileManager,
            FileHandler sut)
        {
            // Arrange
            const string incorrectLogFile = "Incorrect log file content.";
            const string exceptionText = "'I' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.";
            var fakeFileBytes = Encoding.UTF8.GetBytes(incorrectLogFile);
            var fakeMemoryStream = new MemoryStream(fakeFileBytes);

            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(fakeMemoryStream));

            Exception exception = null;

            // Act
            try
            {
                await foreach (var _ in sut.ConvertFileToView(_path)) { }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<System.Text.Json.JsonException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }

        [Theory]
        [AutoDomainData]
        public async void ConvertFileToView_EmptyPath_EmptyCollection(
            [Frozen] Mock<IFileManager> mockFileManager,
            FileHandler sut)
        {
            // Arrange
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(_fakeMemoryStream));

            // Act
            var result = new List<LogModel>();
            await foreach (var model in sut.ConvertFileToView(string.Empty))
                result.Add(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.Empty(result);
        }

        [Theory]
        [AutoDomainData]
        public async void ConvertFileToView_CorrectConverting(
            [Frozen] Mock<IFileManager> mockFileManager,
            FileHandler sut)
        {
            // Arrange
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                           .Returns(() => new StreamReader(_fakeMemoryStream));

            var expectedLogModels = new List<LogModel>
            {
                new LogModel { Id = 1, FileName = "Path", t = "2022-05-25 00:01:23.456+05:00", l = "Info", mt = "Line 1", RawLine = "{\"t\":\"2022-05-25 00:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 1\"}", Other = "Line 1" },
                new LogModel { Id = 2, FileName = "Path", t = "2022-05-25 00:04:23.456+05:00", l = "Debug", mt = "Line 2", RawLine = "{\"t\":\"2022-05-25 00:04:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 2\"}", Other = "Line 2" },
                new LogModel { Id = 3, FileName = "Path", t = "2022-05-25 00:07:23.456+05:00", l = "Error", ex = "{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}",
                    RawLine = "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}",
                    Other = "ex: {\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}" },
            };

            // Act
            var result = new List<LogModel>();
            await foreach (var model in sut.ConvertFileToView(_path))
                result.Add(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.True(LogModelsEquals(expectedLogModels, result));
        }

        [Theory]
        [AutoDomainData]
        public async void ConvertFileToView_CancelConverting(
            [Frozen] Mock<IFileManager> mockFileManager,
            FileHandler sut)
        {
            // Arrange
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(_fakeMemoryStream));

            CancellationTokenSource _tokenSource = new ();
            CancellationToken _token = _tokenSource.Token;
            _tokenSource.CancelAfter(0);
            Exception exception = null;

            // Act
            try
            {
                await foreach (var _ in sut.ConvertFileToView(_path, _token)) { }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                _tokenSource.Dispose();
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<OperationCanceledException>(exception);
        }

        [Theory]
        [AutoDomainData]
        public async void ConvertFileToView_ContinuousConverting(
            [Frozen] Mock<IFileManager> mockFileManager,
            FileHandler sut)
        {
            // Arrange
            // 129 - начальная позиция 3ой строки в _fakeMemoryStream
            mockFileManager.SetupProperty(p => p.FileSize, 129);
            mockFileManager.SetupProperty(p => p.FileName, "Path");
            mockFileManager.SetupProperty(p => p.LineNumber, 2);
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(_fakeMemoryStream));

            var expectedLogModels = new List<LogModel>
            {
                new LogModel { Id = 3, FileName = "Path", t = "2022-05-25 00:07:23.456+05:00", l = "Error", ex = "{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}",
                    RawLine = "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}",
                    Other = "ex: {\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}" },
            };

            // Act
            var result = new List<LogModel>();
            await foreach (var model in sut.ConvertFileToView(_path))
                result.Add(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.True(LogModelsEquals(expectedLogModels, result));
        }

        private bool LogModelsEquals(LogModel first, LogModel second)
        {
            if ((first is null && second is not null) ||
                (first is not null && second is null))
                return false;

            if (first is null && second is null)
                return true;

            return first.Id == second.Id &&
                first.t == second.t &&
                first.l == second.l &&
                first.pid == second.pid &&
                first.tab == second.tab &&
                first.mt == second.mt &&
                first.tr == second.tr &&
                first.bn == second.bn &&
                first.bv == second.bv &&
                first.lg == second.lg &&
                first.v == second.v &&
                first.un == second.un &&
                first.tn == second.tn &&
                first.args?.ToString() == second.args?.ToString() &&
                first.cust?.ToString() == second.cust?.ToString() &&
                first.ex?.ToString() == second.ex?.ToString() &&
                first.span?.ToString() == second.span?.ToString() &&
                first.Other == second.Other &&
                first.RawLine == second.RawLine &&
                first.FileName == second.FileName;
        }

        private bool LogModelsEquals(List<LogModel> first, List<LogModel> second)
        {
            if ((first is null && second is not null) ||
                (first is not null && second is null) ||
                (first.Count != second.Count))
                return false;

            if ((first is null && second is null) ||
                (!first.Any() && !second.Any()))
                return true;

            for (int i = 0; i < first.Count; i++)
                if (!LogModelsEquals(first[i], second[i]))
                    return false;

            return true;
        }
    }
}
