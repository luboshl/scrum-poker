using System.Collections.Concurrent;
using ScrumPoker.Domain;

namespace ScrumPoker.Application;

public class RoomService : IRoomService
{
    private readonly IRoomStore _store;
    // Lock objects are never removed after room creation. For the expected scale of
    // a Scrum Poker app (hundreds of rooms at most), this is an intentional trade-off
    // to avoid additional synchronization overhead.
    private readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.OrdinalIgnoreCase);

    public RoomService(IRoomStore store) => _store = store;

    private object LockFor(string roomId) => _locks.GetOrAdd(roomId, _ => new object());

    public JoinResult JoinRoom(string roomId, string connectionId, string name, bool isObserver)
    {
        var room = _store.GetOrCreate(roomId);
        lock (LockFor(roomId))
        {
            var uniqueName = ResolveName(room, name);

            var existing = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (existing != null)
            {
                existing.Name = uniqueName;
                existing.Active = true;
                existing.IsObserver = isObserver;
                existing.LastHeartbeat = DateTime.UtcNow;
            }
            else
            {
                room.Participants.Add(new Participant(connectionId, uniqueName, isObserver));
            }

            return new JoinResult(true, uniqueName, isObserver, ProjectRoom(room));
        }
    }

    private static string ResolveName(Room room, string requestedName)
    {
        var uniqueName = requestedName;
        var counter = 2;
        while (room.Participants.Any(p => p.Name == uniqueName && p.Active))
            uniqueName = $"{requestedName} ({counter++})";
        return uniqueName;
    }

    public bool Vote(string roomId, string connectionId, string vote)
    {
        var room = _store.Get(roomId);
        if (room == null) return false;
        lock (LockFor(roomId))
        {
            var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId && p.Active);
            if (participant == null || participant.IsObserver) return false;
            participant.Vote = vote;
            return true;
        }
    }

    public bool CancelVote(string roomId, string connectionId)
    {
        var room = _store.Get(roomId);
        if (room == null) return false;
        lock (LockFor(roomId))
        {
            var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId && p.Active);
            if (participant == null || participant.IsObserver) return false;
            participant.Vote = null;
            return true;
        }
    }

    public bool EndVoting(string roomId, string connectionId)
    {
        var room = _store.Get(roomId);
        if (room == null) return false;
        lock (LockFor(roomId))
        {
            if (room.VotingEnded) return false;
            room.VotingEnded = true;
            return true;
        }
    }

    public bool ResetVoting(string roomId, string connectionId)
    {
        var room = _store.Get(roomId);
        if (room == null) return false;
        lock (LockFor(roomId))
        {
            room.VotingEnded = false;
            foreach (var p in room.Participants)
                p.Vote = null;
            return true;
        }
    }

    public RemoveUserResult RemoveUser(string roomId, string requesterConnectionId, string targetName)
    {
        var room = _store.Get(roomId);
        if (room == null) return new RemoveUserResult(false, "Room not found", null, null);
        lock (LockFor(roomId))
        {
            var requester = room.Participants.FirstOrDefault(p => p.ConnectionId == requesterConnectionId && p.Active);
            if (requester == null || !requester.IsObserver)
                return new RemoveUserResult(false, "Only observers can remove users from the room", null, null);

            if (requester.Name == targetName)
                return new RemoveUserResult(false, "You cannot remove yourself from the room", null, null);

            var target = room.Participants.FirstOrDefault(p => p.Name == targetName);
            if (target == null)
                return new RemoveUserResult(false, "User not found", null, null);

            room.Participants.Remove(target);
            return new RemoveUserResult(true, null, target.ConnectionId, ProjectRoom(room));
        }
    }

    public void UpdateHeartbeat(string roomId, string connectionId)
    {
        var room = _store.Get(roomId);
        if (room == null) return;
        lock (LockFor(roomId))
        {
            var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (participant != null)
                participant.LastHeartbeat = DateTime.UtcNow;
        }
    }

    public void Disconnect(string roomId, string connectionId)
    {
        var room = _store.Get(roomId);
        if (room == null) return;
        lock (LockFor(roomId))
        {
            var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (participant != null)
            {
                participant.Active = false;
                room.Participants.RemoveAll(p => !p.Active);
            }
        }
    }

    public RoomStateDto? GetRoomState(string roomId)
    {
        var room = _store.Get(roomId);
        if (room == null) return null;
        lock (LockFor(roomId))
            return ProjectRoom(room);
    }

    public IReadOnlyList<string> CleanupInactiveParticipants(TimeSpan inactivityThreshold)
    {
        var cutoff = DateTime.UtcNow - inactivityThreshold;
        var affectedRooms = new List<string>();
        foreach (var room in _store.GetAll())
        {
            lock (LockFor(room.Id))
            {
                int before = room.Participants.Count;
                room.Participants.RemoveAll(p => p.LastHeartbeat < cutoff);
                if (room.Participants.Count != before)
                    affectedRooms.Add(room.Id);
            }
        }
        return affectedRooms;
    }

    private static RoomStateDto ProjectRoom(Room room) =>
        new(room.Participants
            .Where(p => p.Active)
            .Select(p => new ParticipantDto(p.Name, p.Vote, p.IsObserver))
            .ToList(),
            room.VotingEnded);
}
