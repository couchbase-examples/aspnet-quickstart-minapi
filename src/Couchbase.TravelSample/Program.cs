using Microsoft.Extensions.Options;

using Couchbase.Extensions.DependencyInjection;
using Couchbase.TravelSample.Models;
using Route = Couchbase.TravelSample.Models.Route;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// dev origins used to fix CORS for local dev/qa debugging of site
/// </summary>
const string _devSpecificOriginsName = "_devAllowSpecificOrigins";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//register the configuration for Couchbase and Dependency Injection Framework
builder.Services.Configure<CouchbaseConfig>(builder.Configuration.GetSection("Couchbase"));
builder.Services.AddCouchbase(builder.Configuration.GetSection("Couchbase"));
builder.Services.AddHttpClient();

//fix for debugging dev and qa environments in Github 
//DO NOT APPLY to UAT or Production Environments!!!
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: _devSpecificOriginsName,
        builder =>
        {
            builder.WithOrigins("https://*.github.com",
                    "http://localhost:5000",
                    "http://localhost:8080",
                    "https://localhost:5001")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
/*
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
*/

if (app.Environment.EnvironmentName == "Testing")
{
    app.UseCors(_devSpecificOriginsName);
}

//remove couchbase from memory when ASP.NET closes
app.Lifetime.ApplicationStopped.Register(() =>
{
    var cls = app.Services.GetRequiredService<ICouchbaseLifetimeService>();
    if (cls != null)
    {
        cls.Close();
    }
});

app.UseHttpsRedirection();

app.MapGet("/airport/list", async (string? country, int? limit, int? offset, IClusterProvider clusterProvider,
    IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value; 

        //get the cluster provider to run a query from
        var cluster = await clusterProvider.GetClusterAsync();

        var query = $@"SELECT airport.airportname,
                          airport.city,
                          airport.country,
                          airport.faa,
                          airport.geo,
                          airport.icao,
                          airport.tz
             FROM `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airport` AS airport
             WHERE lower(airport.country) = $country
             ORDER BY airport.airportname
             LIMIT $limit
             OFFSET $offset";

        //setup parameters
        var queryParameters = new Couchbase.Query.QueryOptions();
        if (country != null) queryParameters.Parameter("country", country.ToLower());
        if (limit != null) queryParameters.Parameter("limit", limit);
        if (offset != null) queryParameters.Parameter("offset", offset);

        var results = await cluster.QueryAsync<Airport>(query, queryParameters);
        var items = await results.Rows.ToListAsync<Airport>();

        if (items.Count == 0)
            return Results.NotFound();

        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();
});

app.MapGet("/airport/direct-connections", async (string? airport, int? limit, int? offset, IClusterProvider clusterProvider,
    IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the cluster provider to run a query from
        var cluster = await clusterProvider.GetClusterAsync();

        var query = $@"SELECT DISTINCT route.destinationairport
             FROM `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airport` AS airport
             JOIN `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`route` AS route
             ON route.sourceairport = airport.faa
             WHERE lower(airport.faa) = $airport AND route.stops = 0
             ORDER BY route.destinationairport
             LIMIT $limit
             OFFSET $offset";
        
        //setup parameters
        var queryParameters = new Couchbase.Query.QueryOptions();
        if (airport != null) queryParameters.Parameter("airport", airport.ToLower());
        if (limit != null) queryParameters.Parameter("limit", limit);
        if (offset != null) queryParameters.Parameter("offset", offset);

        var results = await cluster.QueryAsync<DestinationAirport>(query, queryParameters);
        var items = await results.Rows.ToListAsync<DestinationAirport>();

        if (items.Count == 0)
            return Results.NotFound();

        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();
});


app.MapGet("/airport/{id}", async (string id, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("airport");

        //get the document from the bucket using the id
        var result = await collection.GetAsync(id);

        //validate we have a document
        var resultAirports = result.ContentAs<Airport>();
        if (resultAirports != null)
        {
            return Results.Ok(resultAirports);
        }
    }
    catch (Couchbase.Core.Exceptions.KeyValue.DocumentNotFoundException)
    {
        Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();

});

app.MapPost("/airport/{id}",
    async (string id, AirportCreateRequestCommand request, IBucketProvider bucketProvider,
        IOptions<CouchbaseConfig> options) =>
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("airport");

        //get airport from request
        var airport = request.GetAirport();

        //save document
        await collection.InsertAsync(id, airport);
        return Results.Created($"/airport/{id}", airport);
    });

app.MapPut("/airport/{id}", async (string? id,AirportCreateRequestCommand request, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("airport");

    //get current airport from the database
    var result = await collection.GetAsync(id);
    if (result != null)
    {
        var airport = result.ContentAs<Airport>();
        var updateResult = await collection.ReplaceAsync<Airport>(id, request.GetAirport());

        return Results.Ok(request);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapDelete("airport/{id}", async(string id, IBucketProvider bucketProvider, IOptions < CouchbaseConfig > options) => 
{

    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("airport");

    //get the docment from the bucket using the id
    var result = await collection.GetAsync(id);

    //validate we have a document
    var resultAirport = result.ContentAs<Airport>();
    if (resultAirport != null)
    {
        await collection.RemoveAsync(id);
        return Results.Ok(id);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/airline/list", async (string? country, int? limit, int? offset, IClusterProvider clusterProvider,
    IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value; 

        //get the cluster provider to run a query from
        var cluster = await clusterProvider.GetClusterAsync();

        var query = $@"SELECT airline.callsign,
                        airline.country,
                        airline.iata,
                        airline.icao,
                        airline.name
                        FROM `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airline` AS airline
                        WHERE lower(airline.country) = $country
                        ORDER BY airline.name
                        LIMIT $limit
                        OFFSET $offset";

        //setup parameters
        var queryParameters = new Couchbase.Query.QueryOptions();
        if (country != null) queryParameters.Parameter("country", country.ToLower());
        if (limit != null) queryParameters.Parameter("limit", limit);
        if (offset != null) queryParameters.Parameter("offset", offset);

        var results = await cluster.QueryAsync<Airline>(query, queryParameters);
        var items = await results.Rows.ToListAsync<Airline>();

        if (items.Count == 0)
            return Results.NotFound();

        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
    return Results.NotFound();
});

app.MapGet("/airline/to-airport", async (string? airport, int? limit, int? offset, IClusterProvider clusterProvider,
    IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the cluster provider to run a query from
        var cluster = await clusterProvider.GetClusterAsync();

        var query = $@"SELECT air.callsign,
                               air.country,
                               air.iata,
                               air.icao,
                               air.name
                      FROM (
                        SELECT DISTINCT META(airline).id AS airlineId
                        FROM `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`route` AS route
                        JOIN `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airline` AS airline
                        ON route.airlineid = META(airline).id
                        WHERE lower(route.destinationairport) = $airport
                      ) AS SUBQUERY
                      JOIN `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airline` AS air
                      ON META(air).id = SUBQUERY.airlineId
                      LIMIT $limit
                      OFFSET $offset";
        
        //setup parameters
        var queryParameters = new Couchbase.Query.QueryOptions();
        if (airport != null) queryParameters.Parameter("airport", airport.ToLower());
        if (limit != null) queryParameters.Parameter("limit", limit);
        if (offset != null) queryParameters.Parameter("offset", offset);

        var results = await cluster.QueryAsync<Airline>(query, queryParameters);
        var items = await results.Rows.ToListAsync<Airline>();

        if (items.Count == 0)
            return Results.NotFound();

        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();
});

app.MapGet("/airline/{id}", async (string id, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("airline");

        //get the document from the bucket using the id
        var result = await collection.GetAsync(id);

        //validate we have a document
        var resultAirlines = result.ContentAs<Airline>();
        if (resultAirlines != null)
        {
            return Results.Ok(resultAirlines);
        }
    }
    catch (Couchbase.Core.Exceptions.KeyValue.DocumentNotFoundException)
    {
        Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();
});

app.MapPost("/airline/{id}",
    async (string id, AirlineCreateRequestCommand request, IBucketProvider bucketProvider,
        IOptions<CouchbaseConfig> options) =>
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("airline");

        //get airline from request
        var airline = request.GetAirline();

        //save document
        await collection.InsertAsync(id, airline);
        return Results.Created($"/airline/{id}", airline);
    });

app.MapPut("/airline/{id}", async (string? id, AirlineCreateRequestCommand request, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("airline");

    //get current airline from the database
    var result = await collection.GetAsync(id);
    if (result != null)
    {
        var airline = result.ContentAs<Airline>();
        var updateResult = await collection.ReplaceAsync<Airline>(id, request.GetAirline());

        return Results.Ok(request);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapDelete("airline/{id}", async(string id, IBucketProvider bucketProvider, IOptions < CouchbaseConfig > options) => 
{

    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("airline");

    //get the document from the bucket using the id
    var result = await collection.GetAsync(id);

    //validate we have a document
    var resultAirline = result.ContentAs<Airline>();
    if ( resultAirline != null)
    {
        await collection.RemoveAsync(id);
        return Results.Ok(id);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/route/{id}", async (string id, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    try
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("route");

        //get the document from the bucket using the id
        var result = await collection.GetAsync(id);

        //validate we have a document
        var resultAirlines = result.ContentAs<Route>();
        if (resultAirlines != null)
        {
            return Results.Ok(resultAirlines);
        }
    }
    catch (Couchbase.Core.Exceptions.KeyValue.DocumentNotFoundException)
    {
        Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

    return Results.NotFound();
});

app.MapPost("/route/{id}",
    async (string id, RouteCreateRequestCommand request, IBucketProvider bucketProvider,
        IOptions<CouchbaseConfig> options) =>
    {
        //get couchbase config values from appsettings.json 
        var couchbaseConfig = options.Value;

        //get the bucket, scope, and collection
        var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
        var scope = bucket.Scope(couchbaseConfig.ScopeName);
        var collection = scope.Collection("route");

        //get route from request
        var route = request.GetRoute();

        //save document
        await collection.InsertAsync(id, route);
        return Results.Created($"/route/{id}", route);
    });

app.MapPut("/route/{id}", async (string? id,RouteCreateRequestCommand request, IBucketProvider bucketProvider, IOptions<CouchbaseConfig> options) =>
{
    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("route");

    //get current route from the database
    var result = await collection.GetAsync(id);
    if (result != null)
    {
        var route = result.ContentAs<Route>();
        var updateResult = await collection.ReplaceAsync<Route>(id, request.GetRoute());

        return Results.Ok(request);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapDelete("route/{id}", async(string id, IBucketProvider bucketProvider, IOptions < CouchbaseConfig > options) => 
{

    //get couchbase config values from appsettings.json 
    var couchbaseConfig = options.Value;

    //get the bucket, scope, and collection
    var bucket = await bucketProvider.GetBucketAsync(couchbaseConfig.BucketName);
    var scope = bucket.Scope(couchbaseConfig.ScopeName);
    var collection = scope.Collection("route");

    //get the document from the bucket using the id
    var result = await collection.GetAsync(id);

    //validate we have a document
    var resultRoute = result.ContentAs<Route>();
    if ( resultRoute != null)
    {
        await collection.RemoveAsync(id);
        return Results.Ok(id);
    }
    else
    {
        return Results.NotFound();
    }
});

app.Run();

// required for integration testing from asp.net
// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0
public partial class Program { }

