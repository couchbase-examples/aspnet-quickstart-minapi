# Couchbase .Net travel-sample Application REST Backend

This is a sample application for getting started with [Couchbase Server] and the .Net SDK.

## Running the application with Docker

You will need [Docker](https://docs.docker.com/get-docker/) installed on your machine in order to run this application as we have defined a [_Dockerfile_](Dockerfile) and a [_docker-compose.yml_](docker-compose.yml) to run Couchbase Server and the .NET REST API.

To launch the full application, simply run this command from a terminal:

    docker-compose up -d

You can access the backend API on http://localhost:8080/swagger/index.html
and Couchbase Server at http://localhost:8091/


[Couchbase Server]: https://www.couchbase.com/
[.NET SDK]: https://docs.couchbase.com/dotnet-sdk/current/hello-world/overview.html
[ASP.NET]: https://dotnet.microsoft.com/apps/aspnet
[Swagger]: https://swagger.io/resources/open-api/
