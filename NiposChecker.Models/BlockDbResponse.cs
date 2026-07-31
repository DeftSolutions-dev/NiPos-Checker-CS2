using Newtonsoft.Json;

namespace NiposChecker.Models;

public class BlockDbResponse
{
	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("bans")]
	public BanInfo[] Bans { get; set; }
}
