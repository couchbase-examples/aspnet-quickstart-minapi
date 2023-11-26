using System.Net;
using System.Text;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Couchbase.TravelSample.Tests;

public class AirportTests 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string BaseHostname = "/api/v1/airport";

    public AirportTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task TestListAirportsInCountryWithPaginationAsync()
    {
        // Define parameters
        const string country = "France";
        const int pageSize = 3;
        const int iterations = 3;
        var airportsList = new HashSet<string?>();

        for (var i = 0; i < iterations; i++)
        {
            // Send an HTTP GET request to the /airport/list endpoint with the specified query parameters
            var getResponse = await _client.GetAsync($"{BaseHostname}/list?country={country}&limit={pageSize}&offset={pageSize * i}");

            // Assert that the HTTP response status code is OK
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Read the JSON response content and deserialize it
            var getJsonResult = await getResponse.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<Airport>>(getJsonResult);

            if (results == null) continue;
            Assert.Equal(pageSize, results.Count);
            foreach (var airport in results)
            {
                airportsList.Add(airport.Airportname);
                Assert.Equal(country, airport.Country);
            }
        }

        Assert.Equal(pageSize * iterations, airportsList.Count);
    }
    
    [Fact]
    public async Task TestListAirportsInInvalidCountryAsync()
    {
        // Arrange
        const string airportApi = $"{BaseHostname}/list?country=invalid"; 

        // Act
        var response = await _client.GetAsync(airportApi);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDirectConnectionsTestAsync()
    {
        // Create query parameters
        const string airport = "SFO";
        const int limit = 5;
        const int offset = 0;
    
        // Send an HTTP GET request to the /airport/direct-connections endpoint with the specified query parameters
        var getResponse = await _client.GetAsync($"{BaseHostname}/direct-connections?airport={airport}&limit={limit}&offset={offset}");
    
        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    
        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<DestinationAirport>>(getJsonResult);
    
        if (results != null)
        {
            Assert.Equal(limit, results.Count);
        }
    }

    [Fact]
    public async Task TestDirectConnectionsInvalidAirportAsync()
    {
        // Arrange
        const string airportApi = $"{BaseHostname}/direct-connections?airport=invalid";

        // Act
        var response = await _client.GetAsync(airportApi);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAirportByIdTestAsync()
    {
        // Create airport
        const string documentId = "airport_test_get";
        var airport = GetAirport();
        var newAirport = JsonConvert.SerializeObject(airport);
        var content = new StringContent(newAirport, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirportResult = JsonConvert.DeserializeObject<Airport>(jsonResults);

        // Get the airport by ID
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultAirport = JsonConvert.DeserializeObject<Airport>(getJsonResult);

        // Validate the retrieved airport
        if (resultAirport != null)
        {
            Assert.Equal(newAirportResult?.Airportname, resultAirport.Airportname);
            Assert.Equal(newAirportResult?.City, resultAirport.City);
            Assert.Equal(newAirportResult?.Country, resultAirport.Country);
        }

        // Remove airport
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestReadInvalidAirportAsync()
    {
        // Arrange
        const string documentId = "airport_test_invalid_id";

        // Act
        var response = await _client.GetAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAirportTestAsync()
    {
        // Create airport
        const string documentId = "airport_test_insert";
        var airport = GetAirport();
        var newAirport = JsonConvert.SerializeObject(airport);
        var content = new StringContent(newAirport, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirportResult = JsonConvert.DeserializeObject<Airport>(jsonResults);

        // Validate creation 
        Assert.Equal(airport.Airportname, newAirportResult?.Airportname);
        Assert.Equal(airport.City, newAirportResult?.City);
        Assert.Equal(airport.Country, newAirportResult?.Country);

        // Remove airport
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task TestAddDuplicateAirportAsync()
    {
        // Create the airport
        const string documentId = "airport_test_duplicate";
        var airport = GetAirport();
        var newAirport = JsonConvert.SerializeObject(airport);
        var content = new StringContent(newAirport, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Try to create the same airport again
        response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Delete the airport
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestAddAirportWithoutRequiredFieldsAsync()
    {
        // Arrange
        const string documentId = "airport_test_invalid_payload";
        var airportData = new AirportCreateRequestCommand
        {
            City = "Test City",
            Faa = "TAA",
            Tz = "Europe/Berlin"
        };
        var content = new StringContent(JsonConvert.SerializeObject(airportData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Airportname"));
        Assert.Equal("'Airportname' must not be empty.", responseData.Errors["Airportname"][0]);
        Assert.True(responseData.Errors.ContainsKey("Country"));
        Assert.Equal("'Country' must not be empty.", responseData.Errors["Country"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateAirportTestAsync()
    {
        // Create airport
        const string documentId = "airport_test_update";
        var airport = GetAirport();
        var newAirport = JsonConvert.SerializeObject(airport);
        var content = new StringContent(newAirport, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newAirportResult = JsonConvert.DeserializeObject<Airport>(jsonResults);

        // Update airport
        if (newAirportResult != null)
        {
            UpdateAirport(newAirportResult);
            var updatedAirport = JsonConvert.SerializeObject(newAirportResult);
            content = new StringContent(updatedAirport, Encoding.UTF8, "application/json");
            response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonResults = await response.Content.ReadAsStringAsync();
            var updatedAirportResult = JsonConvert.DeserializeObject<Airport>(jsonResults);

            // Validate update
            Assert.Equal(newAirportResult.Airportname, updatedAirportResult?.Airportname);
            Assert.Equal(newAirportResult.City, updatedAirportResult?.City);
            Assert.Equal(newAirportResult.Country, updatedAirportResult?.Country);

            // Remove airport
            var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }
    }
    
    [Fact]
    public async Task TestUpdateWithInvalidDocumentAsync()
    {
        // Arrange
        const string documentId = "airport_test_update_invalid_doc";
        var updatedAirportData = new AirportCreateRequestCommand
        {
            City = "Updated City",
            Country = "Updated Country",
            Faa = "TAA",
            Icao = "TAAS",
            Tz = "Europe/Berlin",
            Geo = new Geo { Lat = 40, Lon = 42, Alt = 100 }
        };
        var content = new StringContent(JsonConvert.SerializeObject(updatedAirportData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Airportname"));
        Assert.Equal("'Airportname' must not be empty.", responseData.Errors["Airportname"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAirportTestAsync()
    {
        // Create airport
        const string documentId = "airport_test_delete";
        var airport = GetAirport();
        var newAirport = JsonConvert.SerializeObject(airport);
        var content = new StringContent(newAirport, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        JsonConvert.DeserializeObject<Airport>(jsonResults);

        // Delete airport
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Check if the airport is no longer accessible
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestDeleteNonExistingAirportAsync()
    {
        // Arrange
        const string documentId = "airport_non_existent_document";

        // Act
        var response = await _client.DeleteAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static AirportCreateRequestCommand GetAirport()
    {
        return new AirportCreateRequestCommand()
        {
            Airportname = "Test Airport",
            City = "Test City",
            Country = "Test Country",
            Faa = "TAA",
            Icao = "TAAS",
            Tz = "Europe/Berlin",
            Geo = new Geo { Lat = 40, Lon = 42, Alt = 100 }
        };
    }
    
    private static void UpdateAirport(Airport airport)
    {
        airport.Airportname = "Updated Airport";
        airport.City = "Updated City";
        airport.Country = "Updated Country";
        airport.Faa = "UPTD";
        airport.Icao = "UPDICAO";
        airport.Tz = "Europe/Paris";
        airport.Geo = new Geo { Lat = 55.123456, Lon = -3.987654, Alt = 100 };
    }
}