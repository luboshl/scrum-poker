namespace ScrumPoker.Domain;

public class User
{
    public required string ConnectionId { get; set; }
    public required string Name { get; set; }
    public string? Vote { get; set; }
    public bool IsObserver { get; set; }
    public bool IsActive { get; set; } = true;
}
