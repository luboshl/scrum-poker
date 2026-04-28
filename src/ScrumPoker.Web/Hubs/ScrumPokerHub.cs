using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ScrumPoker.Application;

namespace ScrumPoker.Web.Hubs;

public class ScrumPokerHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly ILogger<ScrumPokerHub> _logger;

    // Shared across all hub instances; entries are removed in OnDisconnectedAsync
    // and when a user is forcibly removed via RemoveUser.
    private static readonly ConcurrentDictionary<string, (string RoomId, string UserName)> _connectionMap = new();

    public ScrumPokerHub(IRoomService roomService, ILogger<ScrumPokerHub> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public async Task JoinRoom(string room, string name, bool isObserver)
    {
        var result = _roomService.JoinRoom(room, Context.ConnectionId, name, isObserver);
        _connectionMap[Context.ConnectionId] = (room, result.ResolvedName);

        await Groups.AddToGroupAsync(Context.ConnectionId, room);

        await Clients.Group(room).SendAsync("roomUpdate", result.RoomState);
        await Clients.Caller.SendAsync("joinConfirmation", new { name = result.ResolvedName, isObserver = result.IsObserver });

        _logger.LogInformation("User {Name} joined room {Room}{Observer}", result.ResolvedName, room, result.IsObserver ? " as observer" : "");
    }

    public async Task Vote(string vote)
    {
        if (!_connectionMap.TryGetValue(Context.ConnectionId, out var info)) return;
        if (!_roomService.Vote(info.RoomId, Context.ConnectionId, vote)) return;

        var state = _roomService.GetRoomState(info.RoomId);
        if (state != null)
            await Clients.Group(info.RoomId).SendAsync("roomUpdate", state);
    }

    public async Task CancelVote()
    {
        if (!_connectionMap.TryGetValue(Context.ConnectionId, out var info)) return;
        if (!_roomService.CancelVote(info.RoomId, Context.ConnectionId)) return;

        var state = _roomService.GetRoomState(info.RoomId);
        if (state != null)
            await Clients.Group(info.RoomId).SendAsync("roomUpdate", state);
    }

    public async Task EndVoting()
    {
        if (!_connectionMap.TryGetValue(Context.ConnectionId, out var info)) return;
        if (!_roomService.EndVoting(info.RoomId, Context.ConnectionId)) return;

        var state = _roomService.GetRoomState(info.RoomId);
        if (state != null)
            await Clients.Group(info.RoomId).SendAsync("roomUpdate", state);
    }

    public async Task ResetVoting()
    {
        if (!_connectionMap.TryGetValue(Context.ConnectionId, out var info)) return;
        if (!_roomService.ResetVoting(info.RoomId, Context.ConnectionId)) return;

        await Clients.Group(info.RoomId).SendAsync("resetVoting");
        var state = _roomService.GetRoomState(info.RoomId);
        if (state != null)
            await Clients.Group(info.RoomId).SendAsync("roomUpdate", state);
    }

    public async Task RemoveUser(string userToRemove)
    {
        if (!_connectionMap.TryGetValue(Context.ConnectionId, out var info)) return;
        var result = _roomService.RemoveUser(info.RoomId, Context.ConnectionId, userToRemove);

        if (result.Success)
        {
            if (result.RoomState != null)
                await Clients.Group(info.RoomId).SendAsync("roomUpdate", result.RoomState);

            await Clients.Caller.SendAsync("userRemoved", new { status = "success", userName = userToRemove });

            if (result.RemovedConnectionId != null)
            {
                await Clients.Client(result.RemovedConnectionId).SendAsync("forcedDisconnect",
                    new { message = $"You were removed from room {info.RoomId} by an observer" });
                await Groups.RemoveFromGroupAsync(result.RemovedConnectionId, info.RoomId);
                _connectionMap.TryRemove(result.RemovedConnectionId, out _);
            }
        }
        else
        {
            await Clients.Caller.SendAsync("userRemoved", new { status = "error", userName = userToRemove, message = result.ErrorMessage });
        }
    }

    public Task Heartbeat()
    {
        if (_connectionMap.TryGetValue(Context.ConnectionId, out var info))
            _roomService.UpdateHeartbeat(info.RoomId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionMap.TryRemove(Context.ConnectionId, out var info))
        {
            _roomService.Disconnect(info.RoomId, Context.ConnectionId);
            var state = _roomService.GetRoomState(info.RoomId);
            if (state != null)
                await Clients.Group(info.RoomId).SendAsync("roomUpdate", state);
            _logger.LogInformation("User {Name} disconnected from room {Room}", info.UserName, info.RoomId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
