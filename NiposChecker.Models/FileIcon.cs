using Newtonsoft.Json;

namespace NiposChecker.Models;

public class FileIcon
{
	[JsonProperty("icon")]
	public string Icon { get; set; }
}
