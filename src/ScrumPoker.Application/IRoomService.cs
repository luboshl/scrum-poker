using ScrumPoker.Domain;

namespace ScrumPoker.Application;

public interface IRoomService
{
    Task<(Room Room, IDisposable Lock)> GetOrCreateRoomAsync(string roomId, CancellationToken cancellationToken = default);
    Task<(Room Room, IDisposable Lock)?> TryGetRoomAsync(string roomId, CancellationToken cancellationToken = default);
    bool TryRemoveRoom(string roomId);
}
