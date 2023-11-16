using System.Net;
using System.Net.Http.Json;
using System.Text;
using Couchbase.TravelSample.Models;
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
        var newRouteResult = JsonConvert.DeserializeObject<Route>(jsonResults);

        // Delete route
        var deleteResponse = await _client.DeleteAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Check if the route is no longer accessible
        var getResponse = await _client.GetAsync($"{BaseHostname}/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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