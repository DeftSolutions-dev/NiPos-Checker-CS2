using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NiposChecker.Views;

public partial class AppDialog : Window
{










	public AppDialog()
	{
		InitializeComponent();
	}

	private static string IconGeometry(DialogKind kind)
	{
		return kind switch
		{
			DialogKind.Success => "M20 6 L9 17 L4 12", 
			DialogKind.Info => "M12 21 A9 9 0 1 1 12 3 A9 9 0 1 1 12 21 Z M12 8 h.01 M12 12 v4", 
			_ => "M12 3 L22 20 L2 20 Z M12 10 v4 M12 17 h.01", 
		};
	}

	private static (Color stroke, Color fill, Color border) IconColors(DialogKind kind)
	{
		return kind switch
		{
			DialogKind.Success => (stroke: Color.FromRgb(52, 211, 153), fill: Color.FromArgb(23, 52, 211, 153), border: Color.FromArgb(89, 52, 211, 153)), 
			DialogKind.Warning => (stroke: Color.FromRgb(240, 169, 60), fill: Color.FromArgb(23, 240, 169, 60), border: Color.FromArgb(89, 240, 169, 60)), 
			DialogKind.Info => (stroke: Color.FromRgb(byte.MaxValue, 40, 63), fill: Color.FromArgb(23, byte.MaxValue, 40, 63), border: Color.FromArgb(89, byte.MaxValue, 40, 63)), 
			_ => (stroke: Color.FromRgb(byte.MaxValue, 40, 63), fill: Color.FromArgb(23, byte.MaxValue, 40, 63), border: Color.FromArgb(89, byte.MaxValue, 40, 63)), 
		};
	}

	private void Configure(string title, string heading, string message, string yesText, string noText, DialogKind kind, bool dangerPrimary)
	{
		HeaderTitle.Text = title ?? "";
		Heading.Text = heading ?? "";
		if (!string.IsNullOrEmpty(message))
		{
			Message.Text = message;
			Message.Visibility = Visibility.Visible;
		}
		var (color, color2, color3) = IconColors(kind);
		IconPath.Data = Geometry.Parse(IconGeometry(kind));
		IconPath.Stroke = new SolidColorBrush(color);
		IconBorder.Background = new SolidColorBrush(color2);
		IconBorder.BorderBrush = new SolidColorBrush(color3);
		YesText.Text = yesText ?? "OK";
		if (dangerPrimary && FindResource("DangerBtn") is Style style)
		{
			YesBtn.Style = style;
		}
		if (noText == null)
		{
			NoBtn.Visibility = Visibility.Collapsed;
		}
		else
		{
			NoText.Text = noText;
		}
	}

	private void Yes_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
		Close();
	}

	private void No_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed)
		{
			try
			{
				DragMove();
			}
			catch
			{
			}
		}
	}

	public static bool Confirm(Window owner, string title, string heading, string message = null, string yesText = "Да", string noText = "Нет", DialogKind kind = DialogKind.Warning, bool dangerPrimary = false)
	{
		AppDialog appDialog = new AppDialog
		{
			Owner = ResolveOwner(owner)
		};
		if (appDialog.Owner == null)
		{
			appDialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
		appDialog.Configure(title, heading, message, yesText, noText, kind, dangerPrimary);
		return appDialog.ShowDialog() == true;
	}

	public static void Alert(Window owner, string title, string heading, string message = null, DialogKind kind = DialogKind.Info, string okText = "OK")
	{
		AppDialog appDialog = new AppDialog
		{
			Owner = ResolveOwner(owner)
		};
		if (appDialog.Owner == null)
		{
			appDialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
		appDialog.Configure(title, heading, message, okText, null, kind, dangerPrimary: false);
		appDialog.ShowDialog();
	}

	private static Window ResolveOwner(Window owner)
	{
		if (owner != null && owner.IsLoaded)
		{
			return owner;
		}
		WindowCollection windowCollection = Application.Current?.Windows;
		if (windowCollection != null)
		{
			foreach (Window item in windowCollection)
			{
				if (item.IsActive)
				{
					return item;
				}
			}
		}
		Window window2 = Application.Current?.MainWindow;
		if (window2 == null || !window2.IsLoaded)
		{
			return null;
		}
		return window2;
	}

}
