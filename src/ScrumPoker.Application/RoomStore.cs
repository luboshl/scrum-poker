using System.Collections.Concurrent;
using ScrumPoker.Domain;

namespace ScrumPoker.Application;

public class RoomStore : IRoomStore
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public Room GetOrCreate(string roomId) =>
        _rooms.GetOrAdd(roomId, id => new Room(id));

    public Room? Get(string roomId) =>
        _rooms.TryGetValue(roomId, out var room) ? room : null;

    public IReadOnlyList<Room> GetAll() => _rooms.Values.ToList();
}
