using Newtonsoft.Json;

namespace NiposChecker.Models;

public class CheatRuleModel
{
	[JsonProperty("rule")]
	public string Rule { get; set; }

	[JsonProperty("cheat_name")]
	public string CheatName { get; set; }

	[JsonProperty("severity")]
	public string Severity { get; set; }
}
