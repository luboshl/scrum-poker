namespace ScrumPoker.Domain;

public class Room
{
    public required string Id { get; init; }
    public List<User> Users { get; } = [];
    public bool VotingEnded { get; set; }
}
