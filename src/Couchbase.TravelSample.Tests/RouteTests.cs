using System.Net;
using System.Text;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Couchbase.TravelSample.Tests;

public class RouteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string BaseHostname = "/api/v1/route";

    public RouteTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetRouteByIdTestAsync()
    {
        // Create route
        const string documentId = "route_test_get";
        var route = GetRoute();
        var newRoute = JsonConvert.SerializeObject(route);
        var content = new StringContent(newRoute, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newRouteResult = JsonConvert.DeserializeObject<Route>(jsonResults);

        // Get the route by ID
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultRoute = JsonConvert.DeserializeObject<Route>(getJsonResult);

        // Validate the retrieved route
        if (resultRoute != null)
        {
            Assert.Equal(newRouteResult?.Airline, resultRoute.Airline);
            Assert.Equal(newRouteResult?.SourceAirport, resultRoute.SourceAirport);
            Assert.Equal(newRouteResult?.DestinationAirport, resultRoute.DestinationAirport);
        }

        // Remove route
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestReadInvalidRouteAsync()
    {
        // Arrange
        const string documentId = "route_test_invalid_id";

        // Act
        var response = await _client.GetAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRouteTestAsync()
    {
        // Create route
        const string documentId = "route_test_insert";
        var route = GetRoute();
        var newRoute = JsonConvert.SerializeObject(route);
        var content = new StringContent(newRoute, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newRouteResult = JsonConvert.DeserializeObject<Route>(jsonResults);

        // Validate creation 
        Assert.Equal(route.Airline, newRouteResult?.Airline);
        Assert.Equal(route.SourceAirport, newRouteResult?.SourceAirport);
        Assert.Equal(route.DestinationAirport, newRouteResult?.DestinationAirport);

        // Remove route
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestAddDuplicateRouteAsync()
    {
        // Create the airport
        const string documentId = "route_test_duplicate";
        var route = GetRoute();
        var newRoute = JsonConvert.SerializeObject(route);
        var content = new StringContent(newRoute, Encoding.UTF8, "application/json");
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
    public async Task TestAddRouteWithoutRequiredFieldsAsync()
    {
        // Arrange
        const string documentId = "route_test_invalid_payload";
        var routeData = new RouteCreateRequestCommand
        {
            AirlineId = "airline_sample",
            DestinationAirport = "JFK",
            Stops = 0,
            Equipment = "CRJ",
            Schedule = new List<Schedule>() { new Schedule() { Day = 0, Flight = "SAF123", Utc = "14:05:00" } },
            Distance = 1000.79
        };
        var content = new StringContent(JsonConvert.SerializeObject(routeData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Airline"));
        Assert.Equal("'Airline' must not be empty.", responseData.Errors["Airline"][0]);
        Assert.True(responseData.Errors.ContainsKey("SourceAirport"));
        Assert.Equal("'Source Airport' must not be empty.", responseData.Errors["SourceAirport"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }


    [Fact]
    public async Task UpdateRouteTestAsync()
    {
        // Create route
        const string documentId = "route_test_update";
        var route = GetRoute();
        var newRoute = JsonConvert.SerializeObject(route);
        var content = new StringContent(newRoute, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        var newRouteResult = JsonConvert.DeserializeObject<Route>(jsonResults);

        // Update route
        if (newRouteResult != null)
        {
            UpdateRoute(newRouteResult);
            var updatedRoute = JsonConvert.SerializeObject(newRouteResult);
            content = new StringContent(updatedRoute, Encoding.UTF8, "application/json");
            response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            jsonResults = await response.Content.ReadAsStringAsync();
            var updatedRouteResult = JsonConvert.DeserializeObject<Route>(jsonResults);

            // Validate update
            Assert.Equal(newRouteResult.Airline, updatedRouteResult?.Airline);
            Assert.Equal(newRouteResult.SourceAirport, updatedRouteResult?.SourceAirport);
            Assert.Equal(newRouteResult.DestinationAirport, updatedRouteResult?.DestinationAirport);
        }

        // Remove route
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestUpdateWithInvalidDocumentAsync()
    {
        // Arrange
        const string documentId = "route_test_update_invalid_doc";
        var updatedRouteData = new RouteCreateRequestCommand
        {
            AirlineId = "airline_sample",
            SourceAirport = "SFO",
            DestinationAirport = "JFK",
            Stops = 0,
            Equipment = "CRJ",
            Schedule = new List<Schedule>() { new Schedule() { Day = 0, Flight = "SAF123", Utc = "14:05:00" } },
            Distance = 1000.79
        };
        var content = new StringContent(JsonConvert.SerializeObject(updatedRouteData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"{BaseHostname}/{documentId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
        Assert.True(responseData != null && responseData.Errors.ContainsKey("Airline"));
        Assert.Equal("'Airline' must not be empty.", responseData.Errors["Airline"][0]);
        Assert.Equal("One or more validation errors occurred.", responseData.Title);

        // Check if the document was not created
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }


    [Fact]
    public async Task DeleteRouteTestAsync()
    {
        // Create route
        const string documentId = "route_test_delete";
        var route = GetRoute();
        var newRoute = JsonConvert.SerializeObject(route);
        var content = new StringContent(newRoute, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{BaseHostname}/{documentId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jsonResults = await response.Content.ReadAsStringAsync();
        JsonConvert.DeserializeObject<Route>(jsonResults);

        // Delete route
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Check if the route is no longer accessible
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task TestDeleteNonExistingRouteAsync()
    {
        // Arrange
        const string documentId = "route_non_existent_document";

        // Act
        var response = await _client.DeleteAsync($"{BaseHostname}/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static RouteCreateRequestCommand GetRoute()
    {
        return new RouteCreateRequestCommand()
        {
            Airline = "SAF",
            AirlineId = "airline_sample",
            DestinationAirport = "JFK",
            Distance = 1000.79,
            Equipment = "CRJ",
            Schedule = new List<Schedule>() { new Schedule() { Day = 0, Utc = "14:05:00", Flight = "SAF123"}} ,
            SourceAirport = "SFO",
            Stops = 0
        };
    }
    
    private static void UpdateRoute(Route route)
    {
        route.Airline = "USAF";
        route.AirlineId = "airline_sample_updated";
        route.DestinationAirport = "JFK";
        route.Distance = 1000.79;
        route.Equipment = "CRJ";
        route.Schedule = new List<Schedule>() { new Schedule() { Day = 0, Utc = "14:05:00", Flight = "SAF123" } };
        route.SourceAirport = "SFO";
        route.Stops = 0;
    }
    
}