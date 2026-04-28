using ScrumPoker.Domain;

namespace ScrumPoker.Application;

public interface IRoomStore
{
    Room GetOrCreate(string roomId);
    Room? Get(string roomId);
    IReadOnlyList<Room> GetAll();
}
