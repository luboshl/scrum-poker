namespace ScrumPoker.Application;

public record JoinResult(
    bool Success,
    string ResolvedName,
    bool IsObserver,
    RoomStateDto RoomState
);
