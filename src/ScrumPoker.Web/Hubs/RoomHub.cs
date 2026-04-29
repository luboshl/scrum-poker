using Microsoft.AspNetCore.SignalR;
using ScrumPoker.Application;
using ScrumPoker.Domain;

namespace ScrumPoker.Web.Hubs;

public class RoomHub : Hub
{
    private readonly IRoomService _roomService;

    public RoomHub(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task JoinRoom(string roomId, string name, bool isObserver)
    {
        var (room, roomLock) = await _roomService.GetOrCreateRoomAsync(roomId, Context.ConnectionAborted);
        using (roomLock)
        {
            string uniqueName = EnsureUniqueName(room, name);

            User? existing = room.Users.Find(u => u.Name == uniqueName);
            if (existing is null)
            {
                room.Users.Add(new User
                {
                    ConnectionId = Context.ConnectionId,
                    Name = uniqueName,
                    IsObserver = isObserver,
                    IsActive = true,
                });
            }
            else
            {
                existing.ConnectionId = Context.ConnectionId;
                existing.IsActive = true;
                existing.IsObserver = isObserver;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId, Context.ConnectionAborted);
            await Clients.Group(roomId).SendAsync("RoomUpdate", BuildRoomUpdate(room), Context.ConnectionAborted);
            await Clients.Caller.SendAsync("JoinConfirmation", new { name = uniqueName, isObserver }, Context.ConnectionAborted);
        }
    }

    public async Task Vote(string roomId, string vote)
    {
        var result = await _roomService.TryGetRoomAsync(roomId, Context.ConnectionAborted);
        if (result is null)
        {
            return;
        }

        var (room, roomLock) = result.Value;
        using (roomLock)
        {
            User? user = room.Users.Find(u => u.ConnectionId == Context.ConnectionId);
            if (user is null || user.IsObserver)
            {
                return;
            }

            user.Vote = vote;
            await Clients.Group(roomId).SendAsync("RoomUpdate", BuildRoomUpdate(room), Context.ConnectionAborted);
        }
    }

    public async Task CancelVote(string roomId)
    {
        var result = await _roomService.TryGetRoomAsync(roomId, Context.ConnectionAborted);
        if (result is null)
        {
            return;
        }

        var (room, roomLock) = result.Value;
        using (roomLock)
        {
            User? user = room.Users.Find(u => u.ConnectionId == Context.ConnectionId);
            if (user is null || user.IsObserver)
            {
                return;
            }

            user.Vote = null;
            await Clients.Group(roomId).SendAsync("RoomUpdate", BuildRoomUpdate(room), Context.ConnectionAborted);
        }
    }

    public async Task EndVoting(string roomId)
    {
        var result = await _roomService.TryGetRoomAsync(roomId, Context.ConnectionAborted);
        if (result is null)
        {
            return;
        }

        var (room, roomLock) = result.Value;
        using (roomLock)
        {
            if (room.VotingEnded)
            {
                return;
            }

            room.VotingEnded = true;
            await Clients.Group(roomId).SendAsync("RoomUpdate", BuildRoomUpdate(room), Context.ConnectionAborted);
        }
    }

    public async Task ResetVoting(string roomId)
    {
        var result = await _roomService.TryGetRoomAsync(roomId, Context.ConnectionAborted);
        if (result is null)
        {
            return;
        }

        var (room, roomLock) = result.Value;
        using (roomLock)
        {
            room.VotingEnded = false;
            foreach (var u in room.Users)
            {
                u.Vote = null;
            }

            await Clients.Group(roomId).SendAsync("ResetVoting", Context.ConnectionAborted);
            await Clients.Group(roomId).SendAsync("RoomUpdate", BuildRoomUpdate(room), Context.ConnectionAborted);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find any room this connection belongs to and remove the user.
        // A real implementation would track the room per-connection; here we
        // rely on the caller passing the roomId via the regular hub methods.
        await base.OnDisconnectedAsync(exception);
    }

    private static string EnsureUniqueName(Room room, string name)
    {
        string candidate = name;
        int counter = 2;

        while (room.Users.Exists(u => u.Name == candidate))
        {
            candidate = $"{name} ({counter++})";
        }

        return candidate;
    }

    private static object BuildRoomUpdate(Room room) =>
        new
        {
            users = room.Users.Select(u => new
            {
                id = u.ConnectionId,
                name = u.Name,
                vote = u.Vote,
                isObserver = u.IsObserver,
                active = u.IsActive,
            }),
            votingEnded = room.VotingEnded,
        };
}
