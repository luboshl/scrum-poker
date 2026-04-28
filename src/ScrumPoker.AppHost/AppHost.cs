var builder = DistributedApplication.CreateBuilder(args);

// Temporary: orchestrate the legacy Node.js app during migration.
// Replace this resource with the ASP.NET Core backend once it is scaffolded.
var legacyApp = builder.AddNpmApp("scrum-poker-legacy", "../../", "start")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .OnBeforeResourceStarted(async (resource, @event, ct) =>
    {
        // Run npm install before starting the Node.js app so the developer does
        // not need to run it manually before launching the Aspire AppHost.
        var workingDirectory = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "../../"));

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "install",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"npm install failed with exit code {process.ExitCode}.");
        }
    });

builder.Build().Run();
