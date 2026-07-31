using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using NiposChecker.Models;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class ReportHistoryWindow : Window
{
	private readonly ApiClient _api;

	private readonly string _steamId;

	private readonly string _nick;

	private const int WM_NCHITTEST = 132;

	private const double ResizeBorder = 6.0;







	public ReportHistoryWindow(ApiClient api, string steamId, string nick = null)
	{
		InitializeComponent();
		_api = api;
		_steamId = steamId;
		_nick = nick;
		SubTitle.Text = (string.IsNullOrEmpty(nick) ? ("SteamID " + steamId) : (nick + " · " + steamId));
		base.Loaded += async delegate
		{
			await LoadAsync();
		};
	}

	private async Task LoadAsync()
	{
		TableBox.Visibility = Visibility.Collapsed;
		OverlayText.Visibility = Visibility.Visible;
		OverlayText.Text = "Загрузка истории…";
		try
		{
			List<ReportHistoryItem> list = await _api.GetReportHistoryAsync(_steamId);
			if (list == null || list.Count == 0)
			{
				OverlayText.Text = "Прошлых проверок для этого аккаунта нет.";
				return;
			}
			ItemsGrid.ItemsSource = list;
			OverlayText.Visibility = Visibility.Collapsed;
			TableBox.Visibility = Visibility.Visible;
		}
		catch
		{
			OverlayText.Text = "Не удалось загрузить историю.";
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
