namespace ScrumPoker.Domain;

public class Participant
{
    public string ConnectionId { get; set; }
    public string Name { get; set; }
    public string? Vote { get; set; }
    public bool Active { get; set; }
    public bool IsObserver { get; set; }
    public DateTime LastHeartbeat { get; set; }

    public Participant(string connectionId, string name, bool isObserver)
    {
        ConnectionId = connectionId;
        Name = name;
        IsObserver = isObserver;
        Active = true;
        LastHeartbeat = DateTime.UtcNow;
    }
}
