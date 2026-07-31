using System.Collections.Generic;

namespace NiposChecker.Services;

public class DetectionResult
{
	public string CheatName { get; set; } = "Detected";

	public int Score { get; set; }

	public string Severity { get; set; } = "mint";

	public List<string> Signals { get; } = new List<string>();

	public void Add(string signal, int weight)
	{
		Signals.Add(signal);
		Score += weight;
	}
}
