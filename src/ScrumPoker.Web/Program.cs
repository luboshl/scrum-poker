using ScrumPoker.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();

app.MapHub<ScrumPokerHub>("/hub");

app.Run();
