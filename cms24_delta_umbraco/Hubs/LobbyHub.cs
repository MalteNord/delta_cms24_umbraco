
using Microsoft.AspNetCore.SignalR;
using cms24_delta_umbraco.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace cms24_delta_umbraco.Hubs;
public class LobbyHub : Hub
{
    private readonly IGameService _gameService;
    private readonly ILogger<LobbyHub> _logger;

    public LobbyHub(IGameService gameService, ILogger<LobbyHub> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    public async Task JoinLobby(string roomId, string playerName, string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("Client with ConnectionId {ConnectionId} joined group {RoomId}", Context.ConnectionId, roomId);

        // Add player to the room
        await _gameService.JoinRoomAsync(roomId, playerName, userId);

        // Fetch and broadcast updated players list
        await FetchPlayersInRoom(roomId);
    }

    public async Task FetchPlayersInRoom(string roomId)
    {
        _logger.LogInformation("Fetching players in room with ID: {RoomId}", roomId);
        var players = await _gameService.GetPlayersInRoomAsync(roomId);

        if (players != null && players.Any())
        {
            _logger.LogInformation("Players found in room {RoomId}: {Players}", roomId, string.Join(", ", players.Select(p => p.Name)));
        }
        else
        {
            _logger.LogWarning("No players found in room with ID: {RoomId}", roomId);
        }

        await Clients.Group(roomId).SendAsync("ReceivePlayers", players);
        _logger.LogInformation("Sent ReceivePlayers event to clients in room {RoomId}. Players: {Players}", roomId, string.Join(", ", players.Select(p => p.Name)));
    }

    public override async Task OnConnectedAsync()
    {
        var roomId = Context.GetHttpContext().Request.Query["roomId"];
        if (!string.IsNullOrEmpty(roomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            _logger.LogInformation("Client connected to room {RoomId}", roomId);
        }
        else
        {
            _logger.LogWarning("Client connected without roomId.");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var roomId = Context.GetHttpContext().Request.Query["roomId"];
        var userId = Context.User?.FindFirst("UserId")?.Value; // Adjust based on your authentication

        if (!string.IsNullOrEmpty(roomId) && !string.IsNullOrEmpty(userId))
        {
            var player = await _gameService.GetPlayerByUserIdAsync(roomId, userId);
            if (player != null && player.Host)
            {
                // Fetch the next player to assign as host
                var nextPlayer = (await _gameService.GetPlayersInRoomAsync(roomId))
                                  .FirstOrDefault(p => p.UserId != userId);

                if (nextPlayer != null)
                {
                    // Assign the new host
                    nextPlayer.Host = true;
                    await _gameService.UpdatePlayerAsync(nextPlayer);
                }
            }

            // Remove the player from the room
            await _gameService.RemovePlayerAsync(roomId, userId);

            // Broadcast the updated players list
            await FetchPlayersInRoom(roomId);
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartGame(string roomId)
    {
        // Notify all clients in the room to start the game
        await Clients.Group(roomId).SendAsync("StartGame");
    }
}
