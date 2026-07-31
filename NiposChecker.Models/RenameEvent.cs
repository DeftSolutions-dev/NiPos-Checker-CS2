using System;
using System.Windows.Media;

namespace NiposChecker.Models;

public class RenameEvent
{
	public string OldName { get; set; }

	public string NewName { get; set; }

	public string When { get; set; }

	public string CurrentPath { get; set; }

	public string OnDisk { get; set; }

	public DateTime WhenRaw { get; set; }

	public ImageSource Icon { get; set; }
}
