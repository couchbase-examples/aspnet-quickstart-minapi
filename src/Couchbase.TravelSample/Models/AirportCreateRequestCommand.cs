namespace Couchbase.TravelSample.Models;

public record AirportCreateRequestCommand
{
    public string Airportname { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Faa { get; set; } = string.Empty;
    
    public Geo? Geo { get; set; }

    public string Icao { get; set; } = string.Empty;
    
    public string Tz { get; set; }

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