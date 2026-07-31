using System;

namespace NiposChecker.Services;

public static class CheatScoring
{
	public const int W_Hash = 100;

	public const int W_Signature = 60;

	public const int W_Rule = 45;

	public const int W_Icon = 25;

	public const int W_ExtOnly = 5;

	public const int W_ContextTemp = 10;

	public const int W_Timestomp = 12;

	private const int NameBase = 35;

	public static string Bucket(int score)
	{
		if (score >= 60)
		{
			return "red";
		}
		if (score >= 25)
		{
			return "amber";
		}
		return "mint";
	}

	public static string FromServer(string serverSeverity)
	{
		if (string.IsNullOrWhiteSpace(serverSeverity))
		{
			return null;
		}
		switch (serverSeverity.Trim().ToLowerInvariant())
		{
		case "crit":
		case "high":
		case "red":
		case "critical":
			return "red";
		case "mid":
		case "amber":
		case "medium":
		case "suspicious":
			return "amber";
		case "mint":
		case "safe":
		case "low":
		case "green":
			return "mint";
		default:
			return null;
		}
	}

	public static int NameMatchScore(string fileName, string keyword, out string kind)
	{
		kind = null;
		if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(keyword))
		{
			return 0;
		}
		string text = fileName.ToLowerInvariant();
		string text2 = keyword.ToLowerInvariant();
		int num = text.LastIndexOf('.');
		if (((num > 0) ? text.Substring(0, num) : text) == text2)
		{
			kind = "exact";
			return (int)Math.Round(35.0 * LengthFactor(text2.Length));
		}
		double num2 = 0.0;
		string text3 = null;
		for (int num3 = text.IndexOf(text2, StringComparison.Ordinal); num3 >= 0; num3 = text.IndexOf(text2, num3 + 1, StringComparison.Ordinal))
		{
			bool flag = num3 == 0 || !IsWordChar(text[num3 - 1]);
			int num4 = num3 + text2.Length;
			bool flag2 = num4 >= text.Length || !IsWordChar(text[num4]);
			double num5;
			string text4;
			if (flag && flag2)
			{
				num5 = 0.95;
				text4 = "token";
			}
			else if (flag && !flag2)
			{
				num5 = 0.85;
				text4 = "prefix";
			}
			else if (!flag && flag2)
			{
				num5 = 0.35;
				text4 = "suffix";
			}
			else
			{
				num5 = 0.2;
				text4 = "infix";
			}
			if (num5 > num2)
			{
				num2 = num5;
				text3 = text4;
			}
		}
		if (num2 <= 0.0)
		{
			return 0;
		}
		kind = text3;
		return (int)Math.Round(35.0 * num2 * LengthFactor(text2.Length));
	}

	private static double LengthFactor(int len)
	{
		if (len <= 2)
		{
			return 0.3;
		}
		return len switch
		{
			3 => 0.55, 
			4 => 0.85, 
			5 => 0.9, 
			_ => 1.0, 
		};
	}

	private static bool IsWordChar(char c)
	{
		return char.IsLetterOrDigit(c);
	}
}
