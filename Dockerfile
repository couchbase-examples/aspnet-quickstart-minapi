# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy only the necessary project files
COPY ./src/Couchbase.TravelSample/Couchbase.TravelSample.csproj ./
RUN dotnet restore

# Copy the entire project directory
COPY ./src/Couchbase.TravelSample ./
RUN dotnet publish -c Release -o out

# Use the .NET ASP.NET Runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 
WORKDIR /App

# Expose port 8080 for the application
EXPOSE 8080

# Copy the published output from build-env to the current directory in the runtime image
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Couchbase.TravelSample.dll"]
