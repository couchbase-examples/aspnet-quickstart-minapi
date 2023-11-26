using System.Text.Json.Serialization;


namespace Couchbase.TravelSample.Models;

public abstract record DestinationAirport
{
    [JsonPropertyName("destinationairport")]
    public string Destinationairport { get; set; } = string.Empty;
}