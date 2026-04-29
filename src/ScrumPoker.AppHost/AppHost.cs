var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ScrumPoker_Web>("scrum-poker-web");

builder.Build().Run();
