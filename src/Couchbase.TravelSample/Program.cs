using Microsoft.Extensions.Options;

using Couchbase.Extensions.DependencyInjection;
using Couchbase.TravelSample.Models;

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




app.MapGet("/airports", async (string? country, int? limit, int? skip, IClusterProvider clusterProvider, IOptions<CouchbaseConfig> options) =>
    {
        try
        {
            //get couchbase config values from appsettings.json 
            var couchbaseConfig = options.Value; //<1>
            
            //get the cluster provider to run a query from
            var cluster = await clusterProvider.GetClusterAsync();
            
            var query = $@"SELECT p.* FROM `{couchbaseConfig.BucketName}`.`{couchbaseConfig.ScopeName}`.`airport` p WHERE lower(p.country) LIKE '%' || $search || '%' OR lower(p.lastName) LIKE '%' || $search || '%' LIMIT $limit OFFSET $skip";
            
            //setup parameters
            var queryParameters = new Couchbase.Query.QueryOptions();
            queryParameters.Parameter("search", country.ToLower());
            queryParameters.Parameter("limit", limit == null ? 5 : limit);
            queryParameters.Parameter("skip", skip == null ? 0 : skip);
            
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
    })
    .WithName("GetAirports")
    .WithOpenApi();

app.Run();

// required for integration testing from asp.net
// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0
public partial class Program { }

