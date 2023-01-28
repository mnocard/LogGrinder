namespace LogGrinder.Tests.Services
{
    public class SearchLineHandlerTest
    {
        private readonly Fixture _fixture;
        private readonly ISearchLineHandler _sut;
        private readonly SearchModel _expectedSearchModel;

        public SearchLineHandlerTest()
        {
            _fixture = new Fixture();
            _fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            _sut = _fixture.Create<ISearchLineHandler>();
            _expectedSearchModel = new SearchModel();
        }

        [Fact]
        public void ProcessSearchLine_ArgumentException_StringIsEmpty()
        {
            // Arrange
            const string exceptionText = "Строка не соответствует шаблону.";
            const string line = "Random line";

            // Act
            Action act = () => _sut.ProcessSearchLine(line);
            var exception = Record.Exception(act);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }

        [Fact]
        public void ProcessSearchLine_ArgumentException_EscapeQuotes()
        {
            // Arrange
            const string exceptionText = "Строка содержит неэкраннированную двойную кавычку.";
            const string line = "$mt=\"Quote \" line\"";

            // Act
            Action act = () => _sut.ProcessSearchLine(line);
            var exception = Record.Exception(act);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }


        [Fact]
        public void ProcessSearchLine_2DifferentUnitedAttributes()
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "t", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
            });

            const string line = "$mt$t=\"Random line\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_2SameSplittedAttributes()
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=\"Second line\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_ExcludeAttributes()
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = false, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=-\"Second line\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_AsteriskAttributes()
        {
            // Arrange
            _expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m].+[l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random*line*\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_EscapeAsteriskAttributes()
        {
            // Arrange
            _expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m][*][l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random**line*\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_CustomAttributes()
        {
            // Arrange
            _expectedSearchModel.LineNumberStart = 1;
            _expectedSearchModel.LineNumberEnd = 2;
            _expectedSearchModel.LinesCountBefore = 3;
            _expectedSearchModel.LinesCountAfter = 4;
            _expectedSearchModel.DateBegin = "2022-02-22";
            _expectedSearchModel.DateEnd = "2022-03-23";
            _expectedSearchModel.Attributes.Add(new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" });

            const string line = "$mt=\"Random line\" $lns=\"1\" $lne=\"2\" $lcb=\"3\" $lca=\"4\" $db=\"2022-02-22\" $de=\"2022-03-23\"";

            // Act
            var result = _sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        public bool SearchModelsEquals(SearchModel first, SearchModel second)
        {
            if ((first is null && second is not null) ||
                (first is not null && second is null))
                return false;

            if (first is null && second is null)
                return true;

            if (first.SearchLine == second.SearchLine &&
                first.ExcludeLine == second.ExcludeLine &&
                first.LineNumberStart == second.LineNumberStart &&
                first.LineNumberEnd == second.LineNumberEnd &&
                first.DateBegin == second.DateBegin &&
                first.DateEnd == second.DateEnd &&
                first.LinesCountBefore == second.LinesCountBefore &&
                first.LinesCountAfter == second.LinesCountAfter &&
                first.Attributes.Count == second.Attributes.Count)
            {
                if (first.Attributes.Count > 0)
                {
                    var result = false;
                    for (int i = 0; i < first.Attributes.Count; i++)
                    {
                        result =
                            first.Attributes[i].Name == second.Attributes[i].Name &&
                            first.Attributes[i].Condition == second.Attributes[i].Condition &&
                            first.Attributes[i].SearchLinePattern == second.Attributes[i].SearchLinePattern;
                    }
                    return result;
                }
                else
                    return true;
            }
            else return false;
        }
    }
}
