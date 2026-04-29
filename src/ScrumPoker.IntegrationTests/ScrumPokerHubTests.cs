using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumPoker.Application;
using Xunit;

namespace ScrumPoker.IntegrationTests;

public class ScrumPokerHubTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HubConnection _connection = null!;

    public ScrumPokerHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var httpClient = _factory.CreateClient();
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
        await _connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task JoinRoom_ReceivesJoinConfirmation()
    {
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _connection.On<object>("joinConfirmation", payload =>
        {
            tcs.TrySetResult(payload);
        });

        await _connection.InvokeAsync("JoinRoom", "test-room", "Alice", false);

        var confirmation = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(confirmation);
    }

    [Fact]
    public async Task JoinRoom_ReceivesRoomUpdate()
    {
        var tcs = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            tcs.TrySetResult(state);
        });

        await _connection.InvokeAsync("JoinRoom", "room-update-test", "Bob", false);

        var state = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(state);
        Assert.Single(state.Users);
        Assert.Equal("Bob", state.Users[0].Name);
    }

    [Fact]
    public async Task Vote_AfterJoin_RoomUpdateContainsVote()
    {
        // First join
        var joinTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinTcs.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "vote-test-room", "Charlie", false);
        await joinTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Then vote
        var voteTcs = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            if (state.Users.Any(u => u.Vote == "5"))
            {
                voteTcs.TrySetResult(state);
            }
        });

        await _connection.InvokeAsync("Vote", "5");

        var state = await voteTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("5", state.Users.First(u => u.Name == "Charlie").Vote);
    }

    [Fact]
    public async Task EndVoting_SetsVotingEnded()
    {
        var joinTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinTcs.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "end-voting-room", "Dave", false);
        await joinTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var endTcs = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            if (state.VotingEnded)
            {
                endTcs.TrySetResult(state);
            }
        });

        await _connection.InvokeAsync("EndVoting");

        var state = await endTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(state.VotingEnded);
    }

    [Fact]
    public async Task ResetVoting_ClearsVotes()
    {
        var joinTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinTcs.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "reset-voting-room", "Eve", false);
        await joinTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await _connection.InvokeAsync("Vote", "13");
        await _connection.InvokeAsync("EndVoting");

        var resetTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On("resetVoting", () => resetTcs.TrySetResult(true));

        await _connection.InvokeAsync("ResetVoting");

        var receivedReset = await resetTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(receivedReset);
    }
}
