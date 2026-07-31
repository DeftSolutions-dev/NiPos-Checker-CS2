using System.Windows.Media;

namespace NiposChecker.Models;

public class ActivityItem
{
	public string Name { get; set; }

	public string Path { get; set; }

	public string Date { get; set; }

	public string Extension { get; set; }

	public ImageSource Icon { get; set; }

	public bool IsSuspicious { get; set; }

	public string Level { get; set; } = "";

	public string OnDisk { get; set; } = "";

	public Brush RowBar
	{
		get
		{
			string level = Level;
			if (!(level == "red"))
			{
				if (level == "amber")
				{
					return new SolidColorBrush(Color.FromRgb(240, 169, 60));
				}
				return Brushes.Transparent;
			}
			return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 40, 63));
		}
	}
}
