using cms24_delta_umbraco.Models;

namespace cms24_delta_umbraco.Interfaces;

public interface ISpotifyService
{
	Task<Track> GetTrackByIdAsync(string id);
	Task<Playlist> GetPlaylistByIdAsync(string id);
	string GetSpotifyLoginUrl();
	Task<string> HandleSpotifyCallbackAsync(string code);
	Task<List<Playlist>> SearchPlaylistByNameAsync(string playlistName);
	Task<List<Playlist>> GetUserPlaylistsAsync(string userId);
	Task SkipToNextTrackAsync();
	Task<List<Track>> SearchTrackByNameAsync(string trackName);
	Task<List<Artist>> SearchArtistsAsync(string artistName);
}
