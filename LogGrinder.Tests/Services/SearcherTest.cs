using System.Text;

using AutoFixture.AutoMoq;

using Moq;

namespace LogGrinder.Tests.Services
{
    public class SearcherTest
    {
        private readonly Fixture _fixture;
        private readonly List<LogModel> _models;

        public SearcherTest()
        {
            _models = new List<LogModel>
            {
                new LogModel { Id = 1, t = "2022-05-25 00:01:23.456+05:00", l = "Info",  mt = "Line 1" },
                new LogModel { Id = 2, t = "2022-05-25 00:04:23.456+05:00", l = "Debug", mt = "Line 2" },
                new LogModel { Id = 3, t = "2022-05-25 00:07:23.456+05:00", l = "Error", ex = "{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}" },
                new LogModel { Id = 4, t = "2022-05-25 01:01:23.456+05:00", l = "Info",  mt = "Line 4" },
                new LogModel { Id = 5, t = "2022-05-25 02:01:23.456+05:00", l = "Debug", mt = "Line 5" },
                new LogModel { Id = 6, t = "2022-05-25 03:01:23.456+05:00", l = "Info",  mt = "Line 6" },
                new LogModel { Id = 7, t = "2022-05-25 04:01:23.456+05:00", l = "Debug", mt = "Line 7" },
                new LogModel { Id = 8, t = "2022-05-25 05:01:23.456+05:00", l = "Info",  mt = "Line 8" },
                new LogModel { Id = 9, t = "2022-05-25 06:01:23.456+05:00", l = "Info",  mt = "Line 9" },

            };

            _fixture = new Fixture();
            _fixture.Customize(new AutoMoqCustomization());
        }

        #region SearchInOpenedFile

        [Theory]
        [InlineData("Error")]
        [InlineData("2022-05-25 00:07:23")]
        [InlineData("AggregateException")]
        public async void SearchInOpenedFile_CorrectSimpleSearching(string searchString)
        {
            // Arrange
            var option = new SearchModel { SearchLine = searchString, };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[2] },
                ResultsWithNearestLines = new List<LogModel> { _models[2] },
            };

            var sut = PrepareSUTForSearchInOpenedFile();

            // Act
            var result = await sut.SearchInOpenedFile(_models, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }

        [Theory]
        [AutoDomainData]
        public async void SearchInOpenedFile_AdvancedSearch(Searcher sut)
        {
            // Arrange
            var option = new SearchModel
            {
                LinesCountBefore = 1,
                LinesCountAfter = 1,
                DateBegin = "2022-05-25 02:01:23",
                DateEnd = "2022-05-25 05:01:23",
                LineNumberStart = 6,
                LineNumberEnd = 9,
                ExcludeLine = "Info",
            };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[6] },
                ResultsWithNearestLines = new List<LogModel> { _models[5], _models[6], _models[7] },
            };

            // Act
            var result = await sut.SearchInOpenedFile(_models, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }

        [Fact]
        public async void SearchInOpenedFile_AdvancedSearch_WithAttributes()
        {
            // Arrange
            var option = new SearchModel
            {
                SearchLine = "$l=-\"Info\"",
                LinesCountBefore = 1,
                LinesCountAfter = 1,
                DateBegin = "2022-05-25 02:01:23",
                DateEnd = "2022-05-25 05:01:23",
                LineNumberStart = 6,
                LineNumberEnd = 9,
                Attributes = new List<SearchModel.Attribute>
                {
                    new SearchModel.Attribute { Condition = false, Name = "l", SearchLinePattern = "^[I][n][f][o]$"},
                    new SearchModel.Attribute { Condition = true, Name = "l", SearchLinePattern = "^[D][e][b][u][g]$"}
                },
            };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[6] },
                ResultsWithNearestLines = new List<LogModel> { _models[5], _models[6], _models[7] },
            };

            var mockSearchLineHandler = _fixture.Freeze<Mock<ISearchLineHandler>>();
            mockSearchLineHandler
                .Setup(handler => handler.ProcessSearchLine(It.IsAny<string>()))
                .Returns(() => option);

            var sut = _fixture.Create<Searcher>();

            // Act
            var result = await sut.SearchInOpenedFile(_models, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }

        [Theory]
        [AutoDomainData]
        public async void SearchInOpenedFile__CancelConverting(Searcher sut)
        {
            // Arrange
            // При запуске теста через Assert.Record вылезает ошибка компилятора
            //  System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[[System.Exception,
            //  System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],
            //  [Xunit.Record+<ExceptionAsync>d__3, xunit.core, Version=2.4.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c]]
            CancellationTokenSource tokenSource = new();
            CancellationToken token = tokenSource.Token;
            tokenSource.CancelAfter(0);
            Exception exception = null;

            // Act
            try
            {
                await sut.SearchInOpenedFile(_models, new SearchModel(), token);
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
            Assert.IsType<OperationCanceledException>(exception);
        }

        #endregion

        #region SearchInFile

        [Theory]
        [InlineAutoData("Error")]
        [InlineAutoData("2022-05-25 00:07:23")]
        [InlineAutoData("AggregateException")]
        public async void SearchInFile_CorrectSimpleSearching(string searchString, string path)
        {
            // Arrange
            var sut = PrepareSUTForSearchInFile();
            var option = new SearchModel { SearchLine = searchString, };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[2] },
                ResultsWithNearestLines = new List<LogModel> { _models[2] },
            };

            //Act
            var result = await sut.SearchInFile(path, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }


        [Theory]
        [AutoData]
        public async void SearchInFile_AdvancedSearch(string path)
        {
            // Arrange
            var sut = PrepareSUTForSearchInFile();

            var option = new SearchModel
            {
                LinesCountBefore = 1,
                LinesCountAfter = 1,
                DateBegin = "2022-05-25 02:01:23",
                DateEnd = "2022-05-25 05:01:23",
                LineNumberStart = 6,
                LineNumberEnd = 9,
                ExcludeLine = "Info",
            };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[6] },
                ResultsWithNearestLines = new List<LogModel> { _models[5], _models[6], _models[7] },
            };

            // Act
            var result = await sut.SearchInFile(path, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }

        [Theory]
        [AutoDomainData]
        public async void SearchInFile_AdvancedSearch_WithAttributes(string path)
        {
            // Arrange
            var option = new SearchModel
            {
                SearchLine = "$l=-\"Info\"",
                LinesCountBefore = 1,
                LinesCountAfter = 1,
                DateBegin = "2022-05-25 02:01:23",
                DateEnd = "2022-05-25 05:01:23",
                LineNumberStart = 6,
                LineNumberEnd = 9,
                Attributes = new List<SearchModel.Attribute>
                {
                    new SearchModel.Attribute { Condition = false, Name = "l", SearchLinePattern = "^[I][n][f][o]$"},
                    new SearchModel.Attribute { Condition = true, Name = "l", SearchLinePattern = "^[D][e][b][u][g]$"}
                },
            };

            var expectedResult = new SearchResult
            {
                ClearResults = new List<LogModel> { _models[6] },
                ResultsWithNearestLines = new List<LogModel> { _models[5], _models[6], _models[7] },
            };

            var mockSearchLineHandler = _fixture.Freeze<Mock<ISearchLineHandler>>();
            mockSearchLineHandler
                .Setup(handler => handler.ProcessSearchLine(It.IsAny<string>()))
                .Returns(() => option);

            var sut = PrepareSUTForSearchInFile();

            // Act
            var result = await sut.SearchInFile(path, option);

            // Assert
            Assert.IsType<SearchResult>(result);
            Assert.True(SearchResultsEquals(expectedResult, result));
        }

        [Theory]
        [AutoDomainData]
        public async void SearchInFile__CancelConverting(string path)
        {
            // Arrange
            // При запуске теста через Assert.Record вылезает ошибка компилятора
            //  System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[[System.Exception,
            //  System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],
            //  [Xunit.Record+<ExceptionAsync>d__3, xunit.core, Version=2.4.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c]]
            using CancellationTokenSource _tokenSource = new();
            CancellationToken _token = _tokenSource.Token;
            _tokenSource.CancelAfter(0);
            Exception exception = null;

            var sut = PrepareSUTForSearchInFile();

            // Act
            try
            {
                await sut.SearchInFile(path, new SearchModel(), _token);
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
            Assert.IsType<OperationCanceledException>(exception);
        }

        #endregion

        private bool SearchResultsEquals(SearchResult expectedResult, SearchResult result)
        {
            if ((expectedResult is null && result is not null) ||
                (expectedResult is not null && result is null))
                return false;

            if (expectedResult is null && result is null)
                return true;

            if (expectedResult.SearchString == result.SearchString)
                if (expectedResult.FilePath == result.FilePath)
                    if (expectedResult.FileName == result.FileName)
                        if (FileHandlerTest.LogModelsEquals(expectedResult.ClearResults, result.ClearResults))
                            if (FileHandlerTest.LogModelsEquals(expectedResult.ResultsWithNearestLines, result.ResultsWithNearestLines))
                                return true;

            return false;
        }

        private Searcher PrepareSUTForSearchInOpenedFile() => _fixture.Create<Searcher>();

        private Searcher PrepareSUTForSearchInFile()
        {
            const string fakeFileContents =
                 "{\"t\":\"2022-05-25 00:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 1\"}\r\n" +
                 "{\"t\":\"2022-05-25 00:04:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 2\"}\r\n" +
                 "{\"t\":\"2022-05-25 00:07:23.456+05:00\",\"l\":\"Error\",\"ex\":{\"type\":\"System.AggregateException\",\"m\":\"Line 3.\",\"stack\":\"System.NullReferenceException.\\r\\n\"}}\r\n" +
                 "{\"t\":\"2022-05-25 01:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 4\"}\r\n" +
                 "{\"t\":\"2022-05-25 02:01:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 5\"}\r\n" +
                 "{\"t\":\"2022-05-25 03:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 6\"}\r\n" +
                 "{\"t\":\"2022-05-25 04:01:23.456+05:00\",\"l\":\"Debug\",\"mt\":\"Line 7\"}\r\n" +
                 "{\"t\":\"2022-05-25 05:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 8\"}\r\n" +
                 "{\"t\":\"2022-05-25 06:01:23.456+05:00\",\"l\":\"Info\",\"mt\":\"Line 9\"}\r\n";

            byte[] fakeFileBytes = Encoding.UTF8.GetBytes(fakeFileContents);
            var fakeMemoryStream = new MemoryStream(fakeFileBytes);

            var mockFileManager = _fixture.Freeze<Mock<IFileManager>>();
            mockFileManager.Setup(fileManager => fileManager.StreamReader(It.IsAny<string>(), It.IsAny<FileStreamOptions>()))
                            .Returns(() => new StreamReader(fakeMemoryStream));

            return _fixture.Create<Searcher>();
        }
    }
}
