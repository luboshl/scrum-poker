using AwesomeAssertions;
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
        // Arrange
        var confirmationSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _connection.On<object>("joinConfirmation", payload =>
        {
            confirmationSource.TrySetResult(payload);
        });

        // Act
        await _connection.InvokeAsync("JoinRoom", "test-room", "Alice", false);
        var confirmation = await confirmationSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        confirmation.Should().NotBeNull();
    }

    [Fact]
    public async Task JoinRoom_ReceivesRoomUpdate()
    {
        // Arrange
        var roomUpdateSource = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            roomUpdateSource.TrySetResult(state);
        });

        // Act
        await _connection.InvokeAsync("JoinRoom", "room-update-test", "Bob", false);
        var state = await roomUpdateSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        state.Should().NotBeNull();
        state.Users.Should().ContainSingle().Which.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task Vote_AfterJoin_RoomUpdateContainsVote()
    {
        // Arrange
        var joinConfirmationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinConfirmationSource.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "vote-test-room", "Charlie", false);
        await joinConfirmationSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var roomUpdateSource = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            if (state.Users.Any(u => u.Vote == "5"))
            {
                roomUpdateSource.TrySetResult(state);
            }
        });

        // Act
        await _connection.InvokeAsync("Vote", "5");
        var state = await roomUpdateSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        state.Users.Should().ContainSingle(user => user.Name == "Charlie").Which.Vote.Should().Be("5");
    }

    [Fact]
    public async Task EndVoting_SetsVotingEnded()
    {
        // Arrange
        var joinConfirmationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinConfirmationSource.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "end-voting-room", "Dave", false);
        await joinConfirmationSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var roomUpdateSource = new TaskCompletionSource<RoomStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<RoomStateDto>("roomUpdate", state =>
        {
            if (state.VotingEnded)
            {
                roomUpdateSource.TrySetResult(state);
            }
        });

        // Act
        await _connection.InvokeAsync("EndVoting");
        var state = await roomUpdateSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        state.VotingEnded.Should().BeTrue();
    }

    [Fact]
    public async Task ResetVoting_ClearsVotes()
    {
        // Arrange
        var joinConfirmationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On<object>("joinConfirmation", _ => joinConfirmationSource.TrySetResult(true));
        await _connection.InvokeAsync("JoinRoom", "reset-voting-room", "Eve", false);
        await joinConfirmationSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await _connection.InvokeAsync("Vote", "13");
        await _connection.InvokeAsync("EndVoting");

        var resetSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.On("resetVoting", () => resetSource.TrySetResult(true));

        // Act
        await _connection.InvokeAsync("ResetVoting");
        var receivedReset = await resetSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        receivedReset.Should().BeTrue();
    }
}
