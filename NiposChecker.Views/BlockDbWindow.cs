using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NiposChecker.Localization;
using NiposChecker.Models;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class BlockDbWindow : Window
{
	public partial class AccountResult
	{
		public string Nickname { get; set; }

		public string SteamID64 { get; set; }

		public string StatusIcon { get; set; } = "⚪";

		public string BanSummary { get; set; } = "";

		public bool IsError { get; set; }

		public bool HasActiveBan { get; set; }

		public List<BanInfo> Bans { get; set; } = new List<BanInfo>();

		public bool HasBan
		{
			get
			{
				if (Bans != null)
				{
					return Bans.Count > 0;
				}
				return false;
			}
		}

		public Brush DotBrush
		{
			get
			{
				if (!HasActiveBan)
				{
					if (!IsError)
					{
						return new SolidColorBrush(Color.FromRgb(52, 211, 153));
					}
					return new SolidColorBrush(Color.FromRgb(55, 43, 55));
				}
				return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 40, 63));
			}
		}

		public Brush SummaryBrush
		{
			get
			{
				if (!HasActiveBan)
				{
					return new SolidColorBrush(Color.FromRgb(94, 79, 91));
				}
				return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 128, 134));
			}
		}
	}

	private readonly List<SteamAccount> _accounts;

	private readonly ApiClient _api;

	private readonly bool _byIp;

	private readonly string _ip;

	private readonly string _project;

	private readonly ObservableCollection<AccountResult> _results = new ObservableCollection<AccountResult>();











	public BlockDbWindow(List<SteamAccount> accounts, ApiClient api, bool byIp = false, string ip = null, string projectName = null)
	{
		InitializeComponent();
		_project = projectName;
		base.Title = Strings.Get("BlockDb_WindowTitle");
		HeaderTitle.Text = Strings.Get("BlockDb_WindowTitle");
		AccountsHeader.Text = Strings.Get("BlockDb_Accounts");
		ProgressLabel.Text = Strings.Get("BlockDb_Waiting");
		BanDetailHeader.Text = Strings.Get("BlockDb_Details");
		CloseBtn.Content = Strings.Get("BlockDb_CloseBtn");
		_accounts = accounts;
		_api = api;
		_byIp = byIp;
		_ip = ip;
		AccountsList.ItemsSource = _results;
		base.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_byIp)
		{
			await CheckByIpAsync();
		}
		else
		{
			await CheckBySteamIdsAsync();
		}
	}

	private async Task CheckBySteamIdsAsync()
	{
		BlockDbService svc = new BlockDbService(_api);
		int total = _accounts?.Count ?? 0;
		int done = 0;
		bool anyBan = false;
		ProgressLabel.Text = Strings.Get("BlockDb_CheckingAccounts");
		foreach (SteamAccount acc in _accounts ?? new List<SteamAccount>())
		{
			done++;
			ProgressCount.Text = $"{done}/{total}";
			ProgressLabel.Text = acc.Nickname ?? acc.SteamID64 ?? "—";
			AccountResult result = new AccountResult
			{
				Nickname = (acc.Nickname ?? "—"),
				SteamID64 = (acc.SteamID64 ?? "—")
			};
			try
			{
				List<BanInfo> list = (result.Bans = await svc.CheckSteamIdAsync(acc.SteamID64));
				int num = BlockDbService.ActiveProjectBanCount(list, _project);
				int num2 = list?.Count ?? 0;
				if (num > 0)
				{
					anyBan = true;
					acc.isBannedOnProject = true;
					result.HasActiveBan = true;
					result.BanSummary = Strings.Get("BlockDb_ProjectBan", num);
				}
				else if (num2 > 0)
				{
					result.BanSummary = Strings.Get("BlockDb_HistoryOnly", num2);
				}
				else
				{
					result.BanSummary = Strings.Get("BlockDb_Clean");
				}
			}
			catch
			{
				result.IsError = true;
				result.BanSummary = Strings.Get("BlockDb_ErrorShort");
			}
			_results.Add(result);
			List<BanInfo> bans = result.Bans;
			if (bans != null && bans.Count > 0 && BansGrid.ItemsSource == null)
			{
				BansGrid.ItemsSource = result.Bans;
				NoBansLabel.Visibility = Visibility.Collapsed;
				AccountsList.SelectedItem = result;
			}
		}
		ProgressLabel.Text = Strings.Get("BlockDb_Done");
		if (!anyBan)
		{
			if (_results.Count > 0)
			{
				AccountsList.SelectedItem = _results[0];
			}
			NoBansLabel.Visibility = Visibility.Visible;
		}
	}

	private async Task CheckByIpAsync()
	{
		BlockDbService blockDbService = new BlockDbService(_api);
		ProgressLabel.Text = Strings.Get("BlockDb_CheckingIp", _ip);
		ProgressCount.Text = "1/1";
		AccountResult result = new AccountResult
		{
			Nickname = _ip,
			SteamID64 = Strings.Get("BlockDb_IpRequest")
		};
		try
		{
			List<BanInfo> list = (result.Bans = await blockDbService.CheckIpAsync(_ip));
			int num = BlockDbService.ActiveProjectBanCount(list, _project);
			int num2 = list?.Count ?? 0;
			if (num2 > 0)
			{
				result.HasActiveBan = num > 0;
				result.BanSummary = ((num > 0) ? Strings.Get("BlockDb_ProjectBan", num) : Strings.Get("BlockDb_HistoryOnly", num2));
				BansGrid.ItemsSource = list;
				NoBansLabel.Visibility = Visibility.Collapsed;
			}
			else
			{
				result.BanSummary = Strings.Get("BlockDb_Clean");
				NoBansLabel.Visibility = Visibility.Visible;
			}
		}
		catch
		{
			result.IsError = true;
			result.BanSummary = Strings.Get("BlockDb_ErrorShort");
		}
		_results.Add(result);
		AccountsList.SelectedItem = result;
		ProgressLabel.Text = Strings.Get("BlockDb_Done");
	}

	private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (AccountsList.SelectedItem is AccountResult accountResult)
		{
			BansGrid.ItemsSource = accountResult.Bans ?? new List<BanInfo>();
			BanDetailHeader.Text = Strings.Get("BlockDb_DetailsFor", accountResult.Nickname);
			NoBansLabel.Visibility = ((accountResult.Bans != null && accountResult.Bans.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
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

}
