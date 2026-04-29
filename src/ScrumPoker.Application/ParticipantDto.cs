namespace ScrumPoker.Application;

public record ParticipantDto(
    string Name,
    string? Vote,
    bool IsObserver
);
