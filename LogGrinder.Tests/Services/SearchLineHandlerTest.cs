namespace LogGrinder.Tests.Services
{
    public class SearchLineHandlerTest
    {
        [Fact]
        public void ProcessSearchLine_ArgumentException_StringIsEmpty()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

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


        [Fact]
        public void ProcessSearchLine_ArgumentException_EscapeQuotes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

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


        [Fact]
        public void ProcessSearchLine_2DifferentUnitedAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel();
            expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "t", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
            });

            const string line = "$mt$t=\"Random line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_2SameSplittedAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel();
            expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=\"Second line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_ExcludeAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel();
            expectedSearchModel.Attributes.AddRange(new[] {
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[F][i][r][s][t][ ][l][i][n][e]$" },
                new SearchModel.Attribute { Condition = false, Name = "mt", SearchLinePattern = "^[S][e][c][o][n][d][ ][l][i][n][e]$" },
            });

            const string line = "$mt=\"First line\" $mt=-\"Second line\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_AsteriskAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel();
            expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m].+[l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random*line*\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_EscapeAsteriskAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel();
            expectedSearchModel.Attributes.Add(
                new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = ".+[R][a][n][d][o][m][*][l][i][n][e].+" }
            );

            const string line = "$mt=\"*Random**line*\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
        }

        [Fact]
        public void ProcessSearchLine_CustomAttributes()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register<ISearchLineHandler>(() => new SearchLineHandler());
            var sut = fixture.Create<ISearchLineHandler>();

            var expectedSearchModel = new SearchModel
            {
                LineNumberStart = 1,
                LineNumberEnd = 2,
                LinesCountBefore = 3,
                LinesCountAfter = 4,
                DateBegin = "2022-02-22",
                DateEnd = "2022-03-23",
                Attributes = new List<SearchModel.Attribute>
                {
                    new SearchModel.Attribute { Condition = true, Name = "mt", SearchLinePattern = "^[R][a][n][d][o][m][ ][l][i][n][e]$" },
                }
            };

            const string line = "$mt=\"Random line\" $lns=\"1\" $lne=\"2\" $lcb=\"3\" $lca=\"4\" $db=\"2022-02-22\" $de=\"2022-03-23\"";

            // Act
            var result = sut.ProcessSearchLine(line);

            // Assert
            Assert.IsType<SearchModel>(result);
            Assert.NotNull(result);
            Assert.True(SearchModelsEquals(result, expectedSearchModel));
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
