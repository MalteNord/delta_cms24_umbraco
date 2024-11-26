namespace cms24_delta_umbraco.Models;

public class Track
{
	public string Id { get; set; } = null!;
	public string Name { get; set; } = null!;
	public string AlbumName { get; set; } = null!;
	public string[] Artists { get; set; } = null!;
	public string ExternalUrl { get; set; } = null!;
	public string AlbumCoverUrl { get; set; } = null!;
	public string Uri { get; set; } = null!;
}
