using AwesomeAssertions;
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
        // Arrange
        var svc = CreateService();

        // Act
        var result = svc.JoinRoom("room1", "conn1", "Alice", false);

        // Assert
        result.Success.Should().BeTrue();
        result.ResolvedName.Should().Be("Alice");
        result.IsObserver.Should().BeFalse();
        result.RoomState.Users.Should().ContainSingle();
    }

    [Fact]
    public void JoinRoom_DuplicateName_AppendsCounter()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var result = svc.JoinRoom("room1", "conn2", "Alice", false);

        // Assert
        result.ResolvedName.Should().Be("Alice (2)");
    }

    [Fact]
    public void JoinRoom_TriplicateName_AppendsIncrementingCounter()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Alice", false);

        // Act
        var result = svc.JoinRoom("room1", "conn3", "Alice", false);

        // Assert
        result.ResolvedName.Should().Be("Alice (3)");
    }

    [Fact]
    public void JoinRoom_SameConnectionId_UpdatesExistingParticipant()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var result = svc.JoinRoom("room1", "conn1", "Alice", false);

        // Assert
        result.ResolvedName.Should().Be("Alice");
        result.RoomState.Users.Should().ContainSingle();
    }

    [Fact]
    public void JoinRoom_Observer_IsMarkedAsObserver()
    {
        // Arrange
        var svc = CreateService();

        // Act
        var result = svc.JoinRoom("room1", "conn1", "Scrum Master", true);

        // Assert
        result.IsObserver.Should().BeTrue();
        result.RoomState.Users.Should().ContainSingle().Which.IsObserver.Should().BeTrue();
    }

    [Fact]
    public void JoinRoom_AfterDisconnect_NameNotConflict()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Disconnect("room1", "conn1");

        // Act
        var result = svc.JoinRoom("room1", "conn2", "Alice", false);

        // Assert
        result.ResolvedName.Should().Be("Alice");
    }

    // ── Vote ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Vote_ValidParticipant_RecordsVote()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var voted = svc.Vote("room1", "conn1", "5");
        var state = svc.GetRoomState("room1")!;

        // Assert
        voted.Should().BeTrue();
        state.Users.Should().ContainSingle(user => user.Vote == "5");
    }

    [Fact]
    public void Vote_Observer_Rejected()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "SM", true);

        // Act
        var voted = svc.Vote("room1", "conn1", "5");

        // Assert
        voted.Should().BeFalse();
    }

    [Fact]
    public void Vote_UnknownRoom_ReturnsFalse()
    {
        // Arrange
        var svc = CreateService();

        // Act
        var voted = svc.Vote("nonexistent", "conn1", "5");

        // Assert
        voted.Should().BeFalse();
    }

    // ── CancelVote ────────────────────────────────────────────────────────────

    [Fact]
    public void CancelVote_ClearsExistingVote()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Vote("room1", "conn1", "5");

        // Act
        var cancelled = svc.CancelVote("room1", "conn1");
        var state = svc.GetRoomState("room1")!;

        // Assert
        cancelled.Should().BeTrue();
        state.Users.Should().ContainSingle().Which.Vote.Should().BeNull();
    }

    [Fact]
    public void CancelVote_Observer_Rejected()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "SM", true);

        // Act
        var cancelled = svc.CancelVote("room1", "conn1");

        // Assert
        cancelled.Should().BeFalse();
    }

    // ── EndVoting ─────────────────────────────────────────────────────────────

    [Fact]
    public void EndVoting_SetsVotingEnded()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var ended = svc.EndVoting("room1", "conn1");

        // Assert
        ended.Should().BeTrue();
        svc.GetRoomState("room1")!.VotingEnded.Should().BeTrue();
    }

    [Fact]
    public void EndVoting_AlreadyEnded_ReturnsFalse()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.EndVoting("room1", "conn1");

        // Act
        var second = svc.EndVoting("room1", "conn1");

        // Assert
        second.Should().BeFalse();
    }

    // ── ResetVoting ───────────────────────────────────────────────────────────

    [Fact]
    public void ResetVoting_ClearsVotesAndResetsFlag()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);
        svc.Vote("room1", "conn1", "3");
        svc.Vote("room1", "conn2", "8");
        svc.EndVoting("room1", "conn1");

        // Act
        var reset = svc.ResetVoting("room1", "conn1");
        var state = svc.GetRoomState("room1")!;

        // Assert
        reset.Should().BeTrue();
        state.VotingEnded.Should().BeFalse();
        state.Users.Should().OnlyContain(user => user.Vote == null);
    }

    // ── RemoveUser ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveUser_ByObserver_Succeeds()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var result = svc.RemoveUser("room1", "observer1", "Alice");

        // Assert
        result.Success.Should().BeTrue();
        result.RemovedConnectionId.Should().Be("conn1");
        result.RoomState!.Users.Should().ContainSingle(user => user.Name == "SM");
    }

    [Fact]
    public void RemoveUser_ByNonObserver_Rejected()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);

        // Act
        var result = svc.RemoveUser("room1", "conn1", "Bob");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("observer");
    }

    [Fact]
    public void RemoveUser_Self_Rejected()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);

        // Act
        var result = svc.RemoveUser("room1", "observer1", "SM");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("yourself");
    }

    [Fact]
    public void RemoveUser_NotFound_Rejected()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "observer1", "SM", true);

        // Act
        var result = svc.RemoveUser("room1", "observer1", "Ghost");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("not found");
    }

    // ── Heartbeat / Disconnect ────────────────────────────────────────────────

    [Fact]
    public void UpdateHeartbeat_UpdatesTimestamp()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        Action act = () => svc.UpdateHeartbeat("room1", "conn1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Disconnect_RemovesParticipantFromState()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);

        // Act
        svc.Disconnect("room1", "conn1");
        var state = svc.GetRoomState("room1")!;

        // Assert
        state.Users.Should().ContainSingle(user => user.Name == "Bob");
    }

    // ── CleanupInactiveParticipants ───────────────────────────────────────────

    [Fact]
    public void CleanupInactiveParticipants_RemovesStaleParticipants()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var affected = svc.CleanupInactiveParticipants(TimeSpan.Zero);

        // Assert
        affected.Should().Contain("room1");
    }

    [Fact]
    public void CleanupInactiveParticipants_EmptiesRoom_SetsEmptySinceInsteadOfRemoving()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        svc.CleanupInactiveParticipants(TimeSpan.Zero);

        // Assert
        svc.GetRoomState("room1").Should().NotBeNull();
        svc.GetRoomState("room1")!.Users.Should().BeEmpty();
    }

    [Fact]
    public void CleanupInactiveParticipants_LargeThreshold_NoAffectedRooms()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        var affected = svc.CleanupInactiveParticipants(TimeSpan.FromHours(1));

        // Assert
        affected.Should().BeEmpty();
    }

    [Fact]
    public void Disconnect_LastParticipant_SetsRoomEmptySince()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        svc.Disconnect("room1", "conn1");

        // Assert
        svc.GetRoomState("room1").Should().NotBeNull();
        svc.GetRoomState("room1")!.Users.Should().BeEmpty();
    }

    // ── CleanupEmptyRooms ─────────────────────────────────────────────────────

    [Fact]
    public void CleanupEmptyRooms_RemovesRoomEmptyLongerThanRetention()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Disconnect("room1", "conn1");

        // Act
        svc.CleanupEmptyRooms(TimeSpan.Zero);

        // Assert
        svc.GetRoomState("room1").Should().BeNull();
    }

    [Fact]
    public void CleanupEmptyRooms_KeepsRoomEmptyForLessThanRetention()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Disconnect("room1", "conn1");

        // Act
        svc.CleanupEmptyRooms(TimeSpan.FromHours(1));

        // Assert
        svc.GetRoomState("room1").Should().NotBeNull();
    }

    [Fact]
    public void CleanupEmptyRooms_DoesNotRemoveRoomWithActiveParticipants()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);

        // Act
        svc.CleanupEmptyRooms(TimeSpan.Zero);

        // Assert
        svc.GetRoomState("room1").Should().NotBeNull();
        svc.GetRoomState("room1")!.Users.Should().ContainSingle(u => u.Name == "Alice");
    }

    [Fact]
    public void CleanupEmptyRooms_AfterInactiveCleanup_RemovesRoom()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.CleanupInactiveParticipants(TimeSpan.Zero);

        // Act
        svc.CleanupEmptyRooms(TimeSpan.Zero);

        // Assert
        svc.GetRoomState("room1").Should().BeNull();
    }

    [Fact]
    public void JoinRoom_WhenRoomWasEmpty_ClearsEmptySince()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.Disconnect("room1", "conn1");

        // Act - rejoin before retention expires
        svc.JoinRoom("room1", "conn2", "Bob", false);

        // Assert - room should not be removed even with zero retention because EmptySince was cleared
        svc.CleanupEmptyRooms(TimeSpan.Zero);
        svc.GetRoomState("room1").Should().NotBeNull();
        svc.GetRoomState("room1")!.Users.Should().ContainSingle(u => u.Name == "Bob");
    }

    // ── GetRoomState ──────────────────────────────────────────────────────────

    [Fact]
    public void GetRoomState_UnknownRoom_ReturnsNull()
    {
        // Arrange
        var svc = CreateService();

        // Act
        var state = svc.GetRoomState("nonexistent");

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetRoomState_OnlyIncludesActiveParticipants()
    {
        // Arrange
        var svc = CreateService();
        svc.JoinRoom("room1", "conn1", "Alice", false);
        svc.JoinRoom("room1", "conn2", "Bob", false);

        // Act
        svc.Disconnect("room1", "conn1");
        var state = svc.GetRoomState("room1")!;

        // Assert
        state.Users.Should().ContainSingle(user => user.Name == "Bob");
    }
}
