using System.Windows;

namespace NiposChecker.Services;

public class AutoDetectResult
{
	public string Title { get; set; }

	public string Text { get; set; }

	public int Id { get; set; }

	public string ActionLabel { get; set; }

	public Visibility ActionVisibility
	{
		get
		{
			if (!string.IsNullOrEmpty(ActionLabel))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}
}
