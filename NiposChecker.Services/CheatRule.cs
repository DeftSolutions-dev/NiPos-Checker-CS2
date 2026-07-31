using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace NiposChecker.Services;

public class CheatRule
{
	private List<Func<FileInfo, bool>> _conditions;

	public string RuleText { get; set; }

	public string CheatName { get; set; }

	public bool IsEmpty
	{
		get
		{
			if (_conditions != null)
			{
				return _conditions.Count == 0;
			}
			return true;
		}
	}

	public bool IsMatch(FileInfo file)
	{
		if (_conditions == null)
		{
			_conditions = Parse(RuleText);
		}
		if (_conditions.Count == 0)
		{
			return file.Name.IndexOf(RuleText, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		foreach (Func<FileInfo, bool> condition in _conditions)
		{
			if (!condition(file))
			{
				return false;
			}
		}
		return true;
	}

	private static List<Func<FileInfo, bool>> Parse(string rule)
	{
		if (string.IsNullOrWhiteSpace(rule))
		{
			return new List<Func<FileInfo, bool>>();
		}
		List<Func<FileInfo, bool>> list = new List<Func<FileInfo, bool>>();
		int i = 0;
		while (i < rule.Length)
		{
			for (; i < rule.Length && rule[i] == ' '; i++)
			{
			}
			if (i >= rule.Length)
			{
				break;
			}
			if (StartsWithAt(rule, i, "ext:", out var value))
			{
				string ext = value.Trim();
				if (!string.IsNullOrEmpty(ext))
				{
					if (ext[0] != '.')
					{
						ext = "." + ext;
					}
					list.Add((FileInfo f) => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
				}
				i += 4 + value.Length;
				continue;
			}
			if (StartsWithAt(rule, i, "size:", out var value2))
			{
				string range = value2.Trim().ToLowerInvariant();
				list.Add(ParseSizeCondition(range));
				i += 5 + value2.Length;
				continue;
			}
			if (StartsWithAt(rule, i, "utf8content:", out var value3))
			{
				string str = ExtractQuotedString(value3);
				if (!string.IsNullOrEmpty(str))
				{
					list.Add((FileInfo f) => FileContainsUtf8(f.FullName, str));
				}
				i += 12 + (str?.Length ?? 0) + 2;
				continue;
			}
			if (StartsWithAt(rule, i, "utf16content:", out var value4))
			{
				string str2 = ExtractQuotedString(value4);
				if (!string.IsNullOrEmpty(str2))
				{
					list.Add((FileInfo f) => FileContainsUtf16(f.FullName, str2));
				}
				i += 13 + (str2?.Length ?? 0) + 2;
				continue;
			}
			if (StartsWithAt(rule, i, "!sig:", out var _))
			{
				list.Add((FileInfo f) => !HasDigitalSignature(f.FullName));
				i += 5;
				continue;
			}
			if (!StartsWithAt(rule, i, "whole:", out var value6))
			{
				break;
			}
			string word = value6.Trim();
			if (!string.IsNullOrEmpty(word))
			{
				list.Add((FileInfo f) => f.Name.Equals(word, StringComparison.OrdinalIgnoreCase));
			}
			i += 6 + word.Length;
		}
		return list;
	}

	private static bool StartsWithAt(string s, int pos, string prefix, out string value)
	{
		value = null;
		if (pos + prefix.Length > s.Length)
		{
			return false;
		}
		for (int i = 0; i < prefix.Length; i++)
		{
			if (char.ToLowerInvariant(s[pos + i]) != char.ToLowerInvariant(prefix[i]))
			{
				return false;
			}
		}
		int num = pos + prefix.Length;
		if (num >= s.Length)
		{
			value = "";
			return true;
		}
		if (s[num] == '"')
		{
			int num2 = s.IndexOf('"', num + 1);
			if (num2 < 0)
			{
				num2 = s.Length;
			}
			value = s.Substring(num + 1, num2 - num - 1);
		}
		else
		{
			int num3 = s.IndexOf(' ', num);
			if (num3 < 0)
			{
				num3 = s.Length;
			}
			value = s.Substring(num, num3 - num);
		}
		return true;
	}

	private static string ExtractQuotedString(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		s = s.Trim();
		if (s.Length >= 2 && s[0] == '"')
		{
			int num = s.IndexOf('"', 1);
			if (num > 0)
			{
				return s.Substring(1, num - 1);
			}
		}
		return s;
	}

	private static Func<FileInfo, bool> ParseSizeCondition(string range)
	{
		int num = range.IndexOf('-');
		if (num > 0)
		{
			string s = range.Substring(0, num).Trim();
			string s2 = range.Substring(num + 1).Trim();
			long min = ParseSize(s);
			long max = ParseSize(s2);
			return (FileInfo f) => f.Length >= min && f.Length <= max;
		}
		long exact = ParseSize(range);
		return (FileInfo f) => f.Length == exact;
	}

	private static long ParseSize(string s)
	{
		s = s.Trim().ToLowerInvariant();
		double num = 1.0;
		Match match = Regex.Match(s, "^([\\d.]+)");
		if (!match.Success)
		{
			return 0L;
		}
		num = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
		if (s.EndsWith("gb"))
		{
			return (long)(num * 1024.0 * 1024.0 * 1024.0);
		}
		if (s.EndsWith("mb"))
		{
			return (long)(num * 1024.0 * 1024.0);
		}
		if (s.EndsWith("kb"))
		{
			return (long)(num * 1024.0);
		}
		return (long)num;
	}

	private static bool FileContainsUtf8(string path, string search)
	{
		if (string.IsNullOrEmpty(search) || search.Length < 3)
		{
			return false;
		}
		try
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			long num = Math.Min(fileStream.Length, 33554432L);
			byte[] array = new byte[num];
			ReadFull(fileStream, array, (int)num);
			return Encoding.UTF8.GetString(array).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool FileContainsUtf16(string path, string search)
	{
		if (string.IsNullOrEmpty(search) || search.Length < 3)
		{
			return false;
		}
		try
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			long num = Math.Min(fileStream.Length, 33554432L);
			byte[] array = new byte[num];
			ReadFull(fileStream, array, (int)num);
			return Encoding.Unicode.GetString(array).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static void ReadFull(Stream fs, byte[] buf, int count)
	{
		int num;
		for (int i = 0; i < count; i += num)
		{
			num = fs.Read(buf, i, count - i);
			if (num <= 0)
			{
				break;
			}
		}
	}

	private static bool HasDigitalSignature(string path)
	{
		try
		{
			using X509Certificate2 x509Certificate = new X509Certificate2(path);
			return x509Certificate != null && !string.IsNullOrEmpty(x509Certificate.Subject);
		}
		catch
		{
			return false;
		}
	}
}
