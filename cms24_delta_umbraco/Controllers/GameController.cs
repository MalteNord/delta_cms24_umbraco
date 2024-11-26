using cms24_delta_umbraco.Hubs;
using cms24_delta_umbraco.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace cms24_delta_umbraco.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class GameController : ControllerBase
	{
		private readonly IGameService _gameService;
		private readonly IHubContext<LobbyHub> _lobbyHubContext;
		private readonly IHubContext<GameHub> _gameHubContext;

		public GameController(
			IGameService gameService,
			IHubContext<LobbyHub> lobbyHubContext,
			IHubContext<GameHub> gameHubContext)
		{
			_gameService = gameService;
			_lobbyHubContext = lobbyHubContext;
			_gameHubContext = gameHubContext;
		}


		[HttpPost]
		public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
		{
			if (request == null)
			{
				return BadRequest(new { message = "Invalid request body" });
			}

			var roomId = await _gameService.CreateRoomAsync(
				host: true,
				playerName: request.PlayerName,
				userId: request.UserId
			);

            var players = await _gameService.GetPlayersInRoomAsync(roomId);

            
            await _lobbyHubContext.Clients.Group(roomId).SendAsync("ReceivePlayers", players);

            return Ok(new { roomId, isHost = true });
		}




		[HttpPost("{roomId}")]
		public async Task<IActionResult> JoinRoom(string roomId, [FromBody] JoinRoomRequest request)
		{
			if (request == null || string.IsNullOrEmpty(request.PlayerName) || string.IsNullOrEmpty(request.UserId))
			{
				return BadRequest(new { message = "Invalid request body. PlayerName and UserId are required." });
			}


			var room = await _gameService.JoinRoomAsync(roomId, request.PlayerName, request.UserId);

			if (room == null)
			{
				return BadRequest(new { success = false, message = "Room does not exist or is no longer available" });
			}


			var player = await _gameService.GetPlayerInRoomAsync(roomId, request.UserId);
			bool isHost = player?.Host ?? false;

            var players = await _gameService.GetPlayersInRoomAsync(roomId);

            
            await _lobbyHubContext.Clients.Group(roomId).SendAsync("ReceivePlayers", players);

            return Ok(new { success = true, roomId = room.RoomId, isHost = isHost });
		}


		[HttpGet("{roomId}/player")]
		public async Task<IActionResult> GetRoomData(string roomId, [FromQuery] string userId)
		{
			if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId))
			{
				return BadRequest(new { message = "Missing roomId or userId." });
			}

			var room = await _gameService.GetRoomByIdAsync(roomId);
			if (room == null)
			{
				return BadRequest(new { message = "Room not found or is no longer active." });
			}

			var player = await _gameService.GetPlayerByUserIdAsync(roomId, userId);
			if (player == null)
			{

				return BadRequest(new { message = $"Player with userId {userId} not found in room {roomId}." });
			}

			return Ok(new
			{
				isHost = player.Host,
				roomId = room.RoomId
			});
		}


		[HttpPost("{roomId}/start")]
		public async Task<IActionResult> StartGame(string roomId)
		{
			var success = await _gameService.StartGameAsync(roomId);

			if (!success)
			{
				return BadRequest(new { success = false, message = "Room not found or already started" });
			}

			return Ok(new { success = true, message = "Game has started!" });
		}


		[HttpPost("{roomId}/end")]
		public async Task<IActionResult> EndGame(string roomId)
		{
			var success = await _gameService.EndGameAsync(roomId);

			if (!success)
			{
				return BadRequest(new { success = false, message = "Room not found or game already ended" });
			}

			// Notify clients via SignalR
			await _gameHubContext.Clients.Group(roomId).SendAsync("GameEnded", new
			{
				roomId,
				message = "Game has ended.",
				players = await _gameService.GetPlayersInRoomAsync(roomId),
				scores = await _gameService.GetScoresAsync(roomId)
			});

			return Ok(new { success = true, message = "Game has ended!" });
		}


		[HttpGet("{roomId}/players")]
		public async Task<IActionResult> GetPlayersInRoom(string roomId)
		{
			var players = await _gameService.GetPlayersInRoomAsync(roomId);
			if (players == null || !players.Any())
			{
				return NotFound(new { message = "No players found in this room." });
			}

			return Ok(players);
		}



		[HttpPost("{roomId}/submitPoints")]
		public async Task<IActionResult> SubmitPoints(string roomId, [FromBody] SubmitPointsRequest request)
		{
			// Allow 0 points for incorrect/empty answers
			if (request == null || string.IsNullOrEmpty(request.UserId))
			{
				return BadRequest(new { message = "Invalid request. UserId is required." });
			}

			var success = await _gameService.AddPointsToUserAsync(roomId, request.UserId, request.Points);
			if (!success)
			{
				return BadRequest(new { success = false, message = "Failed to update score." });
			}

			// Get updated scores and send with user info
			var scores = await _gameService.GetScoresAsync(roomId);
			await _gameHubContext.Clients.Group(roomId).SendAsync("ReceiveUpdatedScores",
				scores,
				new { userId = request.UserId, points = request.Points }
			);

			return Ok(new { success = true });
		}

		[HttpPost("{roomId}/submitAnswer")]
		public async Task<IActionResult> SubmitAnswer(string roomId, [FromBody] SubmitAnswerRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(request.UserId))
				{
					return BadRequest(new { message = "UserId is required." });
				}

				var player = await _gameService.GetPlayerByUserIdAsync(roomId, request.UserId);
				if (player == null)
				{
					return NotFound(new { message = "Player not found." });
				}

				await _gameService.RecordAnswerSubmissionAsync(roomId, request.UserId);

				await _gameHubContext.Clients.Group(roomId).SendAsync("ReceivePlayerSubmission", player.Name);

				return Ok(new { success = true });

			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "An error occurred while processing the answer submission." });
			}
		}


	}
}
