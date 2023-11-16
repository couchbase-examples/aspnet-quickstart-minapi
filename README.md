# Quickstart in Couchbase with C# and ASP.NET 8 Minimum API

#### Build a REST API with Couchbase's C# SDK 3.4 and ASP.NET Minimum API

> This repo is designed to teach you how to connect to a Couchbase cluster to create, read, update, and delete documents and how to write simple parametrized N1QL queries using the new ASP.NET Minimum API framework and the built-in travel-sample bucket. If you want to run this tutorial using a self managed Couchbase cluster, please refer to the [appendix](#appendix-running-self-managed-couchbase-cluster).

Full documentation can be found on the [Couchbase Developer Portal](https://developer.couchbase.com/tutorial-quickstart-csharp-aspnet-minapi/).

## Prerequisites
To run this prebuilt project, you will need:

- Couchbase Server (7 or higher) with [travel-sample](https://docs.couchbase.com/python-sdk/current/ref/travel-app-data-model.html) bucket loaded.
  - [Couchbase Capella](https://www.couchbase.com/products/capella/) is the easiest way to get started.
- [.NET SDK v8+](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- Code Editor installed (Visual Studio Professional, Visual Studio Code, or JetBrains Rider)

### Loading Travel Sample Bucket

If travel-sample is not loaded in your Capella cluster, you can load it by following the instructions for your Capella Cluster:

- [Load travel-sample bucket in Couchbase Capella](https://docs.couchbase.com/cloud/clusters/data-service/import-data-documents.html#import-sample-data)

### Install Dependencies

```sh
cd src/Couchbase.TravelSample
dotnet restore
```

#### DependencyInjection Nuget package

The Couchbase SDK for .NET includes a nuget package called `Couchbase.Extensions.DependencyInjection` which is designed for environments like ASP.NET that takes in a configuration to connect to Couchbase and automatically registers interfaces that you can use in your code to perform full `CRUD (create, read, update, delete)` operations and queries against the database.

### Database Server Configuration

All configuration for communication with the database is stored in the appsettings.Development.json file.  This includes the connection string, username, password, bucket name and scope name.  The default username is assumed to be `Administrator` and the default password is assumed to be `P@$$w0rd12`.  If these are different in your environment you will need to change them before running the application.

Specifically, you need to do the following:

- Create the [database credentials](https://docs.couchbase.com/cloud/clusters/manage-database-users.html) to access the travel-sample bucket (Read and Write) used in the application.
- [Allow access](https://docs.couchbase.com/cloud/clusters/allow-ip-address.html) to the Cluster from the IP on which the application is running.

> Note: The connection string expects the `couchbases://` or `couchbase://` part.

#### Capella Users

For Capella users, follow the directions for [Configure Database Credentials](https://docs.couchbase.com/cloud/clusters/manage-database-users.html); name it `Administrator` with a password of `P@$$w0rd12`.

Next, open the [appsettings.Development.json](https://github.com/couchbase-examples/aspnet-minapi-quickstart-travelsample/blob/main/src/Couchbase.TravelSample/appsettings.Development.json) file.  Locate the ConnectionString property and update it to match your Wide Area Network name found in the [Capella Portal UI Connect tab](https://docs.couchbase.com/cloud/get-started/connect-to-cluster.html#connect-to-your-cluster-using-the-built-in-sdk-examples). Note that Capella uses TLS so the connection string must start with couchbases://.  This configuration is designed for development environments only.

```json
  "Couchbase": {
    "BucketName": "travel-sample",
    "ScopeName": "inventory",
    "ConnectionString": "couchbases://yourassignedhostname.cloud.couchbase.com",
    "Username": "Administrator",
    "Password": "P@ssw0rd12",
    "IgnoreRemoteCertificateNameMismatch": true,
    "HttpIgnoreRemoteCertificateMismatch": true,
    "KvIgnoreRemoteCertificateNameMismatch": true
  }

```

## Running The Application

### Running directly on machine

At this point, we have installed the dependencies, loaded the travel-sample data and configured the application with the credentials. The application is now ready and you can run it.
```shell 
cd src/Couchbase.TravelSample
dotnet run
```

Once the site is up and running you can launch your browser and go to the [Swagger start page](https://localhost:5021/swagger/index.html) to test the APIs.

### Running using docker

  - Build the Docker image
```shell 
cd src
docker build -t couchbase-aspnet-minapi-quickstart -f Couchbase.TravelSample/Dockerfile .
```

  - Run the docker image
```shell 
docker run -p 8080:80 couchbase-aspnet-minapi-quickstart
```
>**Note:** The application can now be reached on port 8080 of your local machine.

### Checking the Application

Once the application starts, you can see the details of the application on the logs.

![Application Startup](app_startup.png)

The application will run on port 8080 of your local machine (http://localhost:5021). You will find the Swagger documentation of the API if you go to the URL in your browser.

![Swagger Documentation](swagger_documentation.png)

## Running The Tests

To run the standard integration tests, use the following commands:

```sh
cd ../Couchbase.TravelSample.Tests/
dotnet restore 
dotnet build
dotnet test
```

## Appendix: Data Model

For this quickstart, we use three collections, airport, airline and routes that contain sample airports, airlines and airline routes respectively. The routes collection connects the airports and airlines as seen in the figure below. We use these connections in the quickstart to generate airports that are directly connected and airlines connecting to a destination airport. Note that these are just examples to highlight how you can use SQL++ queries to join the collections.

![travel sample data model](travel_sample_data_model.png)

## Appendix: Running Self Managed Couchbase Cluster

If you are running this quickstart with a self managed Couchbase cluster, you need to [load](https://docs.couchbase.com/server/current/manage/manage-settings/install-sample-buckets.html) the travel-sample data bucket in your cluster and generate the credentials for the bucket.

You need to update the connection string and the credentials in the [appsettings.Development.json](https://github.com/couchbase-examples/aspnet-minapi-quickstart-travelsample/blob/main/src/Couchbase.TravelSample/appsettings.Development.json) file in the source folder.

> **NOTE:** Couchbase must be installed and running prior to running the the ASP.NET app.

