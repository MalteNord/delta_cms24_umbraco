using cms24_delta_umbraco.Models;

public interface IGameService
{
	Task<string> CreateRoomAsync(bool host, string playerName, string userId);
	Task<Room?> JoinRoomAsync(string roomId, string playerName, string userId);
	Task<bool> StartGameAsync(string roomId);
	Task<bool> EndGameAsync(string roomId);
	Task<Player?> GetPlayerInRoomAsync(string roomId, string playerName);
	Task<Room?> GetRoomByIdAsync(string roomId);
	Task<Player?> GetPlayerByUserIdAsync(string roomId, string userId);
	Task<IEnumerable<Player?>> GetPlayersInRoomAsync(string roomId);
	Task<bool> UpdatePlayerScoreAsync(string roomId, string userId, int score);
	Task<Dictionary<string, int>> GetScoresAsync(string roomId);
	Task<bool> AddPointsToUserAsync(string roomId, string userId, int points);
	Task UpdatePlayerAsync(Player player);
	Task RemovePlayerAsync(string roomId, string userId);
	Task<bool> RecordAnswerSubmissionAsync(string roomId, string userId);
	Task<bool> HasPlayerSubmittedAnswerAsync(string roomId, string userId);
	Task ClearRoundSubmissionsAsync(string roomId);


}
