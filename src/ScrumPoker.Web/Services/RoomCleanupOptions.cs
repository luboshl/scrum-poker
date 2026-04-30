namespace ScrumPoker.Web.Services;

public class RoomCleanupOptions
{
    public const string SectionName = "RoomCleanup";

    public TimeSpan EmptyRoomRetention { get; set; } = TimeSpan.FromHours(1);
}
