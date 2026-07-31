using System.Windows.Media;

namespace NiposChecker.Models;

public class FoundedFile
{
	public string Name { get; set; }

	public string CheatName { get; set; }

	public string Type { get; set; }

	public string Path { get; set; }

	public string Weight { get; set; }

	public string LastChange { get; set; }

	public string LastAccess { get; set; }

	public string Source { get; set; }

	public ImageSource FileIcon { get; set; }

	public bool IsDetected { get; set; }

	public int Score { get; set; }

	public string Severity { get; set; } = "mint";

	public string MatchedSignals { get; set; } = "";

	public string SeverityLabel
	{
		get
		{
			string severity = Severity;
			if (!(severity == "red"))
			{
				if (severity == "amber")
				{
					return "Средний";
				}
				return "Низкий";
			}
			return "Критический";
		}
	}

	public int SeverityRank
	{
		get
		{
			string severity = Severity;
			if (!(severity == "red"))
			{
				if (severity == "amber")
				{
					return 1;
				}
				return 2;
			}
			return 0;
		}
	}

	public Brush SeverityBrush
	{
		get
		{
			string severity = Severity;
			if (!(severity == "red"))
			{
				if (severity == "amber")
				{
					return new SolidColorBrush(Color.FromRgb(245, 190, 107));
				}
				return new SolidColorBrush(Color.FromRgb(95, 224, 180));
			}
			return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 128, 134));
		}
	}

	public Brush SeverityFill
	{
		get
		{
			string severity = Severity;
			if (!(severity == "red"))
			{
				if (severity == "amber")
				{
					return new SolidColorBrush(Color.FromArgb(36, 240, 169, 60));
				}
				return new SolidColorBrush(Color.FromArgb(31, 52, 211, 153));
			}
			return new SolidColorBrush(Color.FromArgb(38, byte.MaxValue, 40, 63));
		}
	}

	public Brush SeverityBar
	{
		get
		{
			string severity = Severity;
			if (!(severity == "red"))
			{
				if (severity == "amber")
				{
					return new SolidColorBrush(Color.FromRgb(240, 169, 60));
				}
				return new SolidColorBrush(Color.FromArgb(128, 52, 211, 153));
			}
			return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 40, 63));
		}
	}
}
