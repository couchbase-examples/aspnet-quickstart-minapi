# Evidence for issue #73 (.NET 10 upgrade)

- `swagger-net10.png`, `swagger-net10-airport-list.png`: Swagger UI from the README Docker image (dotnet/aspnet:10.0) against local Couchbase 8.0.1 + travel-sample
- `swagger-main-net8.json` vs `swagger-branch-net10.json`: generated OpenAPI documents (main on .NET 8 vs this branch on .NET 10)
- `test-summary-*.txt`: dotnet test results
