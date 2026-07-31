using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using NiposChecker.Localization;
using NiposChecker.Models;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class MainWindow : Window
{
	private struct POINTL
	{
		public int X;

		public int Y;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MINMAXINFO
	{
		public POINTL ptReserved;

		public POINTL ptMaxSize;

		public POINTL ptMaxPosition;

		public POINTL ptMinTrackSize;

		public POINTL ptMaxTrackSize;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public int dwFlags;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SHFILEINFO
	{
		public nint hIcon;

		public int iIcon;

		public uint dwAttributes;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}

	private readonly ProjectInfo _projectInfo;

	private readonly List<SteamAccount> _accounts;

	private readonly ApiClient _api;

	private readonly CheatDatabase _cheatDb;

	private FileScanner _scanner;

	private Timer _searchTimer;

	private Stopwatch _searchStopwatch;

	private List<string> _searchDirectories;

	private bool _useIconMatch;

	private bool _useSignatureMatch;

	private List<FoundedFile> _lastSearchResults;

	private readonly string _sessionId = Guid.NewGuid().ToString("N");

	private List<ActivityItem> _allActivity = new List<ActivityItem>();

	private ICollectionView _activityView;

	private string _currentTab = "Search";

	private double _zoom = 1.0;

	private const int WM_NCHITTEST = 132;

	private const int WM_GETMINMAXINFO = 36;

	private const int MONITOR_DEFAULTTONEAREST = 2;

	private const double ResizeBorder = 6.0;

	private Storyboard _lampPulse;

	private bool _tracesRunning;

	private bool _procRunning;

	private bool _reportSending;

	private bool _syncingFilters;

	private bool _renamesLoading;

	private ICollectionView _renamesView;














































































































































































































	public MainWindow(ProjectInfo projectInfo, List<SteamAccount> accounts, ApiClient api, CheatDatabase cheatDb, ImageSource preloadedLogo = null)
	{
		InitializeComponent();
		_projectInfo = projectInfo;
		base.Title = (string.IsNullOrWhiteSpace(_projectInfo?.CheckerName) ? base.Title : _projectInfo.CheckerName);
		_accounts = accounts;
		_api = api;
		_cheatDb = cheatDb;
		if (preloadedLogo != null)
		{
			ProjectLogo.Source = preloadedLogo;
			ProjectInfo projectInfo2 = _projectInfo;
			if (projectInfo2 != null && projectInfo2.LogoWidth > 0)
			{
				ProjectLogo.Width = _projectInfo.LogoWidth;
			}
			ProjectInfo projectInfo3 = _projectInfo;
			if (projectInfo3 != null && projectInfo3.LogoHeight > 0)
			{
				ProjectLogo.Height = _projectInfo.LogoHeight;
			}
		}
		base.Loaded += delegate
		{
			Populate();
		};
		Strings.LanguageChanged += OnLanguageChanged;
		base.Loaded += delegate
		{
			UpdateLocalization();
		};
		base.Closed += delegate
		{
			Strings.LanguageChanged -= OnLanguageChanged;
		};
		base.Loaded += delegate
		{
			AdminReloadBtn.Visibility = (IsElevated() ? Visibility.Collapsed : Visibility.Visible);
		};
		StartSessionClock();
	}

	private void StartSessionClock()
	{
		DateTime start = DateTime.Now;
		DispatcherTimer t = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.0)
		};
		t.Tick += delegate
		{
			if (SessionClock != null)
			{
				SessionClock.Text = (DateTime.Now - start).ToString("hh\\:mm\\:ss");
			}
		};
		t.Start();
		base.Closed += delegate
		{
			t.Stop();
		};
	}

	private void OnLanguageChanged()
	{
		UpdateLocalization();
		PopulateSystemInfo();
		PopulateCurrentAccount();
	}

	private async void Populate()
	{
		_ = 2;
		try
		{
			FooterCid.Text = "CID: " + _projectInfo?.CheckId;
			FooterVersion.Text = (_projectInfo?.CheckerName ?? "NIPOS CHECKER") + " v.1.1";
			SoundIcon.Opacity = (SoundService.Enabled ? 0.7 : 0.3);
			await LoadLogoAsync();
			await LoadWindowIconAsync();
			PopulateCurrentAccount();
			List<SteamAccount> itemsSource = _accounts?.Where((SteamAccount a) => !a.IsCurrent).ToList() ?? new List<SteamAccount>();
			OtherAccountsList.ItemsSource = itemsSource;
			PopulateSystemInfo();
			await PopulateLinksAndBannerAsync();
			RunAutoDetectAsync();
			CheckProjectBansAsync();
			SendReportSilentAsync();
		}
		catch (Exception ex)
		{
			AppDialog.Alert(this, Strings.Get("Title_Error"), Strings.Get("Msg_InitError", ex.Message), null, DialogKind.Danger);
		}
	}

	private async Task CheckProjectBansAsync()
	{
		try
		{
			BlockDbService svc = new BlockDbService(_api);
			string project = _projectInfo?.ProjectName;
			bool any = false;
			foreach (SteamAccount acc in _accounts ?? new List<SteamAccount>())
			{
				try
				{
					if (BlockDbService.HasProjectBan(await svc.CheckSteamIdAsync(acc.SteamID64), project))
					{
						acc.isBannedOnProject = true;
						any = true;
					}
				}
				catch
				{
				}
			}
			if (any)
			{
				OtherAccountsList.Items.Refresh();
				PopulateCurrentAccount();
			}
		}
		catch
		{
		}
	}

	private void PopulateSystemInfo()
	{
		try
		{
			WindowsInfo windowsInfo = SystemInfo.Gather();
			SysWindows.Text = Strings.Get("Sys_Windows", windowsInfo.WindowsVersion);
			SysInstallDate.Text = Strings.Get("Sys_InstallDate", windowsInfo.WindowsInstallDate);
			SysUptime.Text = Strings.Get("Sys_Uptime", windowsInfo.WindowsStartupTime ?? "—");
			SysRam.Text = Strings.Get("Sys_Ram", windowsInfo.PcRAM);
			SysCpu.Text = Strings.Get("Sys_Cpu", windowsInfo.Processor);
			SysGpu.Text = Strings.Get("Sys_Gpu", windowsInfo.GPU);
			SysMotherboard.Text = Strings.Get("Sys_Motherboard", windowsInfo.Motherboard);
			SysScreens.Text = Strings.Get("Sys_Screens", windowsInfo.ScreensCount);
			SysVm.Text = (windowsInfo.DetectVM ? Strings.Get("Sys_VmYes", windowsInfo.VMName) : Strings.Get("Sys_VmNo"));
		}
		catch (Exception)
		{
		}
	}

	private async Task LoadLogoAsync()
	{
		if (ProjectLogo.Source != null)
		{
			return;
		}
		LogoSpinner.Visibility = Visibility.Visible;
		string[] array = new string[2]
		{
			_projectInfo?.MainLogoUrl,
			_projectInfo?.WhiteLogoUrl
		};
		foreach (string text in array)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			BitmapImage bitmapImage = await ImageLoader.DownloadAsync(text);
			if (bitmapImage != null)
			{
				LogoSpinner.Visibility = Visibility.Collapsed;
				ProjectLogo.Source = bitmapImage;
				if (_projectInfo.LogoWidth > 0)
				{
					ProjectLogo.Width = _projectInfo.LogoWidth;
				}
				if (_projectInfo.LogoHeight > 0)
				{
					ProjectLogo.Height = _projectInfo.LogoHeight;
				}
				break;
			}
		}
	}

	private async Task PopulateLinksAndBannerAsync()
	{
		try
		{
			SetLink(LinkWebsite, Strings.Get("Link_Website"), _projectInfo?.LinkToWebsite);
			SetLink(LinkDiscord, Strings.Get("Link_Discord"), _projectInfo?.LinkToDiscord);
			SetLink(LinkVK, Strings.Get("Link_VK"), _projectInfo?.LinkToVK);
			if (_projectInfo != null && _projectInfo.IsShowBanner1 && !string.IsNullOrEmpty(_projectInfo.Banner1Url))
			{
				BitmapImage bitmapImage = await ImageLoader.DownloadAsync(_projectInfo.Banner1Url);
				if (bitmapImage != null)
				{
					BannerImage.Source = bitmapImage;
					BannerTextTop.Text = _projectInfo.Banner1TextTop ?? "";
					BannerTextLink.Text = _projectInfo.Banner1TextLink ?? "";
					BannerPanel.Visibility = Visibility.Visible;
				}
			}
		}
		catch
		{
		}
	}

	private static void SetLink(TextBlock tb, string label, string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			tb.Visibility = Visibility.Collapsed;
			return;
		}
		tb.Text = label;
		tb.Tag = url;
		tb.Visibility = Visibility.Visible;
	}

	private static void OpenUrl(string url)
	{
		if (!Safety.IsSafeWebUrl(url))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void LinkWebsite_Click(object s, MouseButtonEventArgs e)
	{
		SoundService.Click();
		OpenUrl((s as FrameworkElement)?.Tag as string);
	}

	private void LinkDiscord_Click(object s, MouseButtonEventArgs e)
	{
		SoundService.Click();
		OpenUrl((s as FrameworkElement)?.Tag as string);
	}

	private void LinkVK_Click(object s, MouseButtonEventArgs e)
	{
		SoundService.Click();
		OpenUrl((s as FrameworkElement)?.Tag as string);
	}

	private void Banner_Click(object s, MouseButtonEventArgs e)
	{
		SoundService.Click();
		OpenUrl(_projectInfo?.Banner1LinkUrl);
	}

	private async Task LoadWindowIconAsync()
	{
		string text = _projectInfo?.SoftwareIcon;
		if (!string.IsNullOrEmpty(text))
		{
			BitmapImage bitmapImage = await ImageLoader.DownloadAsync(text);
			if (bitmapImage != null)
			{
				base.Icon = bitmapImage;
			}
		}
	}

	private async Task LoadFlagAsync()
	{
		string text = _projectInfo?.FlagURL;
		if (!string.IsNullOrEmpty(text))
		{
			BitmapImage bitmapImage = await ImageLoader.DownloadAsync(text);
			if (bitmapImage != null)
			{
				FlagImage.Source = bitmapImage;
				FlagImage.Visibility = Visibility.Visible;
			}
		}
	}

	private void PopulateCurrentAccount()
	{
		SteamAccount steamAccount = _accounts?.FirstOrDefault((SteamAccount a) => a.IsCurrent) ?? _accounts?.FirstOrDefault();
		if (steamAccount == null)
		{
			return;
		}
		CurrentNickname.Text = steamAccount.Nickname ?? "—";
		CurrentSteamId.Text = steamAccount.SteamID64 ?? "—";
		CurrentProjectBan.Text = (steamAccount.isBannedOnProject ? Strings.Get("Steam_ProjectBanBadge") : "");
		CurrentVac.Text = (steamAccount.Vac_Ban ? $"VAC: бан ({steamAccount.VAC_SinceBan} дн.)" : "VAC чист");
		CurrentProfileType.Text = "Профиль: " + (steamAccount.ProfileTypeFormatted ?? "—");
		string text = _projectInfo?.IP ?? "—";
		if (!string.IsNullOrEmpty(_projectInfo?.CountryCode))
		{
			text = text + " (" + _projectInfo.CountryCode + ")";
		}
		CurrentIp.Text = text;
		LoadFlagAsync();
		CurrentRealName.Text = ((string.IsNullOrEmpty(steamAccount.RealName) || steamAccount.RealName == "—") ? "—" : steamAccount.RealName);
		CurrentRegDate.Text = steamAccount.RegistrationDate ?? "—";
		CurrentCs2Date.Text = ((App.GameBuyDateUnix != 0) ? DateTimeOffset.FromUnixTimeSeconds(App.GameBuyDateUnix).LocalDateTime.ToString("dd.MM.yyyy") : "—");
		if (!string.IsNullOrEmpty(steamAccount.Avatar))
		{
			try
			{
				using MemoryStream streamSource = new MemoryStream(Convert.FromBase64String(steamAccount.Avatar));
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = streamSource;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				CurrentAvatar.Source = bitmapImage;
			}
			catch
			{
			}
		}
		UpdateSteamVerdict();
	}

	private void UpdateSteamVerdict()
	{
		if (SteamVerdict != null)
		{
			int num = _accounts?.Count((SteamAccount a) => a.isBannedOnProject) ?? 0;
			int value = _accounts?.Count ?? 0;
			if (num > 0)
			{
				string value2 = _projectInfo?.IP;
				SteamVerdictSub.Text = (string.IsNullOrEmpty(value2) ? $"Сверено с BlockDB · {DateTime.Now:dd.MM.yyyy HH:mm}" : $"Совпадение по IP {value2} · сверено с BlockDB {DateTime.Now:dd.MM.yyyy HH:mm}");
				SteamVerdictNum.Text = num.ToString();
				SteamVerdictCap.Text = $"ИЗ {value}";
				SteamVerdict.Visibility = Visibility.Visible;
			}
			else
			{
				SteamVerdict.Visibility = Visibility.Collapsed;
			}
		}
	}

	private void CopyCurrentSteamId_Click(object sender, MouseButtonEventArgs e)
	{
		SteamAccount steamAccount = _accounts?.FirstOrDefault((SteamAccount a) => a.IsCurrent) ?? _accounts?.FirstOrDefault();
		if (steamAccount != null)
		{
			try
			{
				Clipboard.SetText(steamAccount.SteamID64 ?? "");
			}
			catch
			{
			}
		}
	}

	private void MenuTab_Checked(object sender, RoutedEventArgs e)
	{
		if (!(e.OriginalSource is RadioButton radioButton) || Tab_Search == null)
		{
			return;
		}
		SoundService.Click();
		Grid tab_Search = Tab_Search;
		Grid tab_Accounts = Tab_Accounts;
		Grid tab_USB = Tab_USB;
		Grid tab_LastActivity = Tab_LastActivity;
		Grid tab_Software = Tab_Software;
		Grid tab_Registry = Tab_Registry;
		Grid tab_Traces = Tab_Traces;
		Grid tab_Processes = Tab_Processes;
		Grid tab_System = Tab_System;
		Visibility visibility = (Tab_Renames.Visibility = Visibility.Collapsed);
		Visibility visibility3 = (tab_System.Visibility = visibility);
		Visibility visibility5 = (tab_Processes.Visibility = visibility3);
		Visibility visibility7 = (tab_Traces.Visibility = visibility5);
		Visibility visibility9 = (tab_Registry.Visibility = visibility7);
		Visibility visibility11 = (tab_Software.Visibility = visibility9);
		Visibility visibility13 = (tab_LastActivity.Visibility = visibility11);
		Visibility visibility15 = (tab_USB.Visibility = visibility13);
		Visibility visibility17 = (tab_Accounts.Visibility = visibility15);
		tab_Search.Visibility = visibility17;
		_currentTab = radioButton.Tag?.ToString() ?? "Search";
		string text = radioButton.Tag?.ToString();
		if (text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 6:
			switch (text[1])
			{
			case 'e':
				if (text == "Search")
				{
					Tab_Search.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Search");
				}
				break;
			case 'r':
				if (text == "Traces")
				{
					Tab_Traces.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Traces");
				}
				break;
			case 'y':
				if (text == "System")
				{
					Tab_System.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Other");
				}
				break;
			}
			break;
		case 8:
			switch (text[0])
			{
			case 'A':
				if (text == "Accounts")
				{
					Tab_Accounts.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Steam");
				}
				break;
			case 'S':
				if (text == "Software")
				{
					Tab_Software.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Software");
				}
				break;
			case 'R':
				if (text == "Registry")
				{
					Tab_Registry.Visibility = Visibility.Visible;
					TabTitle.Text = Strings.Get("Tab_Registry");
				}
				break;
			}
			break;
		case 3:
			if (text == "USB")
			{
				Tab_USB.Visibility = Visibility.Visible;
				TabTitle.Text = Strings.Get("Tab_USB");
			}
			break;
		case 12:
			if (text == "LastActivity")
			{
				Tab_LastActivity.Visibility = Visibility.Visible;
				TabTitle.Text = Strings.Get("Tab_LastActivity");
			}
			break;
		case 9:
			if (text == "Processes")
			{
				Tab_Processes.Visibility = Visibility.Visible;
				TabTitle.Text = Strings.Get("Tab_Processes");
			}
			break;
		case 7:
			if (text == "Renames")
			{
				Tab_Renames.Visibility = Visibility.Visible;
				TabTitle.Text = "Переименованные файлы";
			}
			break;
		case 4:
		case 5:
		case 10:
		case 11:
			break;
		}
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

	private void SetZoom(double z)
	{
		_zoom = Math.Max(1.0, Math.Min(2.2, Math.Round(z, 2)));
		if (ZoomScale != null)
		{
			ZoomScale.ScaleX = _zoom;
			ZoomScale.ScaleY = _zoom;
		}
	}

	private void ZoomIn_Click(object sender, RoutedEventArgs e)
	{
		SetZoom(_zoom + 0.1);
	}

	private void ZoomOut_Click(object sender, RoutedEventArgs e)
	{
		SetZoom(_zoom - 0.1);
	}

	private void ZoomReset_Click(object sender, RoutedEventArgs e)
	{
		SetZoom(1.0);
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		base.OnPreviewKeyDown(e);
		if (Keyboard.Modifiers == ModifierKeys.Control)
		{
			if (e.Key == Key.OemPlus || e.Key == Key.Add)
			{
				SetZoom(_zoom + 0.1);
				e.Handled = true;
			}
			else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
			{
				SetZoom(_zoom - 0.1);
				e.Handled = true;
			}
			else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
			{
				SetZoom(1.0);
				e.Handled = true;
			}
		}
	}

	private void RootBorder_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (sender is Border border)
		{
			double num = ((base.WindowState != WindowState.Maximized) ? 14 : 0);
			border.Clip = new RectangleGeometry(new Rect(0.0, 0.0, border.ActualWidth, border.ActualHeight), num, num);
		}
	}

	private void MaxRestore_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	protected override void OnStateChanged(EventArgs e)
	{
		base.OnStateChanged(e);
		if (MaxRestorePath != null)
		{
			if (base.WindowState == WindowState.Maximized)
			{
				MaxRestorePath.Data = Geometry.Parse("M8 8 H19 V19 H8 Z M5 16 V5 H16 V8");
				MaxRestoreBtn.ToolTip = "Восстановить";
			}
			else
			{
				MaxRestorePath.Data = Geometry.Parse("M5 5 H19 V19 H5 Z");
				MaxRestoreBtn.ToolTip = "Развернуть";
			}
		}
		RootBorder_SizeChanged(RootBorder, null);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		(PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		switch (msg)
		{
		case 36:
			WmGetMinMaxInfo(hwnd, lParam);
			handled = true;
			return IntPtr.Zero;
		case 132:
			if (base.ResizeMode == ResizeMode.CanResize && base.WindowState != WindowState.Maximized)
			{
				long num = ((IntPtr)lParam).ToInt64();
				int num2 = (short)(num & 0xFFFF);
				int num3 = (short)((num >> 16) & 0xFFFF);
				System.Windows.Point point;
				try
				{
					point = PointFromScreen(new System.Windows.Point(num2, num3));
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
			goto default;
		default:
			return IntPtr.Zero;
		}
	}

	private void WmGetMinMaxInfo(nint hwnd, nint lParam)
	{
		MINMAXINFO structure = Marshal.PtrToStructure<MINMAXINFO>(lParam);
		nint num = MonitorFromWindow(hwnd, 2);
		if (num != IntPtr.Zero)
		{
			MONITORINFO info = new MONITORINFO
			{
				cbSize = Marshal.SizeOf<MONITORINFO>()
			};
			if (GetMonitorInfo(num, ref info))
			{
				RECT rcWork = info.rcWork;
				RECT rcMonitor = info.rcMonitor;
				structure.ptMaxPosition.X = rcWork.Left - rcMonitor.Left;
				structure.ptMaxPosition.Y = rcWork.Top - rcMonitor.Top;
				structure.ptMaxSize.X = rcWork.Right - rcWork.Left;
				structure.ptMaxSize.Y = rcWork.Bottom - rcWork.Top;
			}
		}
		double num2 = 1.0;
		double num3 = 1.0;
		try
		{
			DpiScale dpi = VisualTreeHelper.GetDpi(this);
			num2 = dpi.DpiScaleX;
			num3 = dpi.DpiScaleY;
		}
		catch
		{
		}
		structure.ptMinTrackSize.X = (int)(base.MinWidth * num2);
		structure.ptMinTrackSize.Y = (int)(base.MinHeight * num3);
		Marshal.StructureToPtr(structure, lParam, fDeleteOld: true);
	}

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, int flags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO info);

	private async void ExitBtn_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		if (AppDialog.Confirm(this, Strings.Get("Title_Exit"), Strings.Get("Msg_ExitConfirm"), null, Strings.Get("Exit_YesBtn"), Strings.Get("Btn_No"), DialogKind.Danger))
		{
			Hide();
			try
			{
				await Task.WhenAny(SendReportSilentAsync(), Task.Delay(6000));
			}
			catch
			{
			}
			Application.Current.Shutdown();
		}
	}

	private void Minimize_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void Settings_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		SettingsWindow settingsWindow = new SettingsWindow();
		settingsWindow.Owner = this;
		settingsWindow.ShowDialog();
		SoundIcon.Opacity = (SoundService.Enabled ? 0.7 : 0.3);
	}

	private void ToggleSound_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Enabled = !SoundService.Enabled;
		SoundService.SaveConfig();
		SoundIcon.Opacity = (SoundService.Enabled ? 0.7 : 0.3);
		if (SoundService.Enabled)
		{
			SoundService.Soft();
		}
	}

	private void Language_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		LanguageWindow languageWindow = new LanguageWindow();
		languageWindow.Owner = this;
		languageWindow.ShowDialog();
	}

	private void UpdateLocalization()
	{
		MenuText_Search.Text = Strings.Get("Menu_Search");
		MenuText_USB.Text = Strings.Get("Menu_USB");
		MenuText_LastActivity.Text = Strings.Get("Menu_LastActivity");
		MenuText_Software.Text = Strings.Get("Menu_Software");
		MenuText_Registry.Text = Strings.Get("Menu_Registry");
		MenuText_Other.Text = Strings.Get("Menu_Other");
		MenuText_Steam.Text = Strings.Get("Menu_Steam");
		MenuText_Traces.Text = Strings.Get("Menu_Traces");
		AdminReloadText.Text = Strings.Get("Traces_RunAsAdmin");
		if (!_tracesRunning)
		{
			BtnText_Traces.Text = Strings.Get("Traces_Check");
		}
		BtnReport.ToolTip = Strings.Get("Report_Tooltip");
		MenuText_Processes.Text = Strings.Get("Menu_Processes");
		if (!_procRunning)
		{
			BtnText_Proc.Text = Strings.Get("Proc_Scan");
		}
		PCol_Name.Header = Strings.Get("Proc_ColName");
		PCol_Path.Header = Strings.Get("Col_Path");
		PCol_Note.Header = Strings.Get("Proc_ColNote");
		PCol_Risk.Header = Strings.Get("Col_Risk");
		TextBlock tabTitle = TabTitle;
		string currentTab = _currentTab;
		if (currentTab == null)
		{
			goto IL_02c2;
		}
		int length = currentTab.Length;
		string text;
		if (length <= 6)
		{
			if (length != 3)
			{
				if (length != 6)
				{
					goto IL_02c2;
				}
				char c = currentTab[1];
				if (c != 'e')
				{
					if (c != 'y' || !(currentTab == "System"))
					{
						goto IL_02c2;
					}
					text = Strings.Get("Tab_Other");
				}
				else
				{
					if (!(currentTab == "Search"))
					{
						goto IL_02c2;
					}
					text = Strings.Get("Tab_Search");
				}
			}
			else
			{
				if (!(currentTab == "USB"))
				{
					goto IL_02c2;
				}
				text = Strings.Get("Tab_USB");
			}
		}
		else if (length != 8)
		{
			if (length != 12 || !(currentTab == "LastActivity"))
			{
				goto IL_02c2;
			}
			text = Strings.Get("Tab_LastActivity");
		}
		else
		{
			char c = currentTab[0];
			if (c != 'A')
			{
				if (c != 'R')
				{
					if (c != 'S' || !(currentTab == "Software"))
					{
						goto IL_02c2;
					}
					text = Strings.Get("Tab_Software");
				}
				else
				{
					if (!(currentTab == "Registry"))
					{
						goto IL_02c2;
					}
					text = Strings.Get("Tab_Registry");
				}
			}
			else
			{
				if (!(currentTab == "Accounts"))
				{
					goto IL_02c2;
				}
				text = Strings.Get("Tab_Steam");
			}
		}
		goto IL_02ce;
		IL_02c2:
		text = TabTitle.Text;
		goto IL_02ce;
		IL_02ce:
		tabTitle.Text = text;
		BtnText_Folders.Text = Strings.Get("Btn_Folders");
		BtnText_Search.Text = Strings.Get("Btn_Search");
		BtnText_Stop.Text = Strings.Get("Btn_Stop");
		BtnText_SelectFolders.Text = Strings.Get("Btn_SelectFolders");
		BtnText_StartSearch.Text = Strings.Get("Btn_StartSearch");
		SCol_Name.Header = Strings.Get("Col_Name");
		SCol_Cheat.Header = Strings.Get("Col_Cheat");
		SCol_Type.Header = Strings.Get("Col_Type");
		SCol_Size.Header = Strings.Get("Col_Size");
		SCol_Mod.Header = Strings.Get("Col_Modified");
		SCol_Acc.Header = Strings.Get("Col_Access");
		SCol_Path.Header = Strings.Get("Col_Path");
		SCol_Verdict.Header = Strings.Get("Col_Risk");
		SteamText_CurrentAccount.Text = Strings.Get("Steam_CurrentAccount");
		SteamText_OtherAccounts.Text = Strings.Get("Steam_OtherAccounts");
		BtnText_ExportAccounts.Text = Strings.Get("Btn_ExportAccounts");
		BtnText_CheckBlockDB.Text = Strings.Get("Btn_CheckBlockDB");
		BtnText_CheckBlockIP.Text = Strings.Get("Btn_CheckBlockIP");
		CatText_FileAnalysis.Text = Strings.Get("Cat_FileAnalysis");
		CatText_BrowserAnalysis.Text = Strings.Get("Cat_BrowserAnalysis");
		CatText_GameAnalysis.Text = Strings.Get("Cat_GameAnalysis");
		CatText_RegistryAnalysis.Text = Strings.Get("Cat_RegistryAnalysis");
		CatText_SystemApps.Text = Strings.Get("Cat_SystemApps");
		BtnText_DataUsage.Text = Strings.Get("Btn_DataUsage");
		BtnText_Nvidia.Text = Strings.Get("Btn_Nvidia");
		BtnText_Services.Text = Strings.Get("Btn_Services");
		CatText_KeyEmulation.Text = Strings.Get("Cat_KeyEmulation");
		BtnText_AutoCheck.Text = Strings.Get("Btn_AutoCheck");
		BtnText_Keyboard.Text = Strings.Get("Btn_Keyboard");
		CatText_Macros.Text = Strings.Get("Cat_Macros");
		CatText_MacrosSubtitle.Text = Strings.Get("Cat_MacrosHint");
		BtnText_MouseApp.Text = Strings.Get("Btn_MouseApp");
		BtnText_MacroCheck.Text = Strings.Get("Btn_MacroCheck");
		BtnText_LoadUSB.Text = Strings.Get("Btn_LoadUSB");
		UCol_Device.Header = Strings.Get("Col_Device");
		UCol_Type.Header = Strings.Get("Col_DevType");
		UCol_Letter.Header = Strings.Get("Col_Letter");
		UCol_Conn.Header = Strings.Get("Col_Connected");
		UCol_Disc.Header = Strings.Get("Col_Disconnected");
		BtnText_LoadToolbar.Text = Strings.Get("Btn_Load");
		BtnText_ResetToolbar.Text = Strings.Get("Btn_Reset");
		BtnText_LoadOverlay.Text = Strings.Get("Btn_Load");
		BtnText_ResetOverlay.Text = Strings.Get("Btn_Reset");
		ACol_Name.Header = Strings.Get("Col_FileName");
		ACol_Path.Header = Strings.Get("Col_Path");
		ACol_Date.Header = Strings.Get("Col_Date");
		BtnText_StartAnalysis.Text = Strings.Get("Btn_StartAnalysis");
		RCol_App.Header = Strings.Get("Col_Application");
		RCol_Path.Header = Strings.Get("Col_Path");
		NotifText_Title.Text = Strings.Get("Notif_Title");
		BtnSound.ToolTip = Strings.Get("Tip_Sound");
		BtnNotif.ToolTip = Strings.Get("Tip_Notifications");
		BtnSettings.ToolTip = Strings.Get("Tip_Settings");
		BtnLanguage.ToolTip = Strings.Get("Tip_Language");
		GameStatusText.Text = Strings.Get("Game_NotRunning");
		UsbStatus.Text = Strings.Get("Status_Loading");
		RegistryStatus.Text = Strings.Get("Status_Loading");
		ActivityStatus.Text = Strings.Get("Status_LoadingActivity");
		if (_scanner == null || !_scanner.IsRunning)
		{
			SearchCount2.Text = Strings.Get("Search_Found");
		}
	}

	private void OpenNotifications_Click(object sender, RoutedEventArgs e)
	{
		NotificationsPanel.Visibility = ((NotificationsPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void CloseNotifications_Click(object sender, RoutedEventArgs e)
	{
		NotificationsPanel.Visibility = Visibility.Collapsed;
	}

	private async Task RunAutoDetectAsync()
	{
		try
		{
			List<AutoDetectResult> list = await AutoDetectService.DetectAllAsync();
			if (list == null || list.Count == 0)
			{
				NothingDetectedText.Visibility = Visibility.Visible;
				NotificationRedDot.Visibility = Visibility.Collapsed;
			}
			else
			{
				AutoDetectList.ItemsSource = list;
				NotificationRedDot.Visibility = Visibility.Visible;
			}
		}
		catch (Exception)
		{
		}
	}

	private void AutoDetectAction_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: var tag } && tag is int num && num == 2)
		{
			if (WindowsServices.Start("DusmSvc"))
			{
				AppDialog.Alert(this, Strings.Get("DataUsage_Title"), Strings.Get("AD_ServiceStarted"), null, DialogKind.Success);
			}
			else
			{
				AppDialog.Alert(this, Strings.Get("DataUsage_Title"), Strings.Get("AD_ServiceStartFail"), null, DialogKind.Warning);
			}
		}
	}

	private void SelectSearchFolders_Click(object sender, RoutedEventArgs e)
	{
		SearchOptionsWindow searchOptionsWindow = new SearchOptionsWindow
		{
			Owner = this
		};
		if (searchOptionsWindow.ShowDialog() == true)
		{
			_searchDirectories = searchOptionsWindow.SelectedDirectories;
			_useIconMatch = searchOptionsWindow.UseIconMatch;
			_useSignatureMatch = searchOptionsWindow.UseSignatureMatch;
		}
	}

	private async void StartSearch_Click(object sender, RoutedEventArgs e)
	{
		if (_searchDirectories == null || _searchDirectories.Count == 0)
		{
			SearchOptionsWindow searchOptionsWindow = new SearchOptionsWindow
			{
				Owner = this
			};
			if (searchOptionsWindow.ShowDialog() == true)
			{
				_searchDirectories = searchOptionsWindow.SelectedDirectories;
				_useIconMatch = searchOptionsWindow.UseIconMatch;
				_useSignatureMatch = searchOptionsWindow.UseSignatureMatch;
			}
			if (_searchDirectories == null || _searchDirectories.Count == 0)
			{
				return;
			}
		}
		if (_scanner != null && _scanner.IsRunning)
		{
			AppDialog.Alert(this, Strings.Get("Title_Search"), Strings.Get("Msg_SearchRunning"), null, DialogKind.Warning);
			return;
		}
		SearchBlur.Radius = 0.0;
		SearchOverlay.Visibility = Visibility.Collapsed;
		SearchToolbar.Visibility = Visibility.Visible;
		StopSearchBtn.Visibility = Visibility.Visible;
		StartSearchBtn.IsEnabled = false;
		SetVerdictState("scanning");
		VerdictTitle.Text = "Идёт проверка";
		VerdictSub.Text = "Подготовка…";
		VerdictNum.Text = "0";
		VerdictCap.Text = "ПРОВЕРЕНО";
		ObservableCollection<FoundedFile> results = new ObservableCollection<FoundedFile>();
		SearchResultsGrid.ItemsSource = results;
		ICollectionView defaultView = CollectionViewSource.GetDefaultView(results);
		defaultView.SortDescriptions.Clear();
		defaultView.SortDescriptions.Add(new SortDescription("SeverityRank", ListSortDirection.Ascending));
		defaultView.SortDescriptions.Add(new SortDescription("Score", ListSortDirection.Descending));
		if (defaultView is ICollectionViewLiveShaping collectionViewLiveShaping)
		{
			collectionViewLiveShaping.LiveSortingProperties.Add("SeverityRank");
			collectionViewLiveShaping.LiveSortingProperties.Add("Score");
			collectionViewLiveShaping.IsLiveSorting = true;
		}
		_searchStopwatch = Stopwatch.StartNew();
		_searchTimer = new Timer(1000.0);
		_searchTimer.Elapsed += delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				string text = $"⏱  {_searchStopwatch.Elapsed:hh\\:mm\\:ss}";
				SearchTimer2.Text = text;
			});
		};
		_searchTimer.Start();
		_scanner = new FileScanner();
		_scanner.FileFound += delegate(FoundedFile f)
		{
			base.Dispatcher.Invoke(delegate
			{
				results.Add(f);
				SearchCount2.Text = Strings.Get("Search_FoundFmt", results.Count);
			});
		};
		_scanner.Progress += delegate(long scanned, long total, string dir)
		{
			base.Dispatcher.Invoke(delegate
			{
				VerdictNum.Text = scanned.ToString("N0", CultureInfo.CurrentCulture);
				VerdictSub.Text = dir + "\\…";
				if (total > 0)
				{
					VerdictBar.IsIndeterminate = false;
					VerdictBar.Maximum = total;
					VerdictBar.Value = Math.Min(scanned, total);
					VerdictCap.Text = $"{scanned * 100 / Math.Max(1L, total)}% · ПРОВЕРЕНО";
				}
			});
		};
		_scanner.SearchCompleted += delegate(bool cancelled, long ms)
		{
			base.Dispatcher.Invoke(delegate
			{
				_searchTimer?.Stop();
				SearchTimer2.Text = $"⏱  {_searchStopwatch.Elapsed:hh\\:mm\\:ss}";
				SearchCount2.Text = Strings.Get("Search_FoundFmt", results.Count);
				StartSearchBtn.IsEnabled = true;
				StopSearchBtn.Visibility = Visibility.Collapsed;
				if (cancelled)
				{
					SetVerdictState("idle");
					VerdictTitle.Text = "Проверка остановлена";
					VerdictSub.Text = "Результаты неполные — запустите проверку заново";
					VerdictNum.Text = "—";
					VerdictCap.Text = "СОВПАДЕНИЙ";
				}
				else if (results.Count > 0)
				{
					int num = results.Count((FoundedFile x) => x.Severity == "red");
					int num2 = results.Count((FoundedFile x) => x.Severity == "amber");
					int value = results.Count((FoundedFile x) => x.Severity == "mint");
					if (num > 0)
					{
						SetVerdictState("found");
						VerdictTitle.Text = "Обнаружено запрещённое ПО";
						VerdictSub.Text = $"Критический: {num} · средний: {num2} · низкий: {value}";
						VerdictNum.Text = num.ToString();
						VerdictCap.Text = "КРИТИЧЕСКИЙ";
					}
					else if (num2 > 0)
					{
						SetVerdictState("attention");
						VerdictTitle.Text = "Найдены подозрительные файлы";
						VerdictSub.Text = $"Средний: {num2} · низкий: {value} — проверьте вручную";
						VerdictNum.Text = num2.ToString();
						VerdictCap.Text = "СРЕДНИЙ";
					}
					else
					{
						SetVerdictState("clean");
						VerdictTitle.Text = "Найдены совпадения низкого риска";
						VerdictSub.Text = $"Низкий риск: {value} — вероятно, ложные срабатывания";
						VerdictNum.Text = value.ToString();
						VerdictCap.Text = "НИЗКИЙ";
					}
				}
				else
				{
					SetVerdictState("clean");
					VerdictTitle.Text = "Запрещённое ПО не найдено";
					VerdictSub.Text = "Проверка завершена, совпадений нет";
					VerdictNum.Text = "0";
					VerdictCap.Text = "СОВПАДЕНИЙ";
				}
				ExportSearchResults(results);
				_lastSearchResults = results.ToList();
				SendReportSilentAsync();
				string candSteam = App.CurrentSteamID ?? GetSelectedAccount()?.SteamID64;
				List<FoundedFile> candFiles = _lastSearchResults;
				Task.Run(async delegate
				{
					try
					{
						string text = CandidateBuilder.BuildJson(candFiles);
						if (text != null)
						{
							await _api.SaveCandidatesAsync(text, App.HWID, candSteam);
						}
					}
					catch
					{
					}
				});
			});
		};
		await _scanner.StartAsync(_searchDirectories, _cheatDb, _useIconMatch, _useSignatureMatch);
	}

	private void SetVerdictState(string state)
	{
		System.Windows.Media.Color Accent;
		switch (state)
		{
		case "scanning":
			Accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0A93C");
			break;
		case "attention":
			Accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0A93C");
			break;
		case "found":
			Accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF283F");
			break;
		case "clean":
			Accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34D399");
			break;
		default:
			Accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5E4F5B");
			break;
		}
		SolidColorBrush solidColorBrush = new SolidColorBrush(Accent);
		SolidColorBrush Br(byte alpha) => new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, Accent.R, Accent.G, Accent.B));
		if (state == "idle")
		{
			VerdictBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("Brush_Line");
			VerdictLamp.Background = (System.Windows.Media.Brush)FindResource("Brush_Panel2");
			VerdictLamp.BorderBrush = (System.Windows.Media.Brush)FindResource("Brush_Line2");
			VerdictNum.Foreground = (System.Windows.Media.Brush)FindResource("Brush_Text");
		}
		else
		{
			VerdictBorder.BorderBrush = Br(102);
			VerdictLamp.Background = Br(31);
			VerdictLamp.BorderBrush = Br(153);
			VerdictNum.Foreground = solidColorBrush;
		}
		VerdictLampPath.Data = Geometry.Parse(state switch
		{
			"scanning" => "M18 11 A7 7 0 1 1 4 11 A7 7 0 1 1 18 11 M20 20 l-3.6 -3.6", 
			"found" => "M12 3 L3 20 h18 Z M12 10 v4 M12 17 h.01", 
			"attention" => "M12 3 L3 20 h18 Z M12 10 v4 M12 17 h.01", 
			"clean" => "M21 12 A9 9 0 1 1 3 12 A9 9 0 1 1 21 12 M8 12 l3 3 l5 -6", 
			_ => "M21 12 A9 9 0 1 1 3 12 A9 9 0 1 1 21 12 M12 8 v5 M12 16 h.01", 
		});
		VerdictLampPath.Stroke = ((state == "idle") ? ((System.Windows.Media.Brush)FindResource("Brush_Faint")) : solidColorBrush);
		if (state == "scanning")
		{
			VerdictBar.Visibility = Visibility.Visible;
			VerdictBar.IsIndeterminate = true;
			StartLampPulse();
		}
		else
		{
			VerdictBar.IsIndeterminate = false;
			VerdictBar.Visibility = Visibility.Collapsed;
			StopLampPulse();
		}
	}

	private void StartLampPulse()
	{
		StopLampPulse();
		DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 0.4, new Duration(TimeSpan.FromMilliseconds(750.0)))
		{
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		_lampPulse = new Storyboard();
		_lampPulse.Children.Add(doubleAnimation);
		Storyboard.SetTarget(doubleAnimation, VerdictLamp);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Opacity"));
		_lampPulse.Begin();
	}

	private void StopLampPulse()
	{
		_lampPulse?.Stop();
		_lampPulse = null;
		VerdictLamp.Opacity = 1.0;
	}

	private void ExportSearchResults(ObservableCollection<FoundedFile> results)
	{
		try
		{
			if (results == null || results.Count == 0)
			{
				return;
			}
			string text = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
			Directory.CreateDirectory(text);
			string path = System.IO.Path.Combine(text, $"ExportSearch_{DateTime.Now:yyyyMMdd-HH-mm-ss}.txt");
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
			handler.AppendLiteral("===== ");
			handler.AppendFormatted(Strings.Get("Search_ExportTitle"));
			handler.AppendLiteral(" — ");
			handler.AppendFormatted(DateTime.Now, "dd.MM.yyyy HH:mm:ss");
			handler.AppendLiteral(" =====");
			stringBuilder3.AppendLine(ref handler);
			stringBuilder.AppendLine(Strings.Get("Search_ExportCount", results.Count));
			stringBuilder.AppendLine();
			foreach (FoundedFile result in results)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 5, stringBuilder2);
				handler.AppendFormatted(result.Name);
				handler.AppendLiteral(" | ");
				handler.AppendFormatted(result.CheatName);
				handler.AppendLiteral(" | ");
				handler.AppendFormatted(result.Weight);
				handler.AppendLiteral(" | ");
				handler.AppendFormatted(result.LastChange);
				handler.AppendLiteral(" | ");
				handler.AppendFormatted(result.Path);
				stringBuilder4.AppendLine(ref handler);
			}
			File.WriteAllText(path, stringBuilder.ToString());
		}
		catch
		{
		}
	}

	private void StopSearch_Click(object sender, RoutedEventArgs e)
	{
		_scanner?.Stop();
	}

	private void SearchResult_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (SearchResultsGrid.SelectedItem is FoundedFile foundedFile)
		{
			try
			{
				Process.Start("explorer.exe", "/select,\"" + foundedFile.Path + "\"");
			}
			catch
			{
			}
		}
	}

	private SteamAccount GetSelectedAccount()
	{
		object obj = _accounts?.FirstOrDefault((SteamAccount a) => a.IsCurrent);
		if (obj == null)
		{
			List<SteamAccount> accounts = _accounts;
			if (accounts == null)
			{
				return null;
			}
			obj = accounts.FirstOrDefault();
		}
		return (SteamAccount)obj;
	}

	private void OpenSteamProfile_Click(object sender, RoutedEventArgs e)
	{
		SteamAccount selectedAccount = GetSelectedAccount();
		if (selectedAccount == null)
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo("https://steamcommunity.com/profiles/" + selectedAccount.SteamID64)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void CopySteamId_Click(object sender, RoutedEventArgs e)
	{
		SteamAccount selectedAccount = GetSelectedAccount();
		if (selectedAccount != null)
		{
			try
			{
				Clipboard.SetText(selectedAccount.SteamID64 ?? "");
			}
			catch
			{
			}
		}
	}

	private void CheckBlockDB_Click(object sender, RoutedEventArgs e)
	{
		if (_accounts == null || _accounts.Count == 0)
		{
			AppDialog.Alert(this, Strings.Get("Title_BlockDb"), Strings.Get("Msg_NoAccounts"));
			return;
		}
		BlockDbWindow blockDbWindow = new BlockDbWindow(_accounts, _api, byIp: false, null, _projectInfo?.ProjectName);
		blockDbWindow.Owner = this;
		blockDbWindow.ShowDialog();
		OtherAccountsList.Items.Refresh();
		PopulateCurrentAccount();
	}

	private void CheckBlockIP_Click(object sender, RoutedEventArgs e)
	{
		string text = _projectInfo?.IP;
		if (string.IsNullOrEmpty(text))
		{
			AppDialog.Alert(this, Strings.Get("Title_BlockDb"), Strings.Get("Msg_NoIp"));
			return;
		}
		BlockDbWindow blockDbWindow = new BlockDbWindow(_accounts, _api, byIp: true, text, _projectInfo?.ProjectName);
		blockDbWindow.Owner = this;
		blockDbWindow.ShowDialog();
	}

	private void ExportAccounts_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			List<string> list = new List<string>();
			foreach (SteamAccount item in _accounts ?? new List<SteamAccount>())
			{
				list.Add($"{item.Nickname}\t{item.SteamID64}\tVAC:{(item.Vac_Ban ? Strings.Get("Word_Yes") : Strings.Get("Word_No"))}\tLvl:{item.Lvl}");
			}
			string text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"NiposChecker_Accounts_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
			File.WriteAllLines(text, list);
			AppDialog.Alert(this, Strings.Get("Title_Export"), Strings.Get("Msg_Exported", _accounts?.Count ?? 0, text), null, DialogKind.Success);
		}
		catch (Exception ex)
		{
			AppDialog.Alert(this, Strings.Get("Title_Error"), Strings.Get("Msg_Error", ex.Message), null, DialogKind.Danger);
		}
	}

	private async void StartUSB_Click(object sender, RoutedEventArgs e)
	{
		StartUsbBtn.Visibility = Visibility.Collapsed;
		UsbProgressPanel.Visibility = Visibility.Visible;
		UsbStatus.Text = Strings.Get("Status_UsbStart");
		try
		{
			List<FoundedUSB> list = await NirSoftTables.LoadUsbAsync();
			FoundedUSB foundedUSB = list.FirstOrDefault((FoundedUSB x) => x.HasDisconnectDate);
			if (foundedUSB != null)
			{
				foundedUSB.IsLastDisconnected = true;
			}
			UsbGrid.ItemsSource = list;
			UsbBlur.Radius = 0.0;
			UsbOverlay.Visibility = Visibility.Collapsed;
		}
		catch (Exception ex)
		{
			UsbStatus.Text = Strings.Get("Msg_Error", ex.Message);
		}
	}

	private static bool IsElevated()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private void RelaunchAsAdmin_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string text = Process.GetCurrentProcess().MainModule?.FileName;
			if (!string.IsNullOrEmpty(text))
			{
				Process.Start(new ProcessStartInfo(text)
				{
					UseShellExecute = true,
					Verb = "runas"
				});
				Application.Current.Shutdown();
			}
		}
		catch
		{
		}
	}

	private async void StartProcScan_Click(object sender, RoutedEventArgs e)
	{
		if (_procRunning)
		{
			return;
		}
		_procRunning = true;
		SoundService.Click();
		BtnText_Proc.Text = Strings.Get("Proc_Scanning");
		ProcOverlay.Visibility = Visibility.Visible;
		ProcOverlayTitle.Text = Strings.Get("Proc_Scanning");
		ProcSummary.Visibility = Visibility.Collapsed;
		try
		{
			var (list, num) = await Task.Run(() => ProcessScanner.Scan(_cheatDb));
			ProcGrid.ItemsSource = list;
			int num2 = list.Count((ProcessItem i) => i.Level == "alert");
			int num3 = list.Count((ProcessItem i) => i.Level == "warn");
			ProcSummaryNum.Text = list.Count.ToString();
			if (num2 > 0)
			{
				ProcSummaryTitle.Text = Strings.Get("Proc_FoundTitle");
				ProcSummarySub.Text = Strings.Get("Proc_FoundSub", num2, num3, num);
			}
			else if (num3 > 0)
			{
				ProcSummaryTitle.Text = Strings.Get("Proc_WarnTitle");
				ProcSummarySub.Text = Strings.Get("Proc_WarnSub", num3, num);
			}
			else
			{
				ProcSummaryTitle.Text = Strings.Get("Proc_CleanTitle");
				ProcSummarySub.Text = Strings.Get("Proc_CleanSub", num);
			}
			ProcSummary.Visibility = Visibility.Visible;
			ProcOverlay.Visibility = ((list.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
			if (list.Count == 0)
			{
				ProcOverlayTitle.Text = Strings.Get("Proc_None");
			}
		}
		catch (Exception ex)
		{
			ProcOverlayTitle.Text = Strings.Get("Msg_Error", ex.Message);
		}
		finally
		{
			BtnText_Proc.Text = Strings.Get("Proc_Rescan");
			_procRunning = false;
		}
	}

	private void ProcGrid_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (ProcGrid.SelectedItem is ProcessItem processItem && !string.IsNullOrEmpty(processItem.Path) && File.Exists(processItem.Path))
		{
			try
			{
				Process.Start("explorer.exe", "/select,\"" + processItem.Path + "\"");
			}
			catch
			{
			}
		}
	}

	private async Task<string> BuildAndSendReportAsync()
	{
		string reportJson = await ReportBuilder.BuildAsync(_api, _cheatDb, _projectInfo, _accounts, _lastSearchResults);
		string steam = App.CurrentSteamID ?? GetSelectedAccount()?.SteamID64;
		return await _api.SaveReportAsync(reportJson, App.HWID, steam, _sessionId);
	}

	private async Task SendReportSilentAsync()
	{
		if (!PrivacyConsent.Granted)
		{
			return;
		}
		try
		{
			await BuildAndSendReportAsync();
		}
		catch
		{
		}
	}

	private async void SendReport_Click(object sender, RoutedEventArgs e)
	{
		if (_reportSending)
		{
			return;
		}
		_reportSending = true;
		SoundService.Click();
		BtnReport.IsEnabled = false;
		Mouse.OverrideCursor = Cursors.Wait;
		try
		{
			string value = await BuildAndSendReportAsync();
			Mouse.OverrideCursor = null;
			if (string.IsNullOrEmpty(value))
			{
				AppDialog.Alert(this, Strings.Get("Report_Title"), Strings.Get("Report_Fail"), null, DialogKind.Danger);
			}
			else
			{
				AppDialog.Alert(this, Strings.Get("Report_Title"), Strings.Get("Report_Sent"), null, DialogKind.Success);
			}
		}
		catch (Exception ex)
		{
			Mouse.OverrideCursor = null;
			AppDialog.Alert(this, Strings.Get("Report_Title"), Strings.Get("Msg_Error", ex.Message), null, DialogKind.Danger);
		}
		finally
		{
			Mouse.OverrideCursor = null;
			BtnReport.IsEnabled = true;
			_reportSending = false;
		}
	}

	private void History_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		SteamAccount selectedAccount = GetSelectedAccount();
		string text = selectedAccount?.SteamID64 ?? App.CurrentSteamID;
		if (string.IsNullOrEmpty(text))
		{
			AppDialog.Alert(this, "История проверок", "Не удалось определить SteamID аккаунта.");
			return;
		}
		ReportHistoryWindow reportHistoryWindow = new ReportHistoryWindow(_api, text, selectedAccount?.Nickname);
		reportHistoryWindow.Owner = this;
		reportHistoryWindow.ShowDialog();
	}

	private void TraceDetails_Click(object sender, RoutedEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is TraceSignal { HasDetails: not false } traceSignal)
		{
			TraceDetailsWindow traceDetailsWindow = new TraceDetailsWindow(traceSignal.Title, traceSignal.Items, traceSignal.DetailCols, traceSignal.SortPaths);
			traceDetailsWindow.Owner = this;
			traceDetailsWindow.ShowDialog();
		}
	}

	private async void TraceRepair_Click(object sender, RoutedEventArgs e)
	{
		if (!((sender as FrameworkElement)?.DataContext is TraceSignal traceSignal) || traceSignal.RepairKind != "datausage")
		{
			return;
		}
		SoundService.Click();
		Button btn = sender as Button;
		TextBlock tb = btn?.Content as TextBlock;
		string prev = tb?.Text;
		if (tb != null)
		{
			tb.Text = "Восстанавливаю…";
		}
		if (btn != null)
		{
			btn.IsEnabled = false;
		}
		try
		{
			(bool, string) tuple = await Task.Run(() => SrumRepair.RestoreDataUsage());
			AppDialog.Alert(this, "Использование данных", tuple.Item1 ? "Учёт восстановлен" : "Не получилось", tuple.Item2, (!tuple.Item1) ? DialogKind.Warning : DialogKind.Success);
			if (tuple.Item1 && !_tracesRunning)
			{
				StartTraces_Click(this, null);
			}
		}
		finally
		{
			if (btn != null)
			{
				btn.IsEnabled = true;
			}
			if (tb != null && prev != null)
			{
				tb.Text = prev;
			}
		}
	}

	private async void StartTraces_Click(object sender, RoutedEventArgs e)
	{
		if (_tracesRunning)
		{
			return;
		}
		_tracesRunning = true;
		SoundService.Click();
		BtnText_Traces.Text = Strings.Get("Traces_Checking");
		TracesOverlay.Visibility = Visibility.Visible;
		TracesOverlayTitle.Text = Strings.Get("Traces_Checking");
		TracesSummary.Visibility = Visibility.Collapsed;
		try
		{
			List<TraceSignal> list = await Task.Run(() => CleanupDetector.Run(_cheatDb));
			TracesList.ItemsSource = list;
			int num = list.Count((TraceSignal s) => s.Level == "alert");
			int num2 = list.Count((TraceSignal s) => s.Level == "warn");
			TracesSummaryNum.Text = (num + num2).ToString();
			if (num > 0)
			{
				TracesSummaryTitle.Text = Strings.Get("Traces_FoundTitle");
				TracesSummarySub.Text = Strings.Get("Traces_FoundSub", num, num2);
			}
			else if (num2 > 0)
			{
				TracesSummaryTitle.Text = Strings.Get("Traces_WarnTitle");
				TracesSummarySub.Text = Strings.Get("Traces_WarnSub", num2);
			}
			else
			{
				TracesSummaryTitle.Text = Strings.Get("Traces_CleanTitle");
				TracesSummarySub.Text = Strings.Get("Traces_CleanSub");
			}
			TracesSummary.Visibility = Visibility.Visible;
			TracesOverlay.Visibility = ((list.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
			if (list.Count == 0)
			{
				TracesOverlayTitle.Text = Strings.Get("Traces_None");
			}
		}
		catch (Exception ex)
		{
			TracesOverlayTitle.Text = Strings.Get("Msg_Error", ex.Message);
		}
		finally
		{
			BtnText_Traces.Text = Strings.Get("Traces_Recheck");
			_tracesRunning = false;
		}
	}

	private async void StartLastActivity_Click(object sender, RoutedEventArgs e)
	{
		ActivityControls.Visibility = Visibility.Collapsed;
		ActivityProgressPanel.Visibility = Visibility.Visible;
		try
		{
			_allActivity = await NirSoftTables.LoadLastActivityAsync();
			await Task.Run(delegate
			{
				MarkSuspiciousActivity(_allActivity);
				Parallel.ForEach(_allActivity, new ParallelOptions
				{
					MaxDegreeOfParallelism = Environment.ProcessorCount
				}, delegate(ActivityItem it)
				{
					it.Icon = TryGetFileIcon(it.Path, it.Name);
				});
			});
			ObservableCollection<ActivityItem> source = new ObservableCollection<ActivityItem>(_allActivity);
			_activityView = CollectionViewSource.GetDefaultView(source);
			ActivityGrid.ItemsSource = _activityView;
			ApplyActivityFilter();
			ActivityBlur.Radius = 0.0;
			ActivityOverlay.Visibility = Visibility.Collapsed;
			ActivityToolbar.Visibility = Visibility.Visible;
		}
		catch (Exception ex)
		{
			ActivityProgressPanel.Visibility = Visibility.Collapsed;
			ActivityControls.Visibility = Visibility.Visible;
			ActivityStatus.Text = Strings.Get("Msg_Error", ex.Message);
		}
	}

	private void ResetActivityFilter_Click(object sender, RoutedEventArgs e)
	{
		_syncingFilters = true;
		try
		{
			CheckBox filterExe = FilterExe;
			CheckBox filterRar = FilterRar;
			CheckBox filterZip = FilterZip;
			CheckBox filterAhk = FilterAhk;
			bool? flag = (FilterCs2.IsChecked = false);
			bool? flag3 = (filterAhk.IsChecked = flag);
			bool? flag5 = (filterZip.IsChecked = flag3);
			bool? isChecked = (filterRar.IsChecked = flag5);
			filterExe.IsChecked = isChecked;
			CheckBox filterExe2 = FilterExe2;
			CheckBox filterRar2 = FilterRar2;
			CheckBox filterZip2 = FilterZip2;
			CheckBox filterAhk2 = FilterAhk2;
			flag = (FilterCs22.IsChecked = false);
			flag3 = (filterAhk2.IsChecked = flag);
			flag5 = (filterZip2.IsChecked = flag3);
			isChecked = (filterRar2.IsChecked = flag5);
			filterExe2.IsChecked = isChecked;
			ActivitySearch.Text = "";
			ActivitySearch2.Text = "";
		}
		finally
		{
			_syncingFilters = false;
		}
		ApplyActivityFilter();
	}

	private void ActivityFilter_Changed(object sender, RoutedEventArgs e)
	{
		if (_syncingFilters)
		{
			return;
		}
		_syncingFilters = true;
		try
		{
			if (sender is CheckBox { IsChecked: var isChecked, Name: var name })
			{
				switch (name)
				{
				case "FilterExe":
					FilterExe2.IsChecked = isChecked;
					break;
				case "FilterExe2":
					FilterExe.IsChecked = isChecked;
					break;
				case "FilterRar":
					FilterRar2.IsChecked = isChecked;
					break;
				case "FilterRar2":
					FilterRar.IsChecked = isChecked;
					break;
				case "FilterZip":
					FilterZip2.IsChecked = isChecked;
					break;
				case "FilterZip2":
					FilterZip.IsChecked = isChecked;
					break;
				case "FilterAhk":
					FilterAhk2.IsChecked = isChecked;
					break;
				case "FilterAhk2":
					FilterAhk.IsChecked = isChecked;
					break;
				case "FilterCs2":
					FilterCs22.IsChecked = isChecked;
					break;
				case "FilterCs22":
					FilterCs2.IsChecked = isChecked;
					break;
				}
			}
		}
		finally
		{
			_syncingFilters = false;
		}
		ApplyActivityFilter();
	}

	private void ActivitySearch_Changed(object sender, TextChangedEventArgs e)
	{
		if (_syncingFilters)
		{
			return;
		}
		_syncingFilters = true;
		try
		{
			if (sender == ActivitySearch && ActivitySearch2 != null)
			{
				ActivitySearch2.Text = ActivitySearch.Text;
			}
			else if (sender == ActivitySearch2 && ActivitySearch != null)
			{
				ActivitySearch.Text = ActivitySearch2.Text;
			}
		}
		finally
		{
			_syncingFilters = false;
		}
		ApplyActivityFilter();
	}

	private void ApplyActivityFilter()
	{
		if (_activityView == null)
		{
			return;
		}
		string text = ActivitySearch.Text?.ToLowerInvariant() ?? "";
		List<string> exts = new List<string>();
		if (FilterExe.IsChecked == true)
		{
			exts.Add(".exe");
		}
		if (FilterRar.IsChecked == true)
		{
			exts.Add(".rar");
		}
		if (FilterZip.IsChecked == true)
		{
			exts.Add(".zip");
		}
		if (FilterAhk.IsChecked == true)
		{
			exts.Add(".ahk");
		}
		if (FilterCs2.IsChecked == true)
		{
			exts.Add("cs2");
		}
		CheckBox activityRegex = ActivityRegex;
		bool num = (activityRegex != null && activityRegex.IsChecked == true) || (ActivityRegex2?.IsChecked == true);
		Regex rx = null;
		string text2 = ActivitySearch.Text ?? "";
		if (num && !string.IsNullOrEmpty(text2))
		{
			try
			{
				rx = new Regex(text2, RegexOptions.IgnoreCase);
			}
			catch
			{
				rx = null;
			}
		}
		_activityView.Filter = delegate(object obj2)
		{
			ActivityItem item = obj2 as ActivityItem;
			if (item == null)
			{
				return false;
			}
			if (exts.Count > 0 && !exts.Any(delegate(string e)
			{
				string extension = item.Extension;
				return (extension != null && extension.Contains(e)) || (item.Path?.ToLowerInvariant().Contains(e) ?? false);
			}))
			{
				return false;
			}
			if (rx != null)
			{
				if (!rx.IsMatch(item.Path ?? ""))
				{
					return rx.IsMatch(item.Name ?? "");
				}
				return true;
			}
			if (!string.IsNullOrEmpty(text))
			{
				string name = item.Name;
				if (name == null || !name.ToLowerInvariant().Contains(text))
				{
					string path = item.Path;
					if (path == null || !path.ToLowerInvariant().Contains(text))
					{
						return false;
					}
				}
			}
			return true;
		};
	}

	private void ActivityGrid_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (!(ActivityGrid.SelectedItem is ActivityItem activityItem) || string.IsNullOrEmpty(activityItem.Path))
		{
			return;
		}
		try
		{
			if (File.Exists(activityItem.Path))
			{
				Process.Start("explorer.exe", "/select,\"" + activityItem.Path + "\"");
				return;
			}
			string text = null;
			try
			{
				text = System.IO.Path.GetDirectoryName(activityItem.Path);
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
			{
				Process.Start("explorer.exe", "\"" + text + "\"");
			}
		}
		catch
		{
		}
	}

	private void Open_UserAssist_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("UserAssistView.exe");
	}

	private void Open_SystemInformer_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("SystemInformer\\SystemInformer.exe");
	}

	private void Open_BrowserDownloads_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("BrowserDownloadsView.exe");
	}

	private void Open_ShellbagAnalyzer_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("ShellbagAnalyzer.exe");
	}

	private void Open_Everything_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("Everything.exe");
	}

	private void Open_BrowsingHistory_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("BrowsingHistoryView.exe");
	}

	private void Open_OpenedFiles_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("OpenedFilesView.exe");
	}

	private void Open_RegistryFinder_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("RegistryFinder.exe");
	}

	private async void Open_ExecutedPrograms_Click(object s, RoutedEventArgs e)
	{
		_ = 1;
		try
		{
			_allActivity = await NirSoftTables.LoadExecutedProgramsAsync();
			await Task.Run(delegate
			{
				MarkSuspiciousActivity(_allActivity);
			});
			ObservableCollection<ActivityItem> source = new ObservableCollection<ActivityItem>(_allActivity);
			_activityView = CollectionViewSource.GetDefaultView(source);
			ActivityGrid.ItemsSource = _activityView;
			ApplyActivityFilter();
			ActivityBlur.Radius = 0.0;
			ActivityOverlay.Visibility = Visibility.Collapsed;
			ActivityToolbar.Visibility = Visibility.Visible;
			foreach (RadioButton item in FindMenuRadioButtons())
			{
				if (item.Tag?.ToString() == "LastActivity")
				{
					item.IsChecked = true;
					break;
				}
			}
		}
		catch
		{
			AppsLauncher.OpenFile("ExecutedProgramsList.exe");
		}
	}

	private void MarkSuspiciousActivity(List<ActivityItem> items)
	{
		if (items == null || _cheatDb == null)
		{
			return;
		}
		Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (ActivityItem item in items)
		{
			string text = "";
			if (_cheatDb.NameLooksLikeCheat(item.Name, out var exact))
			{
				text = (exact ? "red" : "amber");
			}
			string path = item.Path;
			string onDisk = "";
			bool flag = false;
			if (!string.IsNullOrEmpty(path))
			{
				try
				{
					flag = File.Exists(path);
				}
				catch
				{
				}
				if (flag)
				{
					onDisk = "есть";
					if (IsUserFolderPath(path))
					{
						try
						{
							DetectionResult detectionResult = _cheatDb.Evaluate(path);
							if (detectionResult != null)
							{
								text = ((detectionResult.Severity == "red") ? "red" : ((text == "red") ? "red" : "amber"));
							}
						}
						catch
						{
						}
					}
				}
				else
				{
					onDisk = "нет";
					if (IsUserFolderPath(path))
					{
						string text2 = FindRenamedCheat(path, item.Name, cache);
						if (text2 != null)
						{
							onDisk = "переименован? → " + text2;
							if (text == "")
							{
								text = "amber";
							}
						}
					}
				}
			}
			item.Level = text;
			item.IsSuspicious = text != "";
			item.OnDisk = onDisk;
		}
	}

	private string FindRenamedCheat(string gonePath, string goneName, Dictionary<string, List<string>> cache)
	{
		try
		{
			string directoryName = System.IO.Path.GetDirectoryName(gonePath);
			if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName))
			{
				return null;
			}
			if (!cache.TryGetValue(directoryName, out var value))
			{
				value = new List<string>();
				int num = 0;
				foreach (string item in Directory.EnumerateFiles(directoryName, "*.exe"))
				{
					if (++num > 60)
					{
						break;
					}
					try
					{
						if (_cheatDb.Evaluate(item) != null)
						{
							value.Add(System.IO.Path.GetFileName(item));
						}
					}
					catch
					{
					}
				}
				cache[directoryName] = value;
			}
			foreach (string item2 in value)
			{
				if (!string.Equals(item2, goneName, StringComparison.OrdinalIgnoreCase))
				{
					return item2;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsUserFolderPath(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			return false;
		}
		string text = p.ToLowerInvariant();
		if (!text.Contains("\\users\\") && !text.Contains("\\downloads\\") && !text.Contains("\\temp\\") && !text.Contains("\\appdata\\"))
		{
			return text.Contains("\\desktop\\");
		}
		return true;
	}

	private IEnumerable<RadioButton> FindMenuRadioButtons()
	{
		return LogicalTreeHelperDescendants(this).OfType<RadioButton>();
	}

	private static IEnumerable<DependencyObject> LogicalTreeHelperDescendants(DependencyObject root)
	{
		foreach (DependencyObject child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
		{
			yield return child;
			foreach (DependencyObject item in LogicalTreeHelperDescendants(child))
			{
				yield return item;
			}
		}
	}

	private void Open_LastActivityView_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("LastActivityView.exe");
	}

	private void Open_JumpListsView_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("JumpListsView.exe");
	}

	private void Open_ShellBagsView_Click(object s, RoutedEventArgs e)
	{
		AppsLauncher.OpenFile("ShellBagsView.exe");
	}

	private async void StartRegistry_Click(object sender, RoutedEventArgs e)
	{
		StartRegistryBtn.Visibility = Visibility.Collapsed;
		RegistryProgressPanel.Visibility = Visibility.Visible;
		RegistryStatus.Text = Strings.Get("Status_Loading");
		try
		{
			List<RegistryItem> itemsSource = await NirSoftTables.LoadMuiCacheAsync();
			RegistryGrid.ItemsSource = itemsSource;
			RegistryBlur.Radius = 0.0;
			RegistryOverlay.Visibility = Visibility.Collapsed;
		}
		catch (Exception ex)
		{
			RegistryStatus.Text = Strings.Get("Msg_Error", ex.Message);
		}
	}

	private void RenamesSearch_Changed(object sender, TextChangedEventArgs e)
	{
		ApplyRenamesFilter();
	}

	private void RenamesNoise_Changed(object sender, RoutedEventArgs e)
	{
		if (!_renamesLoading && RenamesBox != null && RenamesBox.Visibility == Visibility.Visible)
		{
			LoadRenames_Click(sender, e);
		}
	}

	private void ApplyRenamesFilter()
	{
		if (_renamesView == null)
		{
			return;
		}
		string t = RenamesSearch.Text?.Trim().ToLowerInvariant() ?? "";
		if (string.IsNullOrEmpty(t))
		{
			_renamesView.Filter = null;
			return;
		}
		_renamesView.Filter = delegate(object obj)
		{
			if (!(obj is RenameEvent renameEvent))
			{
				return false;
			}
			return (renameEvent.OldName != null && renameEvent.OldName.ToLowerInvariant().Contains(t)) || (renameEvent.NewName != null && renameEvent.NewName.ToLowerInvariant().Contains(t)) || (renameEvent.CurrentPath != null && renameEvent.CurrentPath.ToLowerInvariant().Contains(t));
		};
	}

	private async void LoadRenames_Click(object sender, RoutedEventArgs e)
	{
		if (_renamesLoading)
		{
			return;
		}
		_renamesLoading = true;
		SoundService.Click();
		BtnRenames.IsEnabled = false;
		RenamesStatus.Text = "Чтение журнала NTFS…";
		try
		{
			bool includeNoise = RenamesNoise.IsChecked == true;
			List<RenameEvent> list = await Task.Run(delegate
			{
				List<RenameEvent> renames = UsnJournal.GetRenames(500, includeNoise);
				Parallel.ForEach(renames, new ParallelOptions
				{
					MaxDegreeOfParallelism = Environment.ProcessorCount
				}, delegate(RenameEvent ev)
				{
					ev.Icon = TryGetFileIcon(ev.CurrentPath, ev.NewName);
				});
				return renames;
			});
			_renamesView = CollectionViewSource.GetDefaultView(list);
			ApplyRenamesFilter();
			RenamesGrid.ItemsSource = _renamesView;
			RenamesBox.Visibility = ((list.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			RenamesStatus.Text = ((list.Count > 0) ? $"Найдено переименований: {list.Count}" : "Переименований не найдено (или нужны права администратора).");
		}
		catch (Exception ex)
		{
			RenamesStatus.Text = Strings.Get("Msg_Error", ex.Message);
		}
		finally
		{
			BtnRenames.IsEnabled = true;
			_renamesLoading = false;
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern nint SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyIcon(nint hIcon);

	private static ImageSource TryGetFileIcon(string path, string fallbackName = null)
	{
		try
		{
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				using Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
				if (icon != null)
				{
					return FreezeHIcon(icon.Handle, destroy: false);
				}
			}
			string extension = System.IO.Path.GetExtension((!string.IsNullOrEmpty(path)) ? path : fallbackName);
			if (!string.IsNullOrEmpty(extension))
			{
				SHFILEINFO psfi = default(SHFILEINFO);
				SHGetFileInfo("_" + extension, 128u, ref psfi, (uint)Marshal.SizeOf<SHFILEINFO>(), 272u);
				if (psfi.hIcon != IntPtr.Zero)
				{
					return FreezeHIcon(psfi.hIcon, destroy: true);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static ImageSource FreezeHIcon(nint hIcon, bool destroy)
	{
		try
		{
			BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			bitmapSource.Freeze();
			return bitmapSource;
		}
		finally
		{
			if (destroy)
			{
				DestroyIcon(hIcon);
			}
		}
	}

	private void RenamesGrid_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (!(RenamesGrid.SelectedItem is RenameEvent renameEvent) || string.IsNullOrEmpty(renameEvent.CurrentPath))
		{
			return;
		}
		try
		{
			if (File.Exists(renameEvent.CurrentPath))
			{
				Process.Start("explorer.exe", "/select,\"" + renameEvent.CurrentPath + "\"");
				return;
			}
			string directoryName = System.IO.Path.GetDirectoryName(renameEvent.CurrentPath);
			if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
			{
				Process.Start("explorer.exe", "\"" + directoryName + "\"");
			}
		}
		catch
		{
		}
	}

	private void OpenDataUsage_Click(object s, RoutedEventArgs e)
	{
		try
		{
			if (WindowsServices.IsStopped("DusmSvc") && AppDialog.Confirm(this, Strings.Get("DataUsage_Title"), Strings.Get("DataUsage_StartPrompt"), null, Strings.Get("Btn_Yes"), Strings.Get("Btn_No")))
			{
				WindowsServices.Start("DusmSvc");
			}
			Process.Start(new ProcessStartInfo("ms-settings:datausage")
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void OpenServices_Click(object s, RoutedEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("services.msc")
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void OpenKeyboard_Click(object s, RoutedEventArgs e)
	{
		string text = "C:\\Program Files\\Common Files\\Microsoft Shared\\ink\\TabTip.exe";
		string text2 = "C:\\Windows\\System32\\osk.exe";
		try
		{
			if (File.Exists(text))
			{
				Process.Start(text);
			}
			else if (File.Exists(text2))
			{
				Process.Start(text2);
			}
		}
		catch
		{
		}
	}

	private async void OpenNvidia_Click(object s, RoutedEventArgs e)
	{
		if (!(await Task.Run(() => TryLaunchNvidia())))
		{
			AppDialog.Alert(this, Strings.Get("Title_Nvidia"), Strings.Get("Msg_NvidiaNotFound"));
		}
	}

	private static bool TryLaunchNvidia()
	{
		RegistryHive[] array = new RegistryHive[2]
		{
			RegistryHive.LocalMachine,
			RegistryHive.CurrentUser
		};
		foreach (RegistryHive hKey in array)
		{
			try
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(hKey, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\nvcplui.exe");
				if (registryKey?.GetValue(null) is string path && File.Exists(path) && Run(path))
				{
					return true;
				}
			}
			catch
			{
			}
		}
		string[] array2 = new string[3] { "C:\\Program Files\\NVIDIA Corporation\\Control Panel Client\\nvcplui.exe", "C:\\Windows\\System32\\nvcplui.exe", "C:\\Windows\\SysWOW64\\nvcplui.exe" };
		foreach (string path2 in array2)
		{
			if (File.Exists(path2) && Run(path2))
			{
				return true;
			}
		}
		string text = ResolveNvidiaAumid();
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				Process.Start(new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\" + text)
				{
					UseShellExecute = true
				});
				return true;
			}
			catch
			{
			}
		}
		try
		{
			string path3 = "C:\\Program Files\\WindowsApps";
			if (Directory.Exists(path3))
			{
				foreach (string item in Directory.EnumerateDirectories(path3, "NVIDIACorp.NVIDIAControlPanel_*"))
				{
					string path4 = System.IO.Path.Combine(item, "nvcplui.exe");
					if (File.Exists(path4) && Run(path4))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return Run("nvcplui.exe");
		static bool Run(string fileName)
		{
			try
			{
				Process.Start(new ProcessStartInfo(fileName)
				{
					UseShellExecute = true
				});
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	private static string ResolveNvidiaAumid()
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string text = System.IO.Path.Combine(folderPath, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe");
			using Process process = Process.Start(new ProcessStartInfo(File.Exists(text) ? text : "powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Get-StartApps | Where-Object { $_.Name -like '*NVIDIA*Control*' -or $_.AppID -like '*NVIDIAControlPanel*' } | Select-Object -First 1 -ExpandProperty AppID\"")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return null;
			}
			string text2 = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(6000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return null;
			}
			text2 = text2?.Trim();
			return string.IsNullOrWhiteSpace(text2) ? null : text2.Split('\n')[0].Trim();
		}
		catch
		{
			return null;
		}
	}

	private async void DetectMouse_Click(object s, RoutedEventArgs e)
	{
		try
		{
			MouseAppFinder.MouseSoftware mouseSoftware = await Task.Run(() => MouseAppFinder.Find());
			if (mouseSoftware != null && !string.IsNullOrEmpty(mouseSoftware.Path))
			{
				Process.Start(new ProcessStartInfo(mouseSoftware.Path)
				{
					UseShellExecute = true
				});
			}
			else
			{
				AppDialog.Alert(this, Strings.Get("Title_MouseDetect"), Strings.Get("Mouse_NoApp"));
			}
		}
		catch
		{
			AppDialog.Alert(this, Strings.Get("Title_MouseDetect"), Strings.Get("Mouse_Error"), null, DialogKind.Danger);
		}
	}

	private void CheckMacros_Click(object s, RoutedEventArgs e)
	{
		CheckMacrosWindow checkMacrosWindow = new CheckMacrosWindow();
		checkMacrosWindow.Owner = this;
		checkMacrosWindow.ShowDialog();
	}

	private void AutoDetect_Click(object s, RoutedEventArgs e)
	{
		EmulateKeyboardWindow emulateKeyboardWindow = new EmulateKeyboardWindow();
		emulateKeyboardWindow.Owner = this;
		emulateKeyboardWindow.ShowDialog();
	}

}
