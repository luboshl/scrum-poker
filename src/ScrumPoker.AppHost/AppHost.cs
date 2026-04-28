var builder = DistributedApplication.CreateBuilder(args);

// Temporary: orchestrate the legacy Node.js app during migration.
// Replace this resource with the ASP.NET Core backend once it is scaffolded.
var legacyApp = builder.AddNpmApp("scrum-poker-legacy", "../../", "start")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
