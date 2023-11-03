using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit;


namespace Couchbase.TravelSample.Tests;

public class AirportTests 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public AirportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAirportListTestAsync()
    {
        // Create query parameters
        const string country = "United States";
        const int limit = 5;
        const int offset = 0;

        // Send an HTTP GET request to the /airport/list endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"/airport/list?country={country}&limit={limit}&offset={offset}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<Airport>>(getJsonResult);

        if (results != null)
        {
            Assert.Equal(5, results.Count);
            Assert.Equal("United States", results[0].Country);
        }
    }
    
    [Fact]
    public async Task GetDirectConnectionsTestAsync()
    {
        // Create query parameters
        const string airport = "SFO";
        const int limit = 5;
        const int offset = 0;

        // Send an HTTP GET request to the /airport/direct-connections endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"/airport/direct-connections?airport={airport}&limit={limit}&offset={offset}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<DestinationAirport>>(getJsonResult);

        if (results != null)
        {
            Assert.Equal(5, results.Count);
            Assert.Equal("ABQ", results[0].Destinationairport);
        }
    }

    [Fact]
    public async Task GetAirportByIdTestAsync()
    {
        // Specify a valid ID
        const string id = "airport_1263";

        // Send an HTTP GET request to the /airport/{id} endpoint
        var getResponse = await _client.GetAsync($"/airport/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultAirport = JsonConvert.DeserializeObject<Airport>(getJsonResult);
        
        if (resultAirport != null) Assert.Equal("France", resultAirport.Country);
    }
    
    [Fact]
    public async Task CreateAirportTestAsync()
    {
        // Define a unique airport ID and create a request object with valid data
        const string id = "airport_001";

        var request = new AirportCreateRequestCommand
        {
            Airportname = "Cazaux",
            City = "Cazaux",
            Country = "France",
            Faa = null,
            Icao = "LFBC",
            Tz = "Europe/Paris",
            Geo = new Geo { Lat = 44.533333, Lon = -1.125, Alt = 84 }
        };

        // Send an HTTP POST request to create the airport
        var postResponse = await _client.PostAsJsonAsync($"/airport/{id}", request);

        // Assert that the HTTP response status code is Created
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var responseContent = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("France", responseContent);
    }
    
    [Fact]
    public async Task UpdateAirportTestAsync()
    {
        // Specify an existing ID and create a request object with updated data
        const string id = "airport_001";

        var request = new AirportCreateRequestCommand
        {
            Airportname = "Updated Airport",
            City = "Updated City",
            Country = "Updated Country",
            Faa = "UPTD",
            Icao = "UPDICAO",
            Tz = "Europe/Paris",
            Geo = new Geo { Lat = 55.123456, Lon = -3.987654, Alt = 100 }
        };

        // Send an HTTP PUT request to update the airport
        var putResponse = await _client.PutAsJsonAsync($"/airport/{id}", request);

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var responseContent = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("Europe", responseContent);
    }
    
    [Fact]
    public async Task DeleteAirportTestAsync()
    {
        // Specify an existing ID to delete
        const string id = "airport_001";

        // Send an HTTP DELETE request to delete the airport
        var deleteResponse = await _client.DeleteAsync($"/airport/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // check if the airport is no longer accessible
        var getResponse = await _client.GetAsync($"/airport/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}