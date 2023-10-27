using System.Text.Json.Serialization;

namespace Couchbase.TravelSample.Models;

public record Airport
{
    [JsonPropertyName("airportname")]
    public string Airportname { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("faa")]
    public string Faa { get; set; } = string.Empty;

    [JsonPropertyName("icao")]
    public string Icao { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type => "airport";
}