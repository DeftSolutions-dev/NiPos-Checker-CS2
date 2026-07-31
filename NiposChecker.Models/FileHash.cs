using Newtonsoft.Json;

namespace NiposChecker.Models;

public class FileHash
{
	[JsonProperty("File_Sha256")]
	public string FileSha256 { get; set; }

	[JsonProperty("File_SignatureName")]
	public string FileSignatureName { get; set; }

	[JsonProperty("CheatName")]
	public string CheatName { get; set; }

	[JsonProperty("severity")]
	public string Severity { get; set; }
}
