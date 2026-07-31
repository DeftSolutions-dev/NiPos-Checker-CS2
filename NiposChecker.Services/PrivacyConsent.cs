using System;
using System.IO;
using System.Windows;
using NiposChecker.Views;

namespace NiposChecker.Services;

public static class PrivacyConsent
{
	private static bool? _cached;

	private static string FlagPath => Path.Combine(AppContext.BaseDirectory, "consent.cfg");

	public static bool Granted
	{
		get
		{
			if (_cached.HasValue)
			{
				return _cached.Value;
			}
			try
			{
				_cached = File.Exists(FlagPath);
			}
			catch
			{
				_cached = false;
			}
			return _cached.Value;
		}
	}

	public static bool Ensure(Window owner)
	{
		if (Granted)
		{
			return true;
		}
		bool flag = AppDialog.Confirm(owner, "NIPOS CHECKER · согласие на проверку", "Это добровольная проверка ПК по запросу администрации сервера.", "Во время проверки на сервер проекта отправляется отчёт: аппаратный идентификатор (HWID), Steam-аккаунты, сведения о системе (ОС, железо), найденные признаки и IP подключения. Данные используются только для проверки на запрещённое ПО.\n\nПродолжая, вы соглашаетесь на эту обработку. Нажмите «Отмена», чтобы выйти.", "Согласен, продолжить", "Отмена", DialogKind.Info);
		if (flag)
		{
			try
			{
				File.WriteAllText(FlagPath, DateTimeOffset.Now.ToString("o"));
			}
			catch
			{
			}
			_cached = true;
		}
		return flag;
	}
}
