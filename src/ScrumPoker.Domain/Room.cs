namespace ScrumPoker.Domain;

public class Room
{
    public string Id { get; }
    public List<Participant> Participants { get; } = new();
    public bool VotingEnded { get; set; }
    public object SyncRoot { get; } = new object();
    public DateTime? EmptySince { get; set; }

    public Room(string id) => Id = id;
}
