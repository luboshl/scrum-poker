using ScrumPoker.Application;
using ScrumPoker.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IRoomService, RoomService>();

var app = builder.Build();

app.MapHub<RoomHub>("/roomHub");

app.Run();
