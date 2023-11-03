using System.Net;
using System.Net.Http.Json;
using Couchbase.TravelSample.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Couchbase.TravelSample.Tests;

public class RouteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public RouteTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetRouteByIdTestAsync()
    {
        // Specify a valid ID
        const string id = "route_10000";

        // Send an HTTP GET request to the /route/{id} endpoint
        var getResponse = await _client.GetAsync($"/route/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Read the JSON response content and deserialize it
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var resultRoute = JsonConvert.DeserializeObject<Route>(getJsonResult);
        
        if (resultRoute != null) Assert.Equal("AF", resultRoute.Airline);
    }
    
    [Fact]
    public async Task CreateRouteTestAsync()
    {
        // Define a unique ID and create a request object with valid data
        const string id = "route_001";

        var request = new RouteCreateRequestCommand
        {
            Airline = "AF",
            AirlineId = "airline_137",
            SourceAirport = "TLV",
            DestinationAirport = "MRS",
            Stops = 0,
            Equipment = "320",
            Schedule = new List<Schedule>
            {
                new Schedule { Day = 0, Utc = "10:13:00", Flight = "AF198" },
                new Schedule { Day = 0, Utc = "19:14:00", Flight = "AF547" },
                new Schedule { Day = 0, Utc = "01:31:00", Flight = "AF943" },
                new Schedule { Day = 1, Utc = "12:40:00", Flight = "AF356" }
            },
            Distance = 2881.617376098415
        };


        // Send an HTTP POST request to create the route
        var postResponse = await _client.PostAsJsonAsync($"/route/{id}", request);

        // Assert that the HTTP response status code is Created
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var responseContent = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("AF", responseContent);
    }
    
    [Fact]
    public async Task UpdateAirlineTestAsync()
    {
        // Specify an existing ID and create a request object with updated data
        const string id = "route_001";

        var request = new RouteCreateRequestCommand()
        {
            Airline = "AF",
            AirlineId = "UpdatedId",
            SourceAirport = "Updated airport",
            DestinationAirport = "Updated destination airport",
            Stops = 5,
            Equipment = "100",
            Schedule = new List<Schedule>
            {
                new Schedule { Day = 1, Utc = "19:13:00", Flight = "AF198" },
                new Schedule { Day = 2, Utc = "10:14:00", Flight = "AF547" },
                new Schedule { Day = 0, Utc = "02:31:00", Flight = "AF943" },
                new Schedule { Day = 1, Utc = "11:40:00", Flight = "AF356" }
            },
            Distance = 881.617376098415
        };

        // Send an HTTP PUT request to update the route
        var putResponse = await _client.PutAsJsonAsync($"/route/{id}", request);

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var responseContent = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("AF", responseContent);
    }
    
    [Fact]
    public async Task DeleteAirlineTestAsync()
    {
        // Specify an existing ID to delete
        const string id = "route_001";

        // Send an HTTP DELETE request to delete the route
        var deleteResponse = await _client.DeleteAsync($"/route/{id}");

        // Assert that the HTTP response status code is OK
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // check if the route is no longer accessible
        var getResponse = await _client.GetAsync($"/route/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
}