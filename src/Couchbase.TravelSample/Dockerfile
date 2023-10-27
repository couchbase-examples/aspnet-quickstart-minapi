FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 
WORKDIR /App
EXPOSE 80
EXPOSE 443

COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Couchbase.TravelSample.dll"]
