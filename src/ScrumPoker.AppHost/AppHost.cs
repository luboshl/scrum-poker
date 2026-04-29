var builder = DistributedApplication.CreateBuilder(args);

// Temporary: orchestrate the legacy Node.js app during migration.
// Will be retired once the C# backend reaches behavior parity.
var legacyApp = builder.AddNpmApp("scrum-poker-legacy", "../../", "start")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

// New ASP.NET Core + SignalR backend host.
// Full room and voting behavior will be added in subsequent issues.
builder.AddProject<Projects.ScrumPoker_Web>("scrum-poker-web");

builder.Build().Run();
