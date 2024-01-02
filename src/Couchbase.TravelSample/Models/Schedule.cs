using System.Text.Json.Serialization;


namespace Couchbase.TravelSample.Models;

public record Schedule
{
    [JsonPropertyName("day")]
    public int Day { get; set; }
    
    [JsonPropertyName("flight")]
    public string Flight { get; set; } = string.Empty;
    
    [JsonPropertyName("utc")]
    public string Utc { get; set; } = string.Empty;
}