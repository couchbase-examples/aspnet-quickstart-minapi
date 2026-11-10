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
}
