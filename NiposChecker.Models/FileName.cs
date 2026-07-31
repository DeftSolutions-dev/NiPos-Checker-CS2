using Newtonsoft.Json;

namespace NiposChecker.Models;

public class FileName
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("severity")]
	public string Severity { get; set; }
}
