using ScrumPoker.Application;
using ScrumPoker.Domain;

namespace ScrumPoker.UnitTests;

public class RoomServiceTests : IDisposable
{
    private readonly RoomService _sut = new();

    public void Dispose() => _sut.Dispose();

    [Fact]
    public async Task GetOrCreateRoomAsync_NewRoom_ReturnsRoomWithMatchingId()
    {
        var (room, roomLock) = await _sut.GetOrCreateRoomAsync("room1");
        using (roomLock)
        {
            Assert.Equal("room1", room.Id);
            Assert.Empty(room.Users);
            Assert.False(room.VotingEnded);
        }
    }

    [Fact]
    public async Task GetOrCreateRoomAsync_ExistingRoom_ReturnsSameRoom()
    {
        var (room1, lock1) = await _sut.GetOrCreateRoomAsync("room1");
        using (lock1) { room1.VotingEnded = true; }

        var (room2, lock2) = await _sut.GetOrCreateRoomAsync("room1");
        using (lock2)
        {
            Assert.Same(room1, room2);
            Assert.True(room2.VotingEnded);
        }
    }

    [Fact]
    public async Task TryGetRoomAsync_ExistingRoom_ReturnsRoom()
    {
        (await _sut.GetOrCreateRoomAsync("room1")).Lock.Dispose();

        var result = await _sut.TryGetRoomAsync("room1");
        Assert.NotNull(result);

        var (room, roomLock) = result.Value;
        using (roomLock)
        {
            Assert.Equal("room1", room.Id);
        }
    }

    [Fact]
    public async Task TryGetRoomAsync_NonExistentRoom_ReturnsNull()
    {
        var result = await _sut.TryGetRoomAsync("does-not-exist");
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRemoveRoom_ExistingRoom_RemovesAndReturnsTrue()
    {
        (await _sut.GetOrCreateRoomAsync("room1")).Lock.Dispose();

        bool removed = _sut.TryRemoveRoom("room1");

        Assert.True(removed);

        // Room should no longer be reachable.
        var result = await _sut.TryGetRoomAsync("room1");
        Assert.Null(result);
    }

    [Fact]
    public void TryRemoveRoom_NonExistentRoom_ReturnsFalse()
    {
        bool removed = _sut.TryRemoveRoom("does-not-exist");
        Assert.False(removed);
    }

    [Fact]
    public async Task TryRemoveRoom_DisposesLockWithEntry_NoLockAccumulation()
    {
        // Create and immediately release several rooms.
        for (int i = 0; i < 5; i++)
        {
            var (_, roomLock) = await _sut.GetOrCreateRoomAsync($"room{i}");
            roomLock.Dispose();
        }

        // Remove all rooms — their locks must be disposed along with the entries.
        for (int i = 0; i < 5; i++)
        {
            Assert.True(_sut.TryRemoveRoom($"room{i}"));
        }

        // None of the rooms should still be accessible.
        for (int i = 0; i < 5; i++)
        {
            var result = await _sut.TryGetRoomAsync($"room{i}");
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task LockIsMutuallyExclusive_ConcurrentCallsSerializeAccess()
    {
        const string roomId = "concurrent-room";
        int counter = 0;
        bool raceDetected = false;

        async Task IncrementAsync()
        {
            var (room, roomLock) = await _sut.GetOrCreateRoomAsync(roomId);
            using (roomLock)
            {
                int snapshot = counter;
                // Yield to give other tasks a chance to run.
                await Task.Yield();
                if (counter != snapshot)
                {
                    raceDetected = true;
                }

                counter++;
                room.Users.Add(new User
                {
                    ConnectionId = Guid.NewGuid().ToString(),
                    Name = $"User{counter}",
                });
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => IncrementAsync()));

        Assert.False(raceDetected, "A data race was detected — the lock did not serialise access.");
        Assert.Equal(20, counter);
    }
}
