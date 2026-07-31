using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NiposChecker.Models;

public class TraceSignal
{
	public string Title { get; set; }

	public string Detail { get; set; }

	public string Level { get; set; } = "ok";

	public List<MissingRunItem> Items { get; set; }

	public string[] DetailCols { get; set; }

	public string[] SortPaths { get; set; }

	public bool HasDetails
	{
		get
		{
			if (Items != null)
			{
				return Items.Count > 0;
			}
			return false;
		}
	}

	public Visibility DetailsVisibility
	{
		get
		{
			if (!HasDetails)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public string RepairKind { get; set; }

	public Visibility RepairVisibility
	{
		get
		{
			if (!string.IsNullOrEmpty(RepairKind))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public int Rank => Level switch
	{
		"alert" => 0, 
		"warn" => 1, 
		"info" => 2, 
		"ok" => 3, 
		_ => 4, 
	};

	public string TagText => Level switch
	{
		"alert" => "ТРЕВОГА", 
		"warn" => "ВНИМАНИЕ", 
		"info" => "ИНФО", 
		"ok" => "ОК", 
		_ => "ИНФО", 
	};

	public Brush BarBrush => Level switch
	{
		"alert" => Rgb(byte.MaxValue, 40, 63), 
		"warn" => Rgb(240, 169, 60), 
		"info" => Rgb(55, 43, 55), 
		"ok" => Rgb(52, 211, 153), 
		_ => Rgb(36, 30, 36), 
	};

	public Brush TagFill => Level switch
	{
		"alert" => Argb(38, byte.MaxValue, 40, 63), 
		"warn" => Argb(36, 240, 169, 60), 
		"ok" => Argb(31, 52, 211, 153), 
		_ => Rgb(28, 24, 28), 
	};

	public Brush TagBrush => Level switch
	{
		"alert" => Rgb(byte.MaxValue, 128, 134), 
		"warn" => Rgb(245, 190, 107), 
		"ok" => Rgb(95, 224, 180), 
		_ => Rgb(154, 142, 150), 
	};

	private static SolidColorBrush Rgb(byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromRgb(r, g, b));
	}

	private static SolidColorBrush Argb(byte a, byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromArgb(a, r, g, b));
	}
}
