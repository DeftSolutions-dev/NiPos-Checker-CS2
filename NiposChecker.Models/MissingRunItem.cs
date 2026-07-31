using System;

namespace NiposChecker.Models;

public class MissingRunItem
{
	public string Name { get; set; }

	public string Path { get; set; }

	public string LastRun { get; set; }

	public string Extra1 { get; set; }

	public string Extra2 { get; set; }

	public string RegKey { get; set; }

	public DateTime LastRunSort { get; set; } = DateTime.MinValue;

	public long Extra1Sort { get; set; }

	public long Extra2Sort { get; set; }
}
