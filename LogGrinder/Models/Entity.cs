using System.Text.Json.Serialization;

namespace LogGrinder.Models
{
    internal class Entity
    {
        [JsonIgnore]
        public int Id { get; set; }
    }
}
