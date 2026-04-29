namespace ScrumPoker.Application;

public record RoomStateDto(
    IReadOnlyList<ParticipantDto> Users,
    bool VotingEnded
);
