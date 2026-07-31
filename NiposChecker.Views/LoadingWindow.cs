using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using NiposChecker.Localization;
using NiposChecker.Models;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class LoadingWindow : Window
{
	private readonly ApiClient _api;

	private readonly CheatDatabase _cheatDb;

	private ProjectInfo _projectInfo;

	private BitmapImage _mainLogo;









	public LoadingWindow()
	{
		InitializeComponent();
		_api = new ApiClient();
		_cheatDb = new CheatDatabase();
		base.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_ = 5;
		try
		{
			if (!(await HasInternetAsync()))
			{
				AppDialog.Alert(this, "NIPOS CHECKER", Strings.Get("Msg_NoInternet"), null, DialogKind.Danger);
				Application.Current.Shutdown();
				return;
			}
			if (!PrivacyConsent.Ensure(this))
			{
				Application.Current.Shutdown();
				return;
			}
			SetStatus(Strings.Get("Load_GettingInfo"), 10);
			_projectInfo = await _api.GetProjectInfoAsync();
			if (_projectInfo.IsSoftwareBanned)
			{
				ShowBannedWindow(_projectInfo.SoftwareBanReason ?? Strings.Get("Load_Blocked"));
				return;
			}
			VersionText.Text = "1.1";
			if (!string.IsNullOrWhiteSpace(_projectInfo.CheckerName))
			{
				base.Title = _projectInfo.CheckerName;
			}
			try
			{
				Branding.ApplyBrandColor(_projectInfo.BrandColor);
			}
			catch
			{
			}
			await LoadBrandingAsync();
			string text = (_projectInfo.LastVersion ?? "").Trim();
			if (IsNewerVersion(text, "1.1"))
			{
				string text2 = ((!string.IsNullOrEmpty(_projectInfo.UpdateUrl)) ? _projectInfo.UpdateUrl : _projectInfo.LinkToWebsite);
				if (Safety.IsSafeWebUrl(text2) && AppDialog.Confirm(this, "NIPOS CHECKER", string.Format(Strings.Get("Update_Available"), text, "1.1"), null, Strings.Get("Btn_Yes"), Strings.Get("Btn_No"), DialogKind.Info))
				{
					Process.Start(new ProcessStartInfo(text2)
					{
						UseShellExecute = true
					});
				}
			}
			SetStatus(Strings.Get("Load_CheatDB"), 35);
			await _cheatDb.LoadAsync(_api);
			SetStatus(Strings.Get("Load_SteamInit"), 55);
			if (SteamworksService.Init())
			{
				App.CurrentSteamID = SteamworksService.GetCurrentSteamID();
				App.GameBuyDateUnix = SteamworksService.GetEarliestPurchaseUnixTime();
				SteamworksService.Shutdown();
			}
			SetStatus(Strings.Get("Load_SteamAccounts"), 70);
			List<Account> allAccounts = SteamLocal.GetAllAccounts();
			try
			{
				foreach (string item in SteamCleaningDetector.CollectHiddenSteamIds(allAccounts.Select((Account a) => a.SteamId).ToList()))
				{
					allAccounts.Add(new Account(item, "0"));
				}
			}
			catch
			{
			}
			App.LocalAccounts = allAccounts;
			SetStatus(Strings.Get("Load_Profiles"), 85);
			List<SteamAccount> accountInfos = await ResolveAccountsAsync(allAccounts);
			SetStatus(Strings.Get("Load_HWID"), 95);
			string hwid = (App.HWID = HwId.Generate());
			string text4 = await _api.CheckHwidBanAsync(hwid);
			if (text4 != null)
			{
				ShowBannedWindow(text4);
				return;
			}
			SetStatus(Strings.Get("Load_Done"), 100);
			App.CheatDatabase = _cheatDb;
			try
			{
				DiscordService.Initialize(_projectInfo);
			}
			catch
			{
			}
			MainWindow mainWindow = new MainWindow(_projectInfo, accountInfos, _api, _cheatDb, _mainLogo);
			mainWindow.Show();
			Application.Current.MainWindow = mainWindow;
			Close();
		}
		catch (Exception ex)
		{
			string text5 = ex.ToString();
			try
			{
				string path = Path.Combine(AppContext.BaseDirectory, "error.log");
				if (File.Exists(path))
				{
					text5 = text5 + "\n\n--- error.log ---\n" + File.ReadAllText(path);
				}
			}
			catch
			{
			}
			AppDialog.Alert(this, "NIPOS CHECKER", Strings.Get("Load_Error"), ex.Message + "\n\n" + text5, DialogKind.Danger);
			Application.Current.Shutdown();
		}
	}

	private static bool IsNewerVersion(string server, string client)
	{
		if (string.IsNullOrWhiteSpace(server))
		{
			return false;
		}
		if (Version.TryParse(server.Trim(), out Version result) && Version.TryParse((client ?? "").Trim(), out Version result2))
		{
			return result > result2;
		}
		return false;
	}

	private static async Task<bool> HasInternetAsync()
	{
		if (!NetworkInterface.GetIsNetworkAvailable())
		{
			return false;
		}
		try
		{
			using Ping ping = new Ping();
			if ((await ping.SendPingAsync("8.8.8.8", 2000)).Status == IPStatus.Success)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			using HttpClient http = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(4.0)
			};
			using (await http.GetAsync(Config.ApiUrl))
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (ProgressFill != null)
		{
			ProgressFill.Width = Math.Max(0.0, Math.Min(1.0, e.NewValue / 100.0)) * 360.0;
		}
	}

	private void SetStatus(string text, int progress)
	{
		StatusText.Text = text;
		PercentText.Text = progress + "%";
		DoubleAnimation animation = new DoubleAnimation(ProgressBar.Value, progress, TimeSpan.FromMilliseconds(250.0));
		ProgressBar.BeginAnimation(RangeBase.ValueProperty, animation);
	}

	private async Task LoadBrandingAsync()
	{
		if (_projectInfo == null)
		{
			return;
		}
		string text = ((!string.IsNullOrEmpty(_projectInfo.WhiteLogoUrl)) ? _projectInfo.WhiteLogoUrl : _projectInfo.MainLogoUrl);
		if (!string.IsNullOrEmpty(text))
		{
			BitmapImage bitmapImage = await ImageLoader.DownloadAsync(text);
			if (bitmapImage != null)
			{
				SplashLogo.Source = bitmapImage;
				SplashLogo.Visibility = Visibility.Visible;
				SplashSpinner.Visibility = Visibility.Collapsed;
			}
		}
		if (!string.IsNullOrEmpty(_projectInfo.SoftwareIcon))
		{
			BitmapImage bitmapImage2 = await ImageLoader.DownloadAsync(_projectInfo.SoftwareIcon);
			if (bitmapImage2 != null)
			{
				base.Icon = bitmapImage2;
			}
		}
		_mainLogo = await ImageLoader.DownloadAsync((!string.IsNullOrEmpty(_projectInfo.MainLogoUrl)) ? _projectInfo.MainLogoUrl : _projectInfo.WhiteLogoUrl);
		if (_mainLogo == null)
		{
			_mainLogo = SplashLogo.Source as BitmapImage;
		}
	}

	private async Task<List<SteamAccount>> ResolveAccountsAsync(List<Account> accounts)
	{
		List<SteamAccount> result = new List<SteamAccount>();
		if (accounts.Count == 0)
		{
			return result;
		}
		string value = string.Join(",", accounts.Select((Account a) => a.SteamId));
		Dictionary<string, string> extraData = new Dictionary<string, string>
		{
			["steamids"] = value,
			["checkonbanproject"] = "no"
		};
		JObject jObject = await _api.CallAsync("getsteaminfo", extraData);
		if (jObject == null)
		{
			return result;
		}
		foreach (KeyValuePair<string, JToken> item2 in jObject)
		{
			try
			{
				SteamProfileJson profile = item2.Value.ToObject<SteamProfileJson>();
				if (profile != null)
				{
					string lastActivity = accounts.FirstOrDefault((Account a) => a.SteamId == profile.SteamId)?.Timestamp ?? "0";
					bool isCurrent = profile.SteamId == App.CurrentSteamID;
					SteamAccount item = new SteamAccount
					{
						SteamID64 = profile.SteamId,
						Nickname = (profile.PersonaName ?? "—"),
						Vac_Ban = profile.VacBanned,
						Avatar = (profile.AvatarString ?? profile.Avatar),
						RegistrationDate = ((profile.TimeCreated > 0) ? DateTimeOffset.FromUnixTimeSeconds(profile.TimeCreated).LocalDateTime.ToString("dd.MM.yyyy") : "—"),
						ProfileType = profile.CommunityVisibilityState.ToString(),
						RealName = (string.IsNullOrEmpty(profile.RealName) ? "—" : profile.RealName),
						LastActivity = lastActivity,
						Lvl = profile.Level.GetValueOrDefault(),
						VAC_SinceBan = profile.DaysSinceBan.GetValueOrDefault(),
						IsCurrent = isCurrent,
						LevelColorHex = SteamworksService.GetLevelColorHex(profile.Level.GetValueOrDefault())
					};
					result.Add(item);
				}
			}
			catch
			{
			}
		}
		return result;
	}

	private void ShowBannedWindow(string reason)
	{
		new UserBannedWindow(reason).Show();
		Close();
	}

}
