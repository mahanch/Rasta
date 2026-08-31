var builder = DistributedApplication.CreateBuilder(args);

// 1. PostgreSQL Server (for Write Databases: license-db and shop-write-db)
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16")
    .WithDataVolume();

var licenseDb = postgres.AddDatabase("license-db");
var shopWriteDb = postgres.AddDatabase("shop-write-db");

// 2. MongoDB Server (for Read Database: shop-read-db)
var mongodb = builder.AddMongoDB("mongodb")
    .WithImage("mongo", "7.0")
    .WithDataVolume();

var shopReadDb = mongodb.AddDatabase("shop-read-db");

// 3. RabbitMQ Message Broker (for Event-Driven Projections & Licensing Sync)
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

// 4. License Management Server API
var licenseServerApi = builder.AddProject<Projects.LicenseServer_Api>("licenseserver-api")
    .WithReference(licenseDb)
    .WithHttpEndpoint(port: 5100, name: "public");

// 5. Enterprise Shop API (DDD + CQRS + RabbitMQ + Mongo Read + Postgres Write)
var shopApi = builder.AddProject<Projects.Shop_Api>("shop-api")
    .WithReference(shopWriteDb)
    .WithReference(shopReadDb)
    .WithReference(messaging)
    .WithReference(licenseServerApi)
    .WithEnvironment("LicenseSettings__LicenseServerUrl", licenseServerApi.GetEndpoint("public"))
    .WithHttpEndpoint(port: 5200, name: "public");

builder.Build().Run();