using System.Text.Json.Serialization;

namespace Couchbase.TravelSample.Models;

public record AirportCreateRequestCommand
{
    [JsonPropertyName("airportname")] 
    public string Airportname { get; set; } = string.Empty;

    [JsonPropertyName("city")] 
    public string City { get; set; } = string.Empty;
    
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("faa")] 
    public string Faa { get; set; } = string.Empty;

    [JsonPropertyName("geo")] 
    public Geo? Geo { get; set; }
    
    [JsonPropertyName("icao")]
    public string Icao { get; set; } = string.Empty;

    [JsonPropertyName("tz")] 
    public string Tz { get; set; } = string.Empty;

    public Airport GetAirport ()
    {
        return new Airport()
        {
            Airportname = this.Airportname,
            City = this.City,
            Country = this.Country,
            Faa = this.Faa,
            Geo = this.Geo,
            Icao = this.Icao,
            Tz = this.Tz
        };
    }
}