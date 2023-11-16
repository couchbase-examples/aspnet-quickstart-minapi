using System.Net;
using System.Net.Http.Json;
using System.Text;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Couchbase.TravelSample.Tests;

public class AirlineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string BaseHostname = "/api/v1/airline";

    public AirlineTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestListAirlinesInCountryWithPaginationAsync()
    {
        // Define parameters
        const string country = "United States";
        const int pageSize = 3;
        const int iterations = 3;
        var airlinesList = new HashSet<string>();

        for (var i = 0; i < iterations; i++)
        {
            // Send an HTTP GET request to the /airline/list endpoint with the specified query parameters
            var getResponse = await _client.GetAsync($"{BaseHostname}/list?country={country}&limit={pageSize}&offset={pageSize * i}");

            // Assert that the HTTP response status code is OK
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Read the JSON response content and deserialize it
            var getJsonResult = await getResponse.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<Airline>>(getJsonResult);

            if (results == null) continue;
            Assert.Equal(pageSize, results.Count);
            foreach (var airline in results)
            {
                airlinesList.Add(airline.Name);
                Assert.Equal(country, airline.Country);
            }
        }

        Assert.Equal(pageSize * iterations, airlinesList.Count);
    }

    
    [Fact]
    public async Task GetToAirportTestAsync()
    {
        // Create query parameters
        const string airport = "SFO";
        const int limit = 5;
        const int offset = 0;

        // Send an HTTP GET request to the /airline/to-airport endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"{BaseHostname}/to-airport?airport={airport}&limit={limit}&offset={offset}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<Airline>>(getJsonResult);

        if (results != null)
        {
            Assert.Equal(limit, results.Count);
        }
    }

    [Fact]
    public async Task GetAirlineByIdTestAsync()
    {
        // Create airline
        const string documentId = "airline_test_get";
        var airline = GetAirline();
        var newAirline = JsonConvert.SerializeObject(airline);
        var content = new StringContent(newAirline, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirlineResult = JsonConvert.DeserializeObject<Airline>(jsonResults);

        // Get the airline by ID
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultAirline = JsonConvert.DeserializeObject<Airline>(getJsonResult);

        // Validate the retrieved airline
        if (resultAirline != null)
        {
            Assert.Equal(newAirlineResult?.Name, resultAirline.Name);
            Assert.Equal(newAirlineResult?.Country, resultAirline.Country);
            Assert.Equal(newAirlineResult?.Icao, resultAirline.Icao);
        }

        // Remove airline
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    
    [Fact]
    public async Task CreateAirlineTestAsync()
    {
        // Create airline
        const string documentId = "airline_test_insert";
        var airline = GetAirline();
        var newAirline = JsonConvert.SerializeObject(airline);
        var content = new StringContent(newAirline, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirlineResult = JsonConvert.DeserializeObject<Airline>(jsonResults);

        // Validate creation 
        Assert.Equal(airline.Name, newAirlineResult?.Name);
        Assert.Equal(airline.Country, newAirlineResult?.Country);

        // Remove airline
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateAirlineTestAsync()
    {
        // Create airline
        const string documentId = "airline_test_update";
        var airline = GetAirline();
        var newAirline = JsonConvert.SerializeObject(airline);
        var content = new StringContent(newAirline, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirlineResult = JsonConvert.DeserializeObject<Airline>(jsonResults);

        // Update airline
        if (newAirlineResult != null)
        {
            UpdateAirline(newAirlineResult);
            var updatedAirline = JsonConvert.SerializeObject(newAirlineResult);
            content = new StringContent(updatedAirline, Encoding.UTF8, "application/json");
            response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonResults = await response.Content.ReadAsStringAsync();
            var updatedAirlineResult = JsonConvert.DeserializeObject<Airline>(jsonResults);

            // Validate update
            Assert.Equal(newAirlineResult.Name, updatedAirlineResult?.Name);
            Assert.Equal(newAirlineResult.Country, updatedAirlineResult?.Country);
            Assert.Equal(newAirlineResult.Icao, updatedAirlineResult?.Icao);
        }

        // Remove airline
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAirlineTestAsync()
    {
        // Create airline
        const string documentId = "airline_test_delete";
        var airline = GetAirline();
        var newAirline = JsonConvert.SerializeObject(airline);
        var content = new StringContent(newAirline, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirlineResult = JsonConvert.DeserializeObject<Airline>(jsonResults);

        // Delete airline
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Check if the airline is no longer accessible
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private static AirlineCreateRequestCommand GetAirline()
    {
        return new AirlineCreateRequestCommand()
        {
            Callsign = "SAM",
            Country = "Sample Country",
            Iata = "SAL",
            Icao = "SALL",
            Name = "Sample Airline"
        };
    }
    
    private static void UpdateAirline(Airline airline)
    {
        airline.Callsign = "SAM";
        airline.Country = "Updated Country";
        airline.Iata = "SAL";
        airline.Icao = "SALL";
        airline.Name = "Updated Airline";
    }
}