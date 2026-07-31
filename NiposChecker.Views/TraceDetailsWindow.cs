using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Microsoft.Win32;
using NiposChecker.Localization;
using NiposChecker.Models;

namespace NiposChecker.Views;

public partial class TraceDetailsWindow : Window
{
	private const int WM_NCHITTEST = 132;

	private const double ResizeBorder = 6.0;











	public TraceDetailsWindow(string title, List<MissingRunItem> items, string[] cols = null, string[] sortPaths = null)
	{
		InitializeComponent();
		HeaderTitle.Text = title ?? "";
		ItemsGrid.ItemsSource = items;
		HintText.Text = Strings.Get("Traces_DetailsHint");
		CloseText.Text = Strings.Get("BlockDb_CloseBtn");
		SetColumn(ColName, (cols != null && cols.Length != 0) ? cols[0] : Strings.Get("Col_Application"));
		SetColumn(ColPath, (cols != null && cols.Length > 1) ? cols[1] : Strings.Get("Col_Path"));
		SetColumn(ColRun, (cols != null && cols.Length > 2) ? cols[2] : Strings.Get("Traces_ColLastRun"));
		SetColumn(ColExtra1, (cols != null && cols.Length > 3) ? cols[3] : null);
		SetColumn(ColExtra2, (cols != null && cols.Length > 4) ? cols[4] : null);
		SetSort(ColName, sortPaths, 0);
		SetSort(ColPath, sortPaths, 1);
		SetSort(ColRun, sortPaths, 2);
		SetSort(ColExtra1, sortPaths, 3);
		SetSort(ColExtra2, sortPaths, 4);
	}

	private static void SetSort(DataGridColumn c, string[] paths, int i)
	{
		if (paths != null && i < paths.Length && !string.IsNullOrEmpty(paths[i]))
		{
			c.SortMemberPath = paths[i];
		}
	}

	private static void SetColumn(DataGridColumn c, string header)
	{
		if (string.IsNullOrEmpty(header))
		{
			c.Visibility = Visibility.Collapsed;
			c.Width = 0.0;
		}
		else
		{
			c.Header = header;
		}
	}

	private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (!(ItemsGrid.SelectedItem is MissingRunItem missingRunItem))
		{
			return;
		}
		try
		{
			string path = missingRunItem.Path;
			string text = ((!string.IsNullOrEmpty(path) && path.StartsWith("\\")) ? ("C:" + path) : path);
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				Process.Start("explorer.exe", "/select,\"" + text + "\"");
				return;
			}
			string text2 = null;
			try
			{
				if (!string.IsNullOrEmpty(text))
				{
					text2 = Path.GetDirectoryName(text);
				}
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(text2) && Directory.Exists(text2))
			{
				Process.Start("explorer.exe", "\"" + text2 + "\"");
			}
			else if (!string.IsNullOrEmpty(missingRunItem.RegKey))
			{
				OpenRegistry(missingRunItem.RegKey);
			}
		}
		catch
		{
		}
	}

	private static void OpenRegistry(string fullKey)
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName("regedit");
			foreach (Process process in processesByName)
			{
				try
				{
					process.Kill();
					process.WaitForExit(2000);
				}
				catch
				{
				}
			}
			string text = "Computer\\";
			try
			{
				if (Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Applets\\Regedit", "LastKey", null) is string text2 && !string.IsNullOrEmpty(text2))
				{
					int num = text2.IndexOf("HKEY_", StringComparison.OrdinalIgnoreCase);
					text = ((num > 0) ? text2.Substring(0, num) : (text2.TrimEnd('\\') + "\\"));
				}
			}
			catch
			{
			}
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Applets\\Regedit", "LastKey", text + fullKey);
			Process.Start(new ProcessStartInfo("regedit.exe")
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
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

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		(PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg != 132 || base.ResizeMode != ResizeMode.CanResize)
		{
			return IntPtr.Zero;
		}
		long num = ((IntPtr)lParam).ToInt64();
		int num2 = (short)(num & 0xFFFF);
		int num3 = (short)((num >> 16) & 0xFFFF);
		Point point;
		try
		{
			point = PointFromScreen(new Point(num2, num3));
		}
		catch
		{
			return IntPtr.Zero;
		}
		bool flag = point.X <= 6.0;
		bool flag2 = point.X >= base.ActualWidth - 6.0;
		bool flag3 = point.Y <= 6.0;
		bool flag4 = point.Y >= base.ActualHeight - 6.0;
		int num4 = ((flag3 && flag) ? 13 : ((flag3 && flag2) ? 14 : ((flag4 && flag) ? 16 : ((flag4 && flag2) ? 17 : (flag ? 10 : (flag2 ? 11 : (flag3 ? 12 : (flag4 ? 15 : 0))))))));
		if (num4 == 0)
		{
			return IntPtr.Zero;
		}
		handled = true;
		return new IntPtr(num4);
	}

}
