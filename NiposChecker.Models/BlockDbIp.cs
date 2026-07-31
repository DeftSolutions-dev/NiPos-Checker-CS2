using Newtonsoft.Json;

namespace NiposChecker.Models;

public class BlockDbIp
{
	[JsonProperty("value")]
	public string Value { get; set; }
}
