using System.Net;
using System.Net.Http.Json;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Couchbase.TravelSample.Tests;

public class AirlineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public AirlineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAirlineListTestAsync()
    {
        // Create query parameters
        const string country = "United States";
        const int limit = 5;
        const int offset = 0;

        // Send an HTTP GET request to the /airline/list endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"/airline/list?country={country}&limit={limit}&offset={offset}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<Airline>>(getJsonResult);

        if (results != null)
        {
            Assert.Equal(5, results.Count);
            Assert.Equal("United States", results[0].Country);
        }
    }
    
    [Fact]
    public async Task GetToAirportTestAsync()
    {
        // Create query parameters
        const string airport = "SFO";
        const int limit = 5;
        const int offset = 0;

        // Send an HTTP GET request to the /airline/to-airport endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"/airline/to-airport?airport={airport}&limit={limit}&offset={offset}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<Airline>>(getJsonResult);

        if (results != null)
        {
            Assert.Equal(5, results.Count);
            Assert.Equal("United States", results[0].Country);
        }
    }

    [Fact]
    public async Task GetAirlineByIdTestAsync()
    {
        // Specify a valid ID
        const string id = "airline_109";

        // Send an HTTP GET request to the /airline/{id} endpoint
        var getResponse = await _client.GetAsync($"/airline/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultAirline = JsonConvert.DeserializeObject<Airline>(getJsonResult);
        
        if (resultAirline != null) Assert.Equal("United States", resultAirline.Country);
    }
    
    [Fact]
    public async Task CreateAirlineTestAsync()
    {
        // Define a unique ID and create a request object with valid data
        const string id = "airline_001";

        var request = new AirlineCreateRequestCommand()
        {
            Name = "Alaska Central Express",
            Iata = "KO",
            Icao = "AER",
            Callsign = "ACE AIR",
            Country = "United States"
        };

        // Send an HTTP POST request to create the airline
        var postResponse = await _client.PostAsJsonAsync($"/airline/{id}", request);

        // Assert that the HTTP response status code is Created
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var responseContent = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("United States", responseContent);
    }
    
    [Fact]
    public async Task UpdateAirlineTestAsync()
    {
        // Specify an existing ID and create a request object with updated data
        const string id = "airline_001";

        var request = new AirlineCreateRequestCommand()
        {
            Name = "Updated Name",
            Iata = "Updated Iata",
            Icao = "Updated Icao",
            Callsign = "Updated Callsign",
            Country = "United States"
        };

        // Send an HTTP PUT request to update the airline
        var putResponse = await _client.PutAsJsonAsync($"/airline/{id}", request);

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var responseContent = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("United States", responseContent);
    }
    
    [Fact]
    public async Task DeleteAirlineTestAsync()
    {
        // Specify an existing ID to delete
        const string id = "airline_001";

        // Send an HTTP DELETE request to delete the airline
        var deleteResponse = await _client.DeleteAsync($"/airline/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // check if the airline is no longer accessible
        var getResponse = await _client.GetAsync($"/airline/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
}