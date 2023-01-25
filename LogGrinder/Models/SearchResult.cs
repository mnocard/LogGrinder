using System.Collections.Generic;

namespace LogGrinder.Models
{
    internal class SearchResult : Entity
    {
        private const string ColonWithSpace = ": ";
        private const string SearchViewConst = "{0}{3}{1}{3}результатов - {2}.";

        public string SearchString { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public List<LogModel> ClearResults { get; set; } = new List<LogModel>();
        public List<LogModel> ResultsWithNearestLines { get; set; } = new List<LogModel>();
        public override string ToString()
        {
            return string.Format(
                        SearchViewConst,
                        SearchString,
                        FileName,
                        ClearResults.Count,
                        ColonWithSpace);
        }
    }
}
