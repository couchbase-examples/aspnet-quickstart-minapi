using System.Text.Json.Serialization;

namespace Couchbase.TravelSample.Models;

public record Geo
{
    [JsonPropertyName("alt")]
    public double Alt { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}