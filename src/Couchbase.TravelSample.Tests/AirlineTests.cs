using System.Net;
using System.Text;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc;
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
        var airlinesList = new List<string>();

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
    public async Task TestListAirlinesInInvalidCountryAsync()
    {
        // Arrange
        const string airlineAPi = $"{BaseHostname}/list?country=invalid"; 

        // Act
        var response = await _client.GetAsync(airlineAPi);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    public async Task TestToAirportInvalidAirportAsync()
    {
        // Arrange
        const string airlineApi = $"{BaseHostname}/to-airport?airport=invalid";

        // Act
        var response = await _client.GetAsync(airlineApi);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    public async Task TestReadInvalidAirlineAsync()
    {
        // Arrange
        const string documentId = "airline_test_invalid_id";

        // Act
        var response = await _client.GetAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    public async Task TestAddDuplicateAirlineAsync()
    {
        // Create the airport
        const string documentId = "airline_test_duplicate";
        var airline = GetAirline();
        var newAirline = JsonConvert.SerializeObject(airline);
        var content = new StringContent(newAirline, Encoding.UTF8, "application/json");
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
    public async Task TestAddAirlineWithoutRequiredFieldsAsync()
    {
        // Arrange
        const string documentId = "airline_test_invalid_payload";
        var airlineData = new AirlineCreateRequestCommand
        {
            Iata = "SAL",
            Icao = "SALL",
            Country = "Sample Country"
        };
        var content = new StringContent(JsonConvert.SerializeObject(airlineData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Name"));
        Assert.Equal("'Name' must not be empty.", responseData.Errors["Name"][0]);
        Assert.True(responseData.Errors.ContainsKey("Callsign"));
        Assert.Equal("'Callsign' must not be empty.", responseData.Errors["Callsign"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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
    public async Task TestUpdateWithInvalidDocumentAsync()
    {
        // Arrange
        const string documentId = "airline_test_update_invalid_doc";
        var updatedAirlineData = new AirlineCreateRequestCommand
        {
            Iata = "SAL",
            Icao = "SALL",
            Callsign = "SAM",
            Country = "Updated Country"
        };
        var content = new StringContent(JsonConvert.SerializeObject(updatedAirlineData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Name"));
        Assert.Equal("'Name' must not be empty.", responseData.Errors["Name"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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
        JsonConvert.DeserializeObject<Airline>(jsonResults);

        // Delete airline
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Check if the airline is no longer accessible
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestDeleteNonExistingAirlineAsync()
    {
        // Arrange
        const string documentId = "airline_non_existent_document";

        // Act
        var response = await _client.DeleteAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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