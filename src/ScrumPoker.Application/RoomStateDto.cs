namespace ScrumPoker.Application;

public record RoomStateDto(
    IReadOnlyList<ParticipantDto> Users,
    bool VotingEnded
);

public record ParticipantDto(
    string Name,
    string? Vote,
    bool IsObserver
);
