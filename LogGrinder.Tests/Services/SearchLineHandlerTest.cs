namespace LogGrinder.Tests.Services
{
    public class SearchLineHandlerTest
    {
        private readonly SearchModel _expectedSearchModel = new SearchModel();

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_StringIsEmpty_ArgumentException(SearchLineHandler sut)
        {
            // Arrange
            const string exceptionText = "Строка не соответствует шаблону.";
            const string line = "Random line";

            // Act
            Action act = () => sut.ProcessSearchLine(line);
            var exception = Record.Exception(act);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_EscapeQuotes_ArgumentException(SearchLineHandler sut)
        {
            // Arrange
            const string exceptionText = "Строка содержит неэкраннированную двойную кавычку.";
            const string line = "$mt=\"Quote \" line\"";

            // Act
            Action act = () => sut.ProcessSearchLine(line);
            var exception = Record.Exception(act);

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(exceptionText, exception.Message);
        }


        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_2DifferentUnitedAttributes(SearchLineHandler sut)
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "t", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
            });

            const string line = "$mt$t=\"Random line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_2SameSplittedAttributes(SearchLineHandler sut)
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=\"Second line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_ExcludeAttributes(SearchLineHandler sut)
        {
            // Arrange
            _expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = false, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=-\"Second line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_AsteriskAttributes(SearchLineHandler sut)
        {
            // Arrange
            _expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m].+[l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random*line*\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_EscapeAsteriskAttributes(SearchLineHandler sut)
        {
            // Arrange
            _expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m][*][l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random**line*\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, _expectedSearchModel));
        }

        [Theory]
        [AutoDomainData]
        public void ProcessSearchLine_CustomAttributes(SearchLineHandler sut)
        {
            // Arrange
            var fixture = new Fixture();
            _expectedSearchModel.LineNumberStart = fixture.Create<int>();
            _expectedSearchModel.LineNumberEnd = fixture.Create<int>();
            _expectedSearchModel.LinesCountBefore = fixture.Create<int>();
            _expectedSearchModel.LinesCountAfter = fixture.Create<int>();
            _expectedSearchModel.DateBegin = fixture.Create<DateTime>().ToString();
            _expectedSearchModel.DateEnd = fixture.Create<DateTime>().ToString();
            _expectedSearchModel.Attributes.Add(new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" });

            var line = string.Format(
                "$mt=\"Random line\" $lns=\"{0}\" $lne=\"{1}\" $lcb=\"{2}\" $lca=\"{3}\" $db=\"{4}\" $de=\"{5}\"",
                _expectedSearchModel.LineNumberStart,
                _expectedSearchModel.LineNumberEnd,
                _expectedSearchModel.LinesCountBefore,
                _expectedSearchModel.LinesCountAfter,
                _expectedSearchModel.DateBegin,
                _expectedSearchModel.DateEnd);

            // Act
            var result = sut.ProcessSearchLine(line);

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
