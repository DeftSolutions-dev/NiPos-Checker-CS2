using Newtonsoft.Json;

namespace NiposChecker.Models;

public class BlockDbSteamId
{
	[JsonProperty("steamid64")]
	public string SteamId64 { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }
}
