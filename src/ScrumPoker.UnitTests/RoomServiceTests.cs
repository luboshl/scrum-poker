using ScrumPoker.Application;
using Xunit;

namespace ScrumPoker.UnitTests;

public class RoomServiceTests
{
    private static RoomService CreateService() => new RoomService(new RoomStore());

    // ── JoinRoom ──────────────────────────────────────────────────────────────

    [Fact]
    public void JoinRoom_NewRoom_ReturnsSuccessWithResolvedName()
    {
        var svc = CreateService();
        var result = svc.JoinRoom("room1", "conn1", "Alice", false);

        Assert.True(result.Success);
        Assert.Equal("Alice", result.ResolvedName);
        Assert.False(result.IsObserver);
        Assert.Single(result.RoomState.Users);
    }

    [Fact]
    public void JoinRoom_DuplicateName_AppendsCounter()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        var result = svc.JoinRoom("room1", "conn2", "Alice", false);

        Assert.Equal("Alice (2)", result.ResolvedName);
    }

    [Fact]
    public void JoinRoom_TriplicateName_AppendsIncrementingCounter()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Alice", false);
        var result = svc.JoinRoom("room1", "conn3", "Alice", false);

        Assert.Equal("Alice (3)", result.ResolvedName);
    }

    [Fact]
    public void JoinRoom_SameConnectionId_UpdatesExistingParticipant()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        var result = svc.JoinRoom("room1", "conn1", "Alice", false);

        Assert.Equal("Alice", result.ResolvedName);
        // Should not create a second participant
        Assert.Single(result.RoomState.Users);
    }

    [Fact]
    public void JoinRoom_Observer_IsMarkedAsObserver()
    {
        var svc = CreateService();
        var result = svc.JoinRoom("room1", "conn1", "Scrum Master", true);

        Assert.True(result.IsObserver);
        Assert.True(result.RoomState.Users[0].IsObserver);
    }

    [Fact]
    public void JoinRoom_AfterDisconnect_NameNotConflict()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Disconnect("room1", "conn1");

        // Alice disconnected; new connection should be able to use "Alice"
        var result = svc.JoinRoom("room1", "conn2", "Alice", false);
        Assert.Equal("Alice", result.ResolvedName);
    }

    // ── Vote ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Vote_ValidParticipant_RecordsVote()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        var voted = svc.Vote("room1", "conn1", "5");

        Assert.True(voted);
        var state = svc.GetRoomState("room1")!;
        Assert.Equal("5", state.Users[0].Vote);
    }

    [Fact]
    public void Vote_Observer_Rejected()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "SM", true);
        var voted = svc.Vote("room1", "conn1", "5");

        Assert.False(voted);
    }

    [Fact]
    public void Vote_UnknownRoom_ReturnsFalse()
    {
        var svc = CreateService();
        Assert.False(svc.Vote("nonexistent", "conn1", "5"));
    }

    // ── CancelVote ────────────────────────────────────────────────────────────

    [Fact]
    public void CancelVote_ClearsExistingVote()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Vote("room1", "conn1", "5");
        var cancelled = svc.CancelVote("room1", "conn1");

        Assert.True(cancelled);
        var state = svc.GetRoomState("room1")!;
        Assert.Null(state.Users[0].Vote);
    }

    [Fact]
    public void CancelVote_Observer_Rejected()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "SM", true);
        Assert.False(svc.CancelVote("room1", "conn1"));
    }

    // ── EndVoting ─────────────────────────────────────────────────────────────

    [Fact]
    public void EndVoting_SetsVotingEnded()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        var ended = svc.EndVoting("room1", "conn1");

        Assert.True(ended);
        Assert.True(svc.GetRoomState("room1")!.VotingEnded);
    }

    [Fact]
    public void EndVoting_AlreadyEnded_ReturnsFalse()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.EndVoting("room1", "conn1");
        var second = svc.EndVoting("room1", "conn1");

        Assert.False(second);
    }

    // ── ResetVoting ───────────────────────────────────────────────────────────

    [Fact]
    public void ResetVoting_ClearsVotesAndResetsFlag()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);
        svc.Vote("room1", "conn1", "3");
        svc.Vote("room1", "conn2", "8");
        svc.EndVoting("room1", "conn1");

        var reset = svc.ResetVoting("room1", "conn1");

        Assert.True(reset);
        var state = svc.GetRoomState("room1")!;
        Assert.False(state.VotingEnded);
        Assert.All(state.Users, u => Assert.Null(u.Vote));
    }

    // ── RemoveUser ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveUser_ByObserver_Succeeds()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);
        svc.JoinRoom("room1", "conn1", "Alice", false);

        var result = svc.RemoveUser("room1", "observer1", "Alice");

        Assert.True(result.Success);
        Assert.Equal("conn1", result.RemovedConnectionId);
        Assert.Single(result.RoomState!.Users); // only SM remains
    }

    [Fact]
    public void RemoveUser_ByNonObserver_Rejected()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);

        var result = svc.RemoveUser("room1", "conn1", "Bob");

        Assert.False(result.Success);
        Assert.Contains("observer", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveUser_Self_Rejected()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);

        var result = svc.RemoveUser("room1", "observer1", "SM");

        Assert.False(result.Success);
        Assert.Contains("yourself", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveUser_NotFound_Rejected()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);

        var result = svc.RemoveUser("room1", "observer1", "Ghost");

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ── Heartbeat / Disconnect ────────────────────────────────────────────────

    [Fact]
    public void UpdateHeartbeat_UpdatesTimestamp()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Should not throw
        svc.UpdateHeartbeat("room1", "conn1");
    }

    [Fact]
    public void Disconnect_RemovesParticipantFromState()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);
        svc.Disconnect("room1", "conn1");

        var state = svc.GetRoomState("room1")!;
        Assert.Single(state.Users);
        Assert.Equal("Bob", state.Users[0].Name);
    }

    // ── CleanupInactiveParticipants ───────────────────────────────────────────

    [Fact]
    public void CleanupInactiveParticipants_RemovesStaleParticipants()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Immediately cleanup with zero threshold — everyone is stale
        var affected = svc.CleanupInactiveParticipants(TimeSpan.Zero);

        Assert.Contains("room1", affected);
        var state = svc.GetRoomState("room1")!;
        Assert.Empty(state.Users);
    }

    [Fact]
    public void CleanupInactiveParticipants_LargeThreshold_NoAffectedRooms()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        var affected = svc.CleanupInactiveParticipants(TimeSpan.FromHours(1));

        Assert.Empty(affected);
    }

    // ── GetRoomState ──────────────────────────────────────────────────────────

    [Fact]
    public void GetRoomState_UnknownRoom_ReturnsNull()
    {
        var svc = CreateService();
        Assert.Null(svc.GetRoomState("nonexistent"));
    }

    [Fact]
    public void GetRoomState_OnlyIncludesActiveParticipants()
    {
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);
        svc.Disconnect("room1", "conn1");

        var state = svc.GetRoomState("room1")!;
        Assert.Single(state.Users);
    }
}
