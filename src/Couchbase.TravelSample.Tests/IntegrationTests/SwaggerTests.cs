using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;

namespace Couchbase.TravelSample.Tests;

public class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestSwaggerDocumentVersionIsAcceptedBySwaggerUiAsync()
    {
        var docResponse = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, docResponse.StatusCode);

        var document = JObject.Parse(await docResponse.Content.ReadAsStringAsync());
        var openApiVersion = document.Value<string>("openapi");
        Assert.False(string.IsNullOrEmpty(openApiVersion));
        Assert.NotEmpty(document["paths"]!.Children());

        var uiResponse = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);

        // Swagger UI refuses to render ("does not specify a valid version field") unless the
        // document's "openapi" value matches the OpenAPI 3.0 version check in its bundle.
        var bundle = await _client.GetStringAsync("/swagger/swagger-ui-bundle.js");
        var versionChecks = Regex.Matches(bundle, @"/(\^3\\\.0\\\.[^/\n]{1,40}\$)/")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.NotEmpty(versionChecks);
        Assert.Contains(versionChecks, pattern => Regex.IsMatch(openApiVersion!, pattern));
    }

    [Fact]
    public async Task TestSwaggerDocumentIncludesEndpointDocumentationAsync()
    {
        var document = JObject.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));

        // Every endpoint documents its operation, parameters, and responses.
        foreach (var path in document["paths"]!.Children<JProperty>())
        {
            foreach (var operation in path.Value.Children<JProperty>())
            {
                var name = $"{operation.Name.ToUpperInvariant()} {path.Name}";
                Assert.False(string.IsNullOrEmpty(operation.Value.Value<string>("description")), $"{name} has no description");
                foreach (var parameter in operation.Value["parameters"]!)
                {
                    Assert.False(string.IsNullOrEmpty(parameter.Value<string>("description")),
                        $"{name} parameter '{parameter.Value<string>("name")}' has no description");
                }
                Assert.Contains(operation.Value["responses"]!.Children<JProperty>(), r => r.Name == "500");
            }
        }

        var listAirports = document["paths"]!["/api/v1/airport/list"]!["get"]!;
        Assert.StartsWith("Get list of Airports.", listAirports.Value<string>("description"));
        var country = listAirports["parameters"]!.Single(p => p.Value<string>("name") == "country");
        Assert.Equal("query", country.Value<string>("in"));
        Assert.Equal("Country (Example: United Kingdom, France, United States)", country.Value<string>("description"));
        Assert.Equal("List of airports", listAirports["responses"]!["200"]!.Value<string>("description"));
        Assert.Equal("Unexpected Error", listAirports["responses"]!["500"]!.Value<string>("description"));

        var createAirport = document["paths"]!["/api/v1/airport/{id}"]!["post"]!;
        Assert.NotNull(createAirport["requestBody"]);
        Assert.Equal("Airport already exists", createAirport["responses"]!["409"]!.Value<string>("description"));
    }
}
