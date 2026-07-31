using System.Collections.Generic;
using Newtonsoft.Json;

namespace NiposChecker.Models;

public class DatabaseData
{
	[JsonProperty("files_names")]
	public List<FileName> FilesNames { get; set; } = new List<FileName>();

	[JsonProperty("files_extensions")]
	public List<FileExtension> FilesExtensions { get; set; } = new List<FileExtension>();

	[JsonProperty("files_hash")]
	public List<FileHash> FilesHashes { get; set; } = new List<FileHash>();

	[JsonProperty("files_icons")]
	public List<FileIcon> FilesIcons { get; set; } = new List<FileIcon>();

	[JsonProperty("cheat_rules")]
	public List<CheatRuleModel> CheatRules { get; set; } = new List<CheatRuleModel>();
}
