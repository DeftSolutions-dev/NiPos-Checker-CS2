using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NiposChecker.Services;

public class CheatRuleEngine
{
	private List<CheatRule> _rules = new List<CheatRule>();

	public bool HasRules => _rules.Count > 0;

	public void Load(IEnumerable<string> ruleTexts)
	{
		_rules = (from r in ruleTexts
			where !string.IsNullOrWhiteSpace(r)
			select new CheatRule
			{
				RuleText = r.Trim(),
				CheatName = "Detected"
			}).ToList();
	}

	public void Load(IEnumerable<(string Rule, string CheatName)> rules)
	{
		_rules = (from r in rules
			where !string.IsNullOrWhiteSpace(r.Rule)
			select new CheatRule
			{
				RuleText = r.Rule.Trim(),
				CheatName = (r.CheatName ?? "Detected")
			}).ToList();
	}

	public string Match(FileInfo file)
	{
		foreach (CheatRule rule in _rules)
		{
			try
			{
				if (rule.IsMatch(file))
				{
					return rule.CheatName;
				}
			}
			catch
			{
			}
		}
		return null;
	}
}
