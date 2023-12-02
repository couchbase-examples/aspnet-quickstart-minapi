using System.Text.Json.Serialization;


namespace Couchbase.TravelSample.Models;

public record DestinationAirport
{
    [JsonPropertyName("destinationairport")]
    public string Destinationairport { get; set; } = string.Empty;
}