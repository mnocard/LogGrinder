using System.Text;

using AutoFixture.AutoMoq;

using Moq;

namespace LogGrinder.Tests.Services
{
    public partial class FileHandlerTest
    {
        private readonly Mock<IFileManager> _mockFileManager;
        private readonly FileHandler _sut;

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

            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            _mockFileManager = fixture.Freeze<Mock<IFileManager>>();
            _mockFileManager
                .Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                .Returns(() => new StreamReader(_fakeMemoryStream));

            _sut = fixture.Create<FileHandler>();
        }

        [Fact]
        public async void ConvertFileToView_IncorrectContent_AggregateException()
        {
            // Arrange
            const string incorrectLogFile = "Incorrect log file content.";
            const string exceptionText = "'I' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.";
            var fakeFileBytes = Encoding.UTF8.GetBytes(incorrectLogFile);
            var fakeMemoryStream = new MemoryStream(fakeFileBytes);

            _mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(fakeMemoryStream));

            Exception exception = null;

            // Act
            try
            {
                await foreach (var _ in _sut.ConvertFileToView(_path)) { }
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

        [Fact]
        public async void ConvertFileToView_EmptyPath_EmptyCollection()
        {
            // Arrange
            var result = new List<LogModel>();

            // Act
            await foreach (var model in _sut.ConvertFileToView(string.Empty))
                result.Add(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.Empty(result);
        }

        [Fact]
        public async void ConvertFileToView_CorrectConverting()
        {
            // Arrange
            var result = new List<LogModel>();

            var expectedLogModels = new List<LogModel>
            {
                new LogModel { Id = 1, FileName = "Path", t = "2022-05-25 00:01:23.456+05:00", l = "Info", mt = "Line 1", RawLine = "{\"t\":\"2022-05-25 00:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 1\"}", Other = "Line 1" },
                new LogModel { Id = 2, FileName = "Path", t = "2022-05-25 00:04:23.456+05:00", l = "Debug", mt = "Line 2", RawLine = "{\"t\":\"2022-05-25 00:04:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 2\"}", Other = "Line 2" },
                new LogModel { Id = 3, FileName = "Path", t = "2022-05-25 00:07:23.456+05:00", l = "Error", ex = "{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}",
                    RawLine = "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}",
                    Other = "ex: {\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}" },
            };

            // Act
            await foreach (var model in _sut.ConvertFileToView(_path))
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
            // По непонятной причине, если использовать мок FileManager, созданный в конструкторе и тестировать все тесты сразу, то ошибка не выбрасывается и тест проваливается.
            // При этом при одиночном запуске - тест проходит корректно. А если использовать AutoDomainData, то тесты проходит хоть один, хоть с остальными.
            // Очень ненадежный тест, возможно следует от него отказаться...
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(_fakeMemoryStream));

            CancellationTokenSource tokenSource = new();
            CancellationToken token = tokenSource.Token;
            tokenSource.CancelAfter(0);
            Exception exception = null;

            // Act
            try
            {
                await foreach (var _ in sut.ConvertFileToView(_path, token)) { }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                tokenSource.Dispose();
            }

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<OperationCanceledException>(exception);
        }

        [Fact]
        public async void ConvertFileToView_ContinuousConverting()
        {
            // Arrange
            // 129 - начальная позиция 3ой строки в _fakeMemoryStream
            _mockFileManager.SetupProperty(p => p.FileSize, 129);
            _mockFileManager.SetupProperty(p => p.FileName, "Path");
            _mockFileManager.SetupProperty(p => p.LineNumber, 2);

            var expectedLogModels = new List<LogModel>
            {
                new LogModel { Id = 3, FileName = "Path", t = "2022-05-25 00:07:23.456+05:00", l = "Error", ex = "{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}",
                    RawLine = "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}",
                    Other = "ex: {\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}" },
            };

            // Act
            var result = new List<LogModel>();
            await foreach (var model in _sut.ConvertFileToView(_path))
                result.Add(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<LogModel>>(result);
            Assert.True(LogModelsEquals(expectedLogModels, result));
        }

        public static bool LogModelsEquals(LogModel first, LogModel second)
        {
            if ((first is null && second is not null) ||
                (first is not null && second is null))
                return false;

            if (first is null && second is null)
                return true;

            if (first.Id == second.Id)
                if (first.t == second.t)
                    if (first.l == second.l)
                        if (first.pid == second.pid)
                            if (first.tab == second.tab)
                                if (first.mt == second.mt)
                                    if (first.tr == second.tr)
                                        if (first.bn == second.bn)
                                            if (first.bv == second.bv)
                                                if (first.lg == second.lg)
                                                    if (first.v == second.v)
                                                        if (first.un == second.un)
                                                            if (first.tn == second.tn)
                                                                if (first.args?.ToString() == second.args?.ToString())
                                                                    if (first.cust?.ToString() == second.cust?.ToString())
                                                                        if (first.ex?.ToString() == second.ex?.ToString())
                                                                            if (first.span?.ToString() == second.span?.ToString())
                                                                                if (first.Other == second.Other)
                                                                                    if (first.RawLine == second.RawLine)
                                                                                        if (first.FileName == second.FileName)
                                                                                            return true;

            return false;
        }

        public static bool LogModelsEquals(List<LogModel> first, List<LogModel> second)
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
