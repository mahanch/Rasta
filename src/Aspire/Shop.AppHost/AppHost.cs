var builder = DistributedApplication.CreateBuilder(args);

// 1. PostgreSQL Server (Single Database Server for License and Shop)
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16")
    .WithDataVolume();

var licenseDb = postgres.AddDatabase("license-db");
var shopDb = postgres.AddDatabase("shop-db");

// 2. License Management Server API
var licenseServerApi = builder.AddProject<Projects.LicenseServer_Api>("licenseserver-api")
    .WithReference(licenseDb)
    .WithHttpEndpoint(port: 5100, name: "public");

// 3. Enterprise Shop API (Clean Strategy + CQRS on Single PostgreSQL Database)
var shopApi = builder.AddProject<Projects.Shop_Api>("shop-api")
    .WithReference(shopDb)
    .WithReference(licenseServerApi)
    .WithEnvironment("LicenseSettings__LicenseServerUrl", licenseServerApi.GetEndpoint("public"))
    .WithHttpEndpoint(port: 5200, name: "public");

builder.Build().Run();