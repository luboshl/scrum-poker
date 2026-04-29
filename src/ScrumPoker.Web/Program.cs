using ScrumPoker.Application;
using ScrumPoker.Web.Hubs;
using ScrumPoker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IRoomStore, RoomStore>();
builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddHostedService<HeartbeatCleanupService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();

app.MapHub<ScrumPokerHub>("/hub");

app.Run();
