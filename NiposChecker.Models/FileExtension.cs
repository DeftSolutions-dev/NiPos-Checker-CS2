using Newtonsoft.Json;

namespace NiposChecker.Models;

public class FileExtension
{
	[JsonProperty("extension")]
	public string Extension { get; set; }
}
