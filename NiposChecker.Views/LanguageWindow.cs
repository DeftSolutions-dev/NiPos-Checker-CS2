using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using NiposChecker.Localization;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class LanguageWindow : Window
{






	public LanguageWindow()
	{
		InitializeComponent();
		UpdateTexts();
	}

	private void UpdateTexts()
	{
		base.Title = Strings.Get("Lang_WindowTitle");
		HeaderTitle.Text = Strings.Get("Lang_WindowTitle");
		RuText.Text = Strings.Get("Lang_Russian");
		EnText.Text = Strings.Get("Lang_English");
		RuBtn.Tag = ((Strings.Current == "RU") ? "active" : null);
		EnBtn.Tag = ((Strings.Current == "EN") ? "active" : null);
	}

	private void Ru_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		Strings.SetLanguage("RU");
		UpdateTexts();
	}

	private void En_Click(object sender, RoutedEventArgs e)
	{
		SoundService.Click();
		Strings.SetLanguage("EN");
		UpdateTexts();
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
