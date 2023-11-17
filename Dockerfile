FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy only the necessary project files
COPY ./src/Couchbase.TravelSample/Couchbase.TravelSample.csproj ./
RUN dotnet restore

# Copy the entire project directory
COPY ./src/Couchbase.TravelSample ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 
WORKDIR /App

COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Couchbase.TravelSample.dll"]
