namespace ScrumPoker.Application;

public interface IRoomService
{
    JoinResult JoinRoom(string roomId, string connectionId, string name, bool isObserver);
    bool Vote(string roomId, string connectionId, string vote);
    bool CancelVote(string roomId, string connectionId);
    bool EndVoting(string roomId, string connectionId);
    bool ResetVoting(string roomId, string connectionId);
    RemoveUserResult RemoveUser(string roomId, string requesterConnectionId, string targetName);
    void UpdateHeartbeat(string roomId, string connectionId);
    void Disconnect(string roomId, string connectionId);
    RoomStateDto? GetRoomState(string roomId);
    IReadOnlyList<string> CleanupInactiveParticipants(TimeSpan inactivityThreshold);
    void CleanupEmptyRooms(TimeSpan emptyRoomRetention);
}
