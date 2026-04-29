using System.Collections.Concurrent;
using ScrumPoker.Domain;

namespace ScrumPoker.Application;

/// <summary>
/// Thread-safe in-memory room service.
/// </summary>
/// <remarks>
/// Each room entry owns its own <see cref="SemaphoreSlim"/> lock, so when the
/// entry is removed from the dictionary the lock is disposed together with it.
/// This avoids the lock-accumulation problem that arises when a separate
/// dictionary is used to hold lock objects alongside room entries.
/// </remarks>
public sealed class RoomService : IRoomService, IDisposable
{
    private readonly ConcurrentDictionary<string, RoomEntry> _rooms = new(StringComparer.Ordinal);

    public async Task<(Room Room, IDisposable Lock)> GetOrCreateRoomAsync(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        var entry = _rooms.GetOrAdd(roomId, static id => new RoomEntry(new Room { Id = id }));
        return await entry.AcquireAsync(cancellationToken);
    }

    public async Task<(Room Room, IDisposable Lock)?> TryGetRoomAsync(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        if (!_rooms.TryGetValue(roomId, out var entry))
        {
            return null;
        }

        return await entry.AcquireAsync(cancellationToken);
    }

    public bool TryRemoveRoom(string roomId)
    {
        if (_rooms.TryRemove(roomId, out var entry))
        {
            entry.Dispose();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        foreach (var entry in _rooms.Values)
        {
            entry.Dispose();
        }

        _rooms.Clear();
    }

    /// <summary>
    /// Consolidates room state and its per-room lock into a single object so
    /// that both are always removed together.
    /// </summary>
    private sealed class RoomEntry : IDisposable
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _disposed;

        public RoomEntry(Room room)
        {
            Room = room;
        }

        public Room Room { get; }

        public async Task<(Room Room, IDisposable Lock)> AcquireAsync(CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            return (Room, new SemaphoreReleaser(_lock));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _lock.Dispose();
            }
        }
    }

    private sealed class SemaphoreReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _released;

        public SemaphoreReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_released)
            {
                _released = true;
                _semaphore.Release();
            }
        }
    }
}
