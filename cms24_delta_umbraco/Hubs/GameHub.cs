using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace cms24_delta_umbraco.Hubs;

public class GameHub : Hub
{
	private readonly IGameService _gameService;
	private readonly ILogger<GameHub> _logger;
	private string? _lastTrackId;

	public GameHub(IGameService gameService, ILogger<GameHub> logger)
	{
		_gameService = gameService;
		_logger = logger;
	}

	public async Task FetchPlayersInRoom(string roomId)
	{
		_logger.LogInformation("Fetching players in room with ID: {RoomId}", roomId);
		var players = await _gameService.GetPlayersInRoomAsync(roomId);

		if (players != null && players.Any())
		{
			_logger.LogInformation("Players found in room {RoomId}: {Players}", roomId, string.Join(", ", players));
		}
		else
		{
			_logger.LogWarning("No players found in room with ID: {RoomId}", roomId);
		}

		await Clients.Group(roomId).SendAsync("ReceivePlayers", players);
		_logger.LogInformation("Sent ReceivePlayers event to clients in room {RoomId}. Players: {Players}", roomId, string.Join(", ", players));
	}

	public async Task UpdatePlayerScoreAsync(string roomId, string userId, int points)
	{
		_logger.LogInformation("Updating player score in room {RoomId} for userId {UserId}", roomId, userId);

		var success = await _gameService.AddPointsToUserAsync(roomId, userId, points);

		if (success)
		{
			_logger.LogInformation("Score updated successfully for userId {UserId} in room {RoomId}", userId, roomId);
			
			var scores = await _gameService.GetScoresAsync(roomId);
			
			// Log before broadcasting
			_logger.LogInformation("Broadcasting updated scores to room {RoomId}: {Scores}", roomId, string.Join(", ", scores.Select(kv => $"{kv.Key}: {kv.Value}")));

			await Clients.Group(roomId).SendAsync("ReceiveUpdatedScores", scores);
		}
		else
		{
			_logger.LogWarning("Failed to update score for userId {UserId} in room {RoomId}", userId, roomId);
		}
	}





	public async Task FetchScores(string roomId)
	{
		_logger.LogInformation("Fetching scores for room {RoomId}", roomId);
		var scores = await _gameService.GetScoresAsync(roomId);

		if (scores != null)
		{
			await Clients.Caller.SendAsync("ReceiveScores", scores);
			_logger.LogInformation("Sent scores to caller: {Scores}", string.Join(", ", scores.Select(kv => $"{kv.Key}: {kv.Value}")));
		}
		else
		{
			_logger.LogWarning("No scores found for room {RoomId}", roomId);
		}
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
		if (!string.IsNullOrEmpty(roomId))
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
			_logger.LogInformation("Client disconnected from room {RoomId}", roomId);
		}
		await base.OnDisconnectedAsync(exception);
	}

	public async Task TrackChanged(string roomId, string trackId)
	{
		try
		{
			// Only send notification if track has actually changed
			if (_lastTrackId != trackId)
			{
				_logger.LogInformation($"Sending track change notification to room {roomId} for track {trackId}");
				await Clients.Group(roomId).SendAsync("OnTrackChanged", trackId);
				_logger.LogInformation($"Track change notification sent successfully to room {roomId}");
				_lastTrackId = trackId;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error sending track change notification to room {roomId}");
			throw;
		}
	}

public async Task EndGame(string roomId)
{
    try
    {
        _logger.LogInformation("Attempting to end game for room {RoomId}", roomId);

        var room = await _gameService.GetRoomByIdAsync(roomId);

        if (room == null || room.IsEnded == true)
        {
            _logger.LogWarning("Room {RoomId} not found or already ended", roomId);
            return;
        }

		await _gameService.ClearRoundSubmissionsAsync(roomId);
        await _gameService.EndGameAsync(roomId);

        var players = (await _gameService.GetPlayersInRoomAsync(roomId))
    	.Select(p => new { p.UserId, p.Name, p.Host }) // Explicitly map to required properties
    	.ToList();
        var scores = await _gameService.GetScoresAsync(roomId);

        _logger.LogInformation("Fetched players and scores for room {RoomId}", roomId);

        await Clients.Group(roomId).SendAsync("GameEnded", new
        {
            roomId,
            message = "Game has ended.",
            players,
            scores,
        });

        _logger.LogInformation("Game ended notification sent to room {RoomId}", roomId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error ending game for room {RoomId}", roomId);
    }
}

public Task KeepAlive()
{
    _logger.LogDebug("Keep-alive ping received from client");
    return Task.CompletedTask;
}

}
