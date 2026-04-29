namespace ScrumPoker.Application;

public record RemoveUserResult(
    bool Success,
    string? ErrorMessage,
    string? RemovedConnectionId,
    RoomStateDto? RoomState
);
