using Microsoft.AspNetCore.SignalR;
using ScrumPoker.Application;
using ScrumPoker.Web.Hubs;

namespace ScrumPoker.Web.Services;

public class HeartbeatCleanupService : BackgroundService
{
    private readonly IRoomService _roomService;
    private readonly IHubContext<ScrumPokerHub> _hubContext;
    private readonly ILogger<HeartbeatCleanupService> _logger;
    private readonly TimeSpan _emptyRoomRetention;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DefaultEmptyRoomRetention = TimeSpan.FromHours(1);

    public HeartbeatCleanupService(
        IRoomService roomService,
        IHubContext<ScrumPokerHub> hubContext,
        ILogger<HeartbeatCleanupService> logger,
        IConfiguration configuration)
    {
        _roomService = roomService;
        _hubContext = hubContext;
        _logger = logger;
        _emptyRoomRetention = configuration.GetValue<TimeSpan?>("RoomCleanup:EmptyRoomRetention") ?? DefaultEmptyRoomRetention;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var affectedRooms = _roomService.CleanupInactiveParticipants(InactivityThreshold);
                foreach (var roomId in affectedRooms)
                {
                    var state = _roomService.GetRoomState(roomId);
                    if (state != null)
                    {
                        await _hubContext.Clients.Group(roomId).SendAsync("roomUpdate", state, stoppingToken);
                    }
                    _logger.LogInformation("Cleaned up inactive participants in room {RoomId}", roomId);
                }

                _roomService.CleanupEmptyRooms(_emptyRoomRetention);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during heartbeat cleanup");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}
