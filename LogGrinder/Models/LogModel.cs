namespace LogGrinder.Models
{
    public class LogModel
    {
        public int Id { get; set; }
        public string? t { get; set; }
        public string? l { get; set; }
        public string? pid { get; set; }
        public string? tab { get; set; }
        public string? mt { get; set; }
        public string? tr { get; set; }
        public string? bn { get; set; }
        public string? bv { get; set; }
        public string? lg { get; set; }
        public string? v { get; set; }
        public string? un { get; set; }
        public string? tn { get; set; }
        public object? args { get; set; }
        public object? cust { get; set; }
        public object? ex { get; set; }
        public object? span { get; set; }
        public string? Other { get; set; }
        public string? RawLine { get; set; }
        public string? FileName { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not LogModel)
                return false;

            return this.Equals(obj as LogModel);
        }

        public bool Equals(LogModel? obj)
        {
            if ((obj is null && this is not null)
                || (obj is not null && this is null))
                return false;
            else if (obj is null && this is null)
                return true;

            return obj.FileName == FileName && obj.Id == Id;
        }
    }
}
