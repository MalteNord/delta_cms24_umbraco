using cms24_delta_umbraco.Interfaces;
using cms24_delta_umbraco.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;


namespace cms24_delta_umbraco.Services;

public class SpotifyService : ISpotifyService
{
	private readonly string clientId;
	private readonly string clientSecret;
	private readonly HttpClient httpClient;
	private readonly string redirectUri;
	private readonly IHttpContextAccessor httpContextAccessor;
	private readonly ILogger<SpotifyService> _logger;

	public SpotifyService(IOptions<SpotifySettings> settings, IHttpContextAccessor httpContextAccessor,
		ILogger<SpotifyService> logger)
	{
		clientId = settings.Value.ClientId;
		clientSecret = settings.Value.ClientSecret;
		redirectUri = settings.Value.RedirectUri;
		this.httpContextAccessor = httpContextAccessor;
		httpClient = new HttpClient();
		_logger = logger;
	}

	private async Task<string> GetAccessTokenAsync()
	{
		var session = httpContextAccessor.HttpContext?.Session;
		if (session == null)
		{
			throw new Exception("Session is not available");
		}

		// Try to retrieve the token from session
		var accessToken = session.GetString("SpotifyAccessToken");

		// If no token in session, request a new one and store it
		if (string.IsNullOrEmpty(accessToken))
		{
			var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

			var requestBody = new StringContent("grant_type=client_credentials", Encoding.UTF8,
				"application/x-www-form-urlencoded");
			var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestBody);

			if (!response.IsSuccessStatusCode)
				throw new Exception("Failed to retrieve Spotify access token");

			var jsonResponse = await response.Content.ReadAsStringAsync();
			var tokenData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
			accessToken = tokenData.access_token;

			// Store the new access token in session
			session.SetString("SpotifyAccessToken", accessToken);
		}

		return accessToken;
	}

	public async Task<Track> GetTrackByIdAsync(string id)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/tracks/{id}");
		if (!response.IsSuccessStatusCode)
			throw new Exception("Error fetching track data");

		var trackData = await response.Content.ReadAsStringAsync();

		var data = JObject.Parse(trackData);
		var track = new Track
		{
			Id = data["id"]?.ToString(),
			Name = data["name"]?.ToString(),
			ExternalUrl = data["external_urls"]?["spotify"]?.ToString(),
			AlbumCoverUrl = data["album"]?["images"]?[0]?["url"]?.ToString(),
			Artists = data["artists"]?.Select(a => a["name"].ToString()).ToArray(),
			AlbumName = data["album"]?["name"]?.ToString(),
			Uri = data["uri"]?.ToString()
		};

		return track;
	}

	public async Task<Playlist> GetPlaylistByIdAsync(string id)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/playlists/{id}");
		if (!response.IsSuccessStatusCode)
			throw new Exception("Error fetching playlist data");

		var playlistData = await response.Content.ReadAsStringAsync();
		var data = JObject.Parse(playlistData);

		var playlist = new Playlist
		{
			Id = data["id"]?.ToString(),
			Name = data["name"]?.ToString(),
			Description = data["description"]?.ToString(),
			ExternalUrl = data["external_urls"]?["spotify"]?.ToString(),
			PlaylistImageUrl = data["images"]?[0]?["url"]?.ToString(),
			Uri = data["uri"]?.ToString(),
			OwnerName = data["owner"]?["display_name"]?.ToString(),
			OwnerProfileUrl = data["owner"]?["external_urls"]?["spotify"]?.ToString(),
			Tracks = data["tracks"]?["items"]?.Select(data => new Track
			{
				Id = data["track"]?["id"]?.ToString(),
				Name = data["track"]?["name"]?.ToString(),
				ExternalUrl = data["track"]?["external_urls"]?["spotify"]?.ToString(),
				AlbumCoverUrl = data["track"]?["album"]?["images"]?[0]?["url"]?.ToString(),
				Artists = data["track"]?["artists"]?.Select(a => a["name"].ToString()).ToArray(),
				AlbumName = data["track"]?["album"]?["name"]?.ToString(),
				Uri = data["uri"]?.ToString(),
			}).ToList()
		};

		return playlist;
	}

	public string GetSpotifyLoginUrl()
	{
		var scope = "user-read-private user-read-email user-read-playback-state user-modify-playback-state streaming";
		return
			$"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scope)}";
	}

	public async Task<string> HandleSpotifyCallbackAsync(string code)
	{
		var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

		var requestBody = new StringContent(
			$"grant_type=authorization_code&code={code}&redirect_uri={Uri.EscapeDataString(redirectUri)}",
			Encoding.UTF8, "application/x-www-form-urlencoded");

		var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", requestBody);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception("Failed to retrieve Spotify access token");
		}

		var jsonResponse = await response.Content.ReadAsStringAsync();
		var tokenData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
		var accessToken = tokenData.access_token;

		return accessToken; // Just return the access token
	}

	public async Task<List<Playlist>> SearchPlaylistByNameAsync(string playlistName)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		var query = Uri.EscapeDataString(playlistName);
		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/search?q={query}&type=playlist");

		if (!response.IsSuccessStatusCode)
			throw new Exception("Error searching for playlists");

		var searchData = await response.Content.ReadAsStringAsync();
		var data = JObject.Parse(searchData);

		var playlists = data["playlists"]?["items"]?.Select(item => new Playlist
		{
			Id = item["id"]?.ToString(),
			Name = item["name"]?.ToString(),
			Description = item["description"]?.ToString(),
			ExternalUrl = item["external_urls"]?["spotify"]?.ToString(),
			PlaylistImageUrl = item["images"]?[0]?["url"]?.ToString(),
			Uri = item["uri"]?.ToString(),
			OwnerName = item["owner"]?["display_name"]?.ToString(),
			OwnerProfileUrl = item["owner"]?["external_urls"]?["spotify"]?.ToString(),
			OwnerProfileImageUrl = item["owner"]?["images"]?[0]?["url"]?.ToString()
		})
		.Where(playlist => playlist.Name != null && playlist.Name.Contains(playlistName, StringComparison.OrdinalIgnoreCase))
		.ToList();

		return playlists ?? new List<Playlist>();
	}

	public async Task<List<Playlist>> GetUserPlaylistsAsync(string userId)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/users/{userId}/playlists");

		if (!response.IsSuccessStatusCode)
			throw new Exception("Error fetching user playlists");

		var data = JObject.Parse(await response.Content.ReadAsStringAsync());

		var playlists = data["items"]?.Select(item => new Playlist
		{
			Id = item["id"]?.ToString(),
			Name = item["name"]?.ToString(),
			Description = item["description"]?.ToString(),
			ExternalUrl = item["external_urls"]?["spotify"]?.ToString(),
			PlaylistImageUrl = item["images"]?[0]?["url"]?.ToString(),
			Uri = item["uri"]?.ToString(),
			OwnerName = item["owner"]?["display_name"]?.ToString(),
			OwnerProfileUrl = item["owner"]?["external_urls"]?["spotify"]?.ToString(),
			OwnerProfileImageUrl = item["owner"]?["images"]?[0]?["url"]?.ToString()
		}).ToList();

		return playlists ?? new List<Playlist>();
	}

	public async Task SkipToNextTrackAsync()
	{
		Console.WriteLine("SkipToNextTrackAsync called at " + DateTime.Now);

		Console.WriteLine("Custom next logic completed successfully at " + DateTime.Now);
	}

	public async Task<List<Track>> SearchTrackByNameAsync(string trackName)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		trackName = trackName.Trim();
		var query = Uri.EscapeDataString(trackName);
		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/search?q={query}&type=track");

		if (!response.IsSuccessStatusCode)
			throw new Exception("Error searching for tracks");

		var searchData = await response.Content.ReadAsStringAsync();
		var data = JObject.Parse(searchData);

		var tracks = data["tracks"]?["items"]?.Select(item => new Track
		{
			Id = item["id"]?.ToString(),
			Name = item["name"]?.ToString(),
			Artists = item["artists"]?.Select(a => a["name"]?.ToString()).ToArray(),
			ExternalUrl = item["external_urls"]?["spotify"]?.ToString(),
			AlbumCoverUrl = item["album"]?["images"]?[0]?["url"]?.ToString(),
			AlbumName = item["album"]?["name"]?.ToString(),
			Uri = item["uri"]?.ToString()
		})
			.ToList();

		return tracks ?? new List<Track>();
	}

	public async Task<List<Artist>> SearchArtistsAsync(string artistName)
	{
		var accessToken = await GetAccessTokenAsync();
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		artistName = artistName.Trim();
		var query = Uri.EscapeDataString(artistName);
		var response = await httpClient.GetAsync($"https://api.spotify.com/v1/search?q={query}&type=artist");

		if (!response.IsSuccessStatusCode)
			throw new Exception("Error searching for artist");

		var searchData = await response.Content.ReadAsStringAsync();
		var data = JObject.Parse(searchData);

		var artists = data["artists"]?["items"]?.Select(item => new Artist
		{
			Id = item["id"]?.ToString(),
			Name = item["name"]?.ToString(),
			ProfileImageUrl = item["images"] != null && item["images"].Any() ? item["images"][0]["url"]?.ToString() : null
		})
		.ToList();

		return artists ?? new List<Artist>();
	}
}


//.Where(artist => artist.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase))