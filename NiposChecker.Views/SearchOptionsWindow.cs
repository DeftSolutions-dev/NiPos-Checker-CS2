using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NiposChecker.Localization;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class SearchOptionsWindow : Window
{
	public partial class DriveItem
	{
		public string Name { get; set; }

		public string Type { get; set; }

		public bool IsReady { get; set; }

		public string TotalSize { get; set; }

		public bool IsSelected { get; set; }

		public string DisplayText => $"{Name} [{Type}] {(IsReady ? TotalSize : "—")}";
	}























	public List<string> SelectedDirectories { get; private set; }

	public bool UseIconMatch => IconMatchCheck.IsChecked == true;

	public bool UseSignatureMatch => SignatureMatchCheck.IsChecked == true;

	public SearchOptionsWindow()
	{
		InitializeComponent();
		base.Title = Strings.Get("SO_WindowTitle");
		ModeTitle.Text = Strings.Get("SO_Title");
		FastText.Text = Strings.Get("SO_Fast");
		SysText.Text = Strings.Get("SO_SystemDrive");
		ManualText.Text = Strings.Get("SO_ManualDrives");
		BrowseText.Text = Strings.Get("SO_BrowseFolder");
		MethodsTitle.Text = Strings.Get("SO_Methods");
		IconMatchCheck.Content = Strings.Get("SO_ByIcon");
		SignatureMatchCheck.Content = Strings.Get("SO_BySignature");
		SelFoldersTitle.Text = Strings.Get("SO_SelectedFolders");
		DrivesTitle.Text = Strings.Get("SO_SelectDrives");
		CancelText.Text = Strings.Get("Btn_Cancel");
	}

	private void FastSearch_Click(object sender, RoutedEventArgs e)
	{
		List<string> dirs = new List<string>
		{
			KnownFolders.GetDownloads(),
			Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
			Environment.GetFolderPath(Environment.SpecialFolder.Personal),
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
		};
		ApplyDirectories(dirs);
		StatusLabel.Text = Strings.Get("SO_StatusFast");
		SetPicked(ModeFast);
	}

	private void Window_Drag(object sender, MouseButtonEventArgs e)
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

	private void SetPicked(System.Windows.Controls.Button picked)
	{
		System.Windows.Controls.Button[] array = new System.Windows.Controls.Button[4] { ModeFast, ModeSystem, ModeManual, ModeFolder };
		foreach (System.Windows.Controls.Button obj in array)
		{
			obj.Tag = ((obj == picked) ? "picked" : null);
		}
	}

	private void SystemSearch_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string pathRoot = Path.GetPathRoot(Environment.SystemDirectory);
			List<string> list = new List<string>();
			list.Add(pathRoot);
			foreach (string item in Directory.EnumerateDirectories(pathRoot))
			{
				string text = Path.GetFileName(item).ToLowerInvariant();
				switch (text)
				{
				case "windows":
				case "system volume information":
				case "recycler":
				case "$recycle.bin":
				case "boot":
				case "programdata":
					continue;
				}
				if (!text.StartsWith(".") && (File.GetAttributes(item) & FileAttributes.Hidden) != FileAttributes.Hidden)
				{
					list.Add(item);
				}
			}
			ApplyDirectories(list);
			StatusLabel.Text = Strings.Get("SO_StatusSystem");
			SetPicked(ModeSystem);
		}
		catch (Exception ex)
		{
			AppDialog.Alert(this, Strings.Get("Title_Error"), Strings.Get("SO_DriveScanError", ex.Message), null, DialogKind.Danger);
		}
	}

	private void ManualSearch_Click(object sender, RoutedEventArgs e)
	{
		DrivePanel.Visibility = Visibility.Visible;
		List<DriveItem> itemsSource = (from d in DriveInfo.GetDrives()
			select new DriveItem
			{
				Name = d.Name,
				Type = d.DriveType.ToString(),
				IsReady = d.IsReady,
				TotalSize = (d.IsReady ? $"{d.TotalSize / 1024 / 1024 / 1024:F0} GB" : "—"),
				IsSelected = false
			}).ToList();
		DriveList.ItemsSource = itemsSource;
		StatusLabel.Text = Strings.Get("SO_StatusDrives");
		SetPicked(ModeManual);
	}

	private void ApplyDirectories(List<string> dirs)
	{
		SelectedDirectories = dirs;
		SelectedFoldersList.ItemsSource = dirs.Select((string d) => new TextBlock
		{
			Text = "\ud83d\udcc1  " + d,
			Foreground = (Brush)FindResource("Brush_Dim"),
			FontFamily = (FontFamily)FindResource("Font_Mono"),
			FontSize = 11.5,
			Margin = new Thickness(0.0, 2.0, 0.0, 2.0),
			TextTrimming = TextTrimming.CharacterEllipsis
		}).ToList();
		FoldersPlaceholder.Visibility = ((dirs.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
		OkBtn.IsEnabled = dirs.Count > 0;
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		if (DrivePanel.Visibility == Visibility.Visible && DriveList.ItemsSource != null)
		{
			List<DriveItem> list = ((IEnumerable<DriveItem>)DriveList.ItemsSource).Where((DriveItem d) => d.IsSelected && d.IsReady).ToList();
			if (list.Count > 0)
			{
				List<string> list2 = new List<string>();
				foreach (DriveItem item in list)
				{
					list2.Add(item.Name);
				}
				ApplyDirectories(list2);
			}
		}
		if (SelectedDirectories == null || SelectedDirectories.Count == 0)
		{
			AppDialog.Alert(this, Strings.Get("Title_Error"), Strings.Get("SO_NoFolders"), null, DialogKind.Warning);
			return;
		}
		base.DialogResult = true;
		Close();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void BrowseFolder_Click(object sender, RoutedEventArgs e)
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		folderBrowserDialog.Description = Strings.Get("SO_PickFolder");
		if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			ApplyDirectories(new List<string> { folderBrowserDialog.SelectedPath });
			StatusLabel.Text = Strings.Get("SO_StatusFolder");
			SetPicked(ModeFolder);
		}
	}

}
