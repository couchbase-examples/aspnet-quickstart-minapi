using System.Text.Json.Serialization;


namespace Couchbase.TravelSample.Models;

public record RouteCreateRequestCommand
{
    [JsonPropertyName("airline")]
    public string Airline { get; set; } = string.Empty;

    [JsonPropertyName("airlineid")]
    public string AirlineId { get; set; } = string.Empty;

    [JsonPropertyName("destinationairport")]
    public string DestinationAirport { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("equipment")]
    public string Equipment { get; set; } = string.Empty;

    [JsonPropertyName("schedule")]
    public List<Schedule>? Schedule { get; set; }

    [JsonPropertyName("sourceairport")]
    public string SourceAirport { get; set; } = string.Empty;
    
    [JsonPropertyName("stops")]
    public int Stops { get; set; }

    public Route GetRoute()
    {
        return new Route
        {
            Airline = this.Airline,
            AirlineId = this.AirlineId,
            DestinationAirport = this.DestinationAirport,
            Distance = this.Distance,
            Equipment = this.Equipment,
            Schedule = this.Schedule,
            SourceAirport = this.SourceAirport,
            Stops = this.Stops
        };
    }
}