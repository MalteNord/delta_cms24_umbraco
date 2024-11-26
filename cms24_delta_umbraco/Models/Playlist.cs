namespace cms24_delta_umbraco.Models;

public class Playlist
{
	public string Id { get; set; } = null!;
	public string Name { get; set; } = null!;
	public string Description { get; set; } = null!;
	public string ExternalUrl { get; set; } = null!;
	public string PlaylistImageUrl { get; set; } = null!;
	public string Uri { get; set; } = null!;
	public string OwnerName { get; set; } = null!;
	public string OwnerProfileUrl { get; set; } = null!;
	public string OwnerProfileImageUrl { get; set; } = null!;
	public List<Track> Tracks { get; set; }
}
