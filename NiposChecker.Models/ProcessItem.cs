using System.Windows.Media;

namespace NiposChecker.Models;

public class ProcessItem
{
	public string Name { get; set; }

	public string Pid { get; set; }

	public string Path { get; set; }

	public string Note { get; set; }

	public string Level { get; set; } = "warn";

	public int Rank
	{
		get
		{
			string level = Level;
			if (!(level == "alert"))
			{
				if (level == "warn")
				{
					return 1;
				}
				return 2;
			}
			return 0;
		}
	}

	public string TagText
	{
		get
		{
			string level = Level;
			if (!(level == "alert"))
			{
				if (level == "warn")
				{
					return "ВНИМАНИЕ";
				}
				return "ИНФО";
			}
			return "ЧИТ";
		}
	}

	public Brush RowBar
	{
		get
		{
			string level = Level;
			if (!(level == "alert"))
			{
				if (level == "warn")
				{
					return Rgb(240, 169, 60);
				}
				return Rgb(36, 30, 36);
			}
			return Rgb(byte.MaxValue, 40, 63);
		}
	}

	public Brush TagFill
	{
		get
		{
			string level = Level;
			if (!(level == "alert"))
			{
				if (level == "warn")
				{
					return Argb(36, 240, 169, 60);
				}
				return Rgb(28, 24, 28);
			}
			return Argb(38, byte.MaxValue, 40, 63);
		}
	}

	public Brush TagBrush
	{
		get
		{
			string level = Level;
			if (!(level == "alert"))
			{
				if (level == "warn")
				{
					return Rgb(245, 190, 107);
				}
				return Rgb(154, 142, 150);
			}
			return Rgb(byte.MaxValue, 128, 134);
		}
	}

	private static SolidColorBrush Rgb(byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromRgb(r, g, b));
	}

	private static SolidColorBrush Argb(byte a, byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromArgb(a, r, g, b));
	}
}
