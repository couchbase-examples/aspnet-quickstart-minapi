using System.Text.Json.Serialization;

namespace Couchbase.TravelSample.Models;

public record BookFlightModel
{
    [JsonPropertyName("flights")] public List<Flight> Flights { get; set; } = new ();   
}