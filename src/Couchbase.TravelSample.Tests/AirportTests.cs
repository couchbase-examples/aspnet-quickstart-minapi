using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
    private readonly string baseHostname = "/airports";
    private readonly string baseHostnameSearch = "/airports";
    
    public AirportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    
    [Fact]
    public async Task GetAirportSearchTestAsync()
    {
        //get the user from the main API
        var getResponse = await _client.GetAsync($"{baseHostnameSearch}?country=United%20States&skip=0&limit=5");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getJsonResult = await getResponse.Content.ReadAsStringAsync();
        var results = JsonConvert.DeserializeObject<List<Airport>>(getJsonResult);

        Assert.Equal(5, results.Count);
        Assert.Equal("United States", results[0].Country);
    }
}