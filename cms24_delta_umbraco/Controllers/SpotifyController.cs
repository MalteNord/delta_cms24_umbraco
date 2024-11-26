using cms24_delta_umbraco.Interfaces;
using cms24_delta_umbraco.Models;
using Microsoft.AspNetCore.Mvc;

namespace cms24_delta_umbraco.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SpotifyController : ControllerBase
{
	private readonly ISpotifyService spotifyService;

	public SpotifyController(ISpotifyService spotifyService)
	{
		this.spotifyService = spotifyService;
	}

	[HttpGet("track/{id}")]
	public async Task<IActionResult> GetTrackById(string id)
	{
		try
		{
			Track track = await spotifyService.GetTrackByIdAsync(id);

			if (track == null)
			{
				return NotFound("Track not found");
			}

			return Ok(track); // Return the track as JSON
		}
		catch (Exception ex)
		{
			// Return a 500 error if something goes wrong
			return StatusCode(500, $"Internal server error: {ex.Message}");
		}
	}

	[HttpGet("playlist/{id}")]
	public async Task<IActionResult> GetPlayListById(string id)
	{
		try
		{
			Playlist playlist = await spotifyService.GetPlaylistByIdAsync(id);

			if (playlist == null)
			{
				return NotFound("Playlist not found");
			}

			return Ok(playlist); // Return the playlist as JSON
		}
		catch (Exception ex)
		{
			// Return a 500 error if something goes wrong
			return StatusCode(500, $"Internal server error: {ex.Message}");
		}
	}

	[HttpGet("login")]
	public IActionResult Login()
	{
		var loginUrl = spotifyService.GetSpotifyLoginUrl();
		return Redirect(loginUrl); // Redirect to Spotify's login page
	}

	[HttpGet("callback")]
	public async Task<IActionResult> Callback(string code)
	{
		try
		{
			// Get the access token from the Spotify service
			var accessToken = await spotifyService.HandleSpotifyCallbackAsync(code);

			return Ok(new { accessToken });
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Error during Spotify login: {ex.Message}");
		}
	}

	[HttpGet("search")]
	public async Task<IActionResult> SearchPlaylists(string query)
	{
		if (string.IsNullOrEmpty(query))
			return BadRequest("Query parameter is required");

		try
		{
			var playlists = await spotifyService.SearchPlaylistByNameAsync(query);

			if (playlists == null || !playlists.Any())
				return NotFound("No playlist found matching the query");

			return Ok(playlists);
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Internal server error: {ex.Message}");
		}
	}

	[HttpGet("user/{userId}/playlists")]
	public async Task<IActionResult> GetUserPlaylists(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			return BadRequest("User ID is required");

		try
		{
			var playlists = await spotifyService.GetUserPlaylistsAsync(userId);

			if (playlists == null || !playlists.Any())
				return NotFound("No playlists found for the specified user");

			return Ok(playlists);
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Internal server error: {ex.Message}");
		}
	}

	private static int songVersion = 0;

	[HttpPost("next")]
	public async Task<IActionResult> NextSong()
	{
		try
		{
			// Logic to skip to the next track
			songVersion++; // Increment the song version
			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[HttpGet("songversion")]
	public async Task<IActionResult> GetSongVersion()
	{
		try
		{
			return Ok(songVersion);
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[HttpGet("trackname")]
	public async Task<IActionResult> SearchTrackByName(string query)
	{
		try
		{
			if (string.IsNullOrEmpty(query))
				return BadRequest("Query parameter is required");

			var tracks = await spotifyService.SearchTrackByNameAsync(query);

			if (tracks == null || !tracks.Any())
				return NotFound("No tracks found matching the query");

			return Ok(tracks);
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

	[HttpGet("artistname")]
	public async Task<IActionResult> SearchArtistByName(string query)
	{
		try
		{
			if (string.IsNullOrEmpty(query))
				return BadRequest("Query parameter is required");

			var artists = await spotifyService.SearchArtistsAsync(query);

			if (artists == null || !artists.Any())
				return NotFound("No tracks found matching the query");

			return Ok(artists);
		}
		catch (Exception ex)
		{
			return StatusCode(500, ex.Message);
		}
	}

}