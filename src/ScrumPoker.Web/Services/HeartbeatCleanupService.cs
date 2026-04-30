using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using ScrumPoker.Application;
using ScrumPoker.Web.Hubs;

namespace ScrumPoker.Web.Services;

public class HeartbeatCleanupService : BackgroundService
{
    private readonly IRoomService _roomService;
    private readonly IHubContext<ScrumPokerHub> _hubContext;
    private readonly ILogger<HeartbeatCleanupService> _logger;
    private readonly IOptions<RoomCleanupOptions> _options;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(10);

    public HeartbeatCleanupService(
        IRoomService roomService,
        IHubContext<ScrumPokerHub> hubContext,
        ILogger<HeartbeatCleanupService> logger,
        IOptions<RoomCleanupOptions> options)
    {
        _roomService = roomService;
        _hubContext = hubContext;
        _logger = logger;
        _options = options;
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

                _roomService.CleanupEmptyRooms(_options.Value.EmptyRoomRetention);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during heartbeat cleanup");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}
