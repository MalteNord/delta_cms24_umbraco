using cms24_delta_umbraco.Contexts;
using cms24_delta_umbraco.Models;
using Microsoft.EntityFrameworkCore;


public class GameService : IGameService
{
	private readonly AppDbContext _context;
	private readonly ILogger<GameService> _logger;
	private static readonly Dictionary<string, DateTime> _currentRoundSubmissions = new();

	public GameService(AppDbContext context, ILogger<GameService> logger)
	{
		_context = context;
		_logger = logger;
	}

	public async Task<IEnumerable<Player>> GetPlayersInRoomAsync(string roomId)
	{
		return await _context.Players
			.Where(p => p.RoomId == roomId)
			.ToListAsync();
	}

	public async Task<string> CreateRoomAsync(bool host, string playerName, string userId)
	{
		var roomId = GenerateUniqueRoomId();
		var room = new Room
		{
			RoomId = roomId,
			CreatedAt = DateTime.Now,
			IsActive = true,
			IsEnded = false,
		};

		_context.Rooms.Add(room);

		var player = new Player
		{
			Name = playerName,
			Host = host,
			RoomId = roomId,
			UserId = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId
		};

		_context.Players.Add(player);
		await _context.SaveChangesAsync();

		return room.RoomId;
	}

	public async Task<Room?> JoinRoomAsync(string roomId, string playerName, string userId)
	{
		var room = await _context.Rooms.Include(r => r.Players)
			.FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsActive && !r.IsEnded);

		if (room == null)
		{
			return null;
		}


		var existingPlayer = room.Players.FirstOrDefault(p => p.UserId == userId);
		if (existingPlayer != null)
		{

			return room;
		}


		var player = new Player
		{
			Name = playerName,
			UserId = userId,
			Host = room.Players.Count == 0,
			RoomId = roomId
		};

		_context.Players.Add(player);
		await _context.SaveChangesAsync();

		return room;
	}

	public async Task<bool> StartGameAsync(string roomId)
	{
		var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId && r.IsActive);

		if (room == null) return false;

		room.IsActive = false;
		room.IsEnded = false;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> EndGameAsync(string roomId)
	{
		try
		{
			_logger.LogInformation("Attempting to end game for room {RoomId}", roomId);

			var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
			if (room == null)
			{
				_logger.LogWarning("Room {RoomId} not found", roomId);
				return false;
			}

			if (room.IsEnded == true)
			{
				_logger.LogWarning("Room {RoomId} has already ended", roomId);
				return false;
			}

			_logger.LogInformation("Before update: IsEnded = {IsEnded}", room.IsEnded);

			room.IsEnded = true;

			_logger.LogInformation("After update in memory: IsEnded = {IsEnded}", room.IsEnded);

			var entry = _context.Entry(room);
			if (entry.State != EntityState.Modified)
			{
				entry.State = EntityState.Modified;
				_logger.LogInformation("Entity State changed to: {State}", entry.State);
			}

			await _context.SaveChangesAsync();

			var updatedRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
			_logger.LogInformation("After SaveChangesAsync: Database IsEnded = {IsEnded}", updatedRoom?.IsEnded);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save changes while ending room {RoomId}", roomId);
			return false;
		}
	}

	public async Task<Player?> GetPlayerInRoomAsync(string roomId, string playerName)
	{
		return await _context.Players
			.FirstOrDefaultAsync(p => p.RoomId == roomId && p.Name == playerName);
	}

	private string GenerateUniqueRoomId()
	{
		var random = new Random();
		string roomId;

		do
		{
			roomId = random.Next(1000, 9999).ToString();
		} while (_context.Rooms.Any(r => r.RoomId == roomId));

		return roomId;
	}

	public async Task<Room?> GetRoomByIdAsync(string roomId)
	{
		return await _context.Rooms.Include(r => r.Players).FirstOrDefaultAsync(r => r.RoomId == roomId);
	}

	public async Task<Player?> GetPlayerByUserIdAsync(string roomId, string userId)
	{
		return await _context.Players.FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);
	}

	public async Task<bool> UpdatePlayerScoreAsync(string roomId, string userId, int newScore)
	{
		var player = await _context.Players.FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);

		if (player == null)
		{
			return false;
		}

		player.Score = newScore;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> AddPointsToUserAsync(string roomId, string userId, int points)
	{
		var player = await _context.Players
			.FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);

		if (player == null)
		{
			_logger.LogWarning("Player not found in room {RoomId} with userId {UserId}", roomId, userId);
			return false;
		}

		_logger.LogInformation("Adding {Points} points to player {PlayerName} (userId {UserId}) in room {RoomId}. Current score: {CurrentScore}",
			points, player.Name, userId, roomId, player.Score);

		player.Score += points;

		try
		{
			_context.Players.Update(player);
			await _context.SaveChangesAsync();
			_logger.LogInformation("Updated score for player {PlayerName}: {NewScore}", player.Name, player.Score);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating score for player {PlayerName} in room {RoomId}", player.Name, roomId);
			return false;
		}
	}

	public async Task<Dictionary<string, int>> GetScoresAsync(string roomId)
	{
		_logger.LogInformation("Fetching scores for room {RoomId}", roomId);

		try
		{
			var scores = await _context.Players
				.Where(p => p.RoomId == roomId)
				.ToDictionaryAsync(p => p.Name, p => p.Score);

			_logger.LogInformation("Scores fetched for room {RoomId}: {Scores}", roomId, string.Join(", ", scores.Select(kv => $"{kv.Key}: {kv.Value}")));
			return scores;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching scores for room {RoomId}", roomId);
			return new Dictionary<string, int>();
		}
	}

	public async Task<bool> RecordAnswerSubmissionAsync(string roomId, string userId)
	{
		try
		{
			var player = await GetPlayerByUserIdAsync(roomId, userId);
			if (player == null)
			{
				_logger.LogWarning("Player not found when recording answer submission. RoomId: {RoomId}, UserId: {UserId}", roomId, userId);
				return false;
			}

			string submissionKey = $"{roomId}_{userId}";
			_currentRoundSubmissions[submissionKey] = DateTime.UtcNow;

			_logger.LogInformation("Recorded answer submission for player {PlayerName} in room {RoomId}", player.Name, roomId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error recording answer submission. RoomId: {RoomId}, UserId: {UserId}", roomId, userId);
			return false;
		}
	}

	public async Task<bool> HasPlayerSubmittedAnswerAsync(string roomId, string userId)
	{
		string submissionKey = $"{roomId}_{userId}";
		return _currentRoundSubmissions.ContainsKey(submissionKey);
	}

	public async Task ClearRoundSubmissionsAsync(string roomId)
	{
		var keysToRemove = _currentRoundSubmissions.Keys
			.Where(k => k.StartsWith($"{roomId}_"))
			.ToList();

		foreach (var key in keysToRemove)
		{
			_currentRoundSubmissions.Remove(key);
		}

		_logger.LogInformation("Cleared round submissions for room {RoomId}", roomId);
	}


	public async Task UpdatePlayerAsync(Player player)
	{
		_context.Players.Update(player);
		await _context.SaveChangesAsync();
	}

	public async Task RemovePlayerAsync(string roomId, string userId)
	{
		var player = await _context.Players.FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);
		if (player != null)
		{
			_context.Players.Remove(player);
			await _context.SaveChangesAsync();
		}
	}


}
