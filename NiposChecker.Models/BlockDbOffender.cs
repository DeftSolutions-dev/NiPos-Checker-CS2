using Newtonsoft.Json;

namespace NiposChecker.Models;

public class BlockDbOffender
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("steam_ids")]
	public BlockDbSteamId[] SteamIds { get; set; }

	[JsonProperty("ips")]
	public BlockDbIp[] Ips { get; set; }

	[JsonProperty("bans")]
	public BanInfo[] Bans { get; set; }
}
