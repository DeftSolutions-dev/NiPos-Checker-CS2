using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NiposChecker.Models;

public class ReportHistoryItem
{
	[JsonProperty("date")]
	public string Date { get; set; }

	[JsonProperty("project")]
	public string Project { get; set; }

	[JsonProperty("files")]
	public int Files { get; set; }

	[JsonProperty("proc")]
	public int Proc { get; set; }

	[JsonProperty("traces")]
	public int Traces { get; set; }

	[JsonProperty("banned")]
	public int Banned { get; set; }

	[JsonProperty("url")]
	public string Url { get; set; }

	public string DateDisplay
	{
		get
		{
			if (DateTime.TryParse(Date, out var result))
			{
				return result.ToString("dd.MM.yyyy HH:mm");
			}
			return Date ?? "—";
		}
	}

	public string Verdict
	{
		get
		{
			if (Files + Proc + Banned <= 0)
			{
				if (Traces <= 0)
				{
					return "ЧИСТО";
				}
				return "ПОДОЗРЕНИЯ";
			}
			return "НАЙДЕНЫ ЧИТЫ";
		}
	}

	public string Findings
	{
		get
		{
			List<string> list = new List<string>();
			if (Files > 0)
			{
				list.Add($"файлы: {Files}");
			}
			if (Proc > 0)
			{
				list.Add($"читы: {Proc}");
			}
			if (Traces > 0)
			{
				list.Add($"следы: {Traces}");
			}
			if (Banned > 0)
			{
				list.Add($"баны: {Banned}");
			}
			if (list.Count <= 0)
			{
				return "чисто";
			}
			return string.Join(" · ", list);
		}
	}
}
