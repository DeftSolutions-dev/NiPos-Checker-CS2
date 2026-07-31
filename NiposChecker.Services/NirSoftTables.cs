using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class NirSoftTables
{
	public static async Task<List<FoundedUSB>> LoadUsbAsync()
	{
		XDocument xDocument = await AppsLauncher.RunSxmlAsync("USBDeview.exe", "NiposChecker_USB.xml");
		if (xDocument == null)
		{
			return new List<FoundedUSB>();
		}
		List<FoundedUSB> list = new List<FoundedUSB>();
		foreach (XElement item in xDocument.Descendants("item"))
		{
			list.Add(new FoundedUSB
			{
				Device_name = Val(item, "description"),
				Description = Val(item, "device_type"),
				Created_date = Val(item, "connect_time"),
				Last_plug_unplug_date = Val(item, "disconnect_time"),
				DriverLetter = Val(item, "drive_letter"),
				Vendorid = Val(item, "vendorid"),
				Productid = Val(item, "productid")
			});
		}
		string[] formats = new string[2] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy H:mm:ss" };
		DateTime result;
		return list.OrderByDescending((FoundedUSB x) => (!DateTime.TryParseExact(x.Last_plug_unplug_date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) ? DateTime.MinValue : result).ToList();
	}

	public static async Task<List<ActivityItem>> LoadLastActivityAsync()
	{
		XDocument xDocument = await AppsLauncher.RunSxmlAsync("LastActivityView.exe", "NiposChecker_LastActivity.xml");
		if (xDocument == null)
		{
			return new List<ActivityItem>();
		}
		List<ActivityItem> list = new List<ActivityItem>();
		foreach (XElement item in xDocument.Descendants("item"))
		{
			string text = Val(item, "filename");
			string path = Val(item, "full_path");
			string text2 = Val(item, "action_time");
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = Val(item, "modified_time");
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = Val(item, "description");
			}
			list.Add(new ActivityItem
			{
				Name = text,
				Path = path,
				Date = text2,
				Extension = Path.GetExtension(path).ToLowerInvariant()
			});
		}
		string[] formats = new string[4] { "dd/MM/yyyy HH:mm:ss", "dd.MM.yyyy HH:mm:ss", "M/d/yyyy h:mm:ss tt", "d.M.yyyy H:mm:ss" };
		DateTime result;
		return list.OrderByDescending((ActivityItem x) => (!DateTime.TryParseExact(x.Date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) ? DateTime.MinValue : result).ToList();
	}

	public static async Task<List<ActivityItem>> LoadExecutedProgramsAsync()
	{
		XDocument xDocument = await AppsLauncher.RunSxmlAsync("ExecutedProgramsList.exe", "NiposChecker_Executed.xml");
		if (xDocument == null)
		{
			return new List<ActivityItem>();
		}
		List<ActivityItem> list = new List<ActivityItem>();
		foreach (XElement item in xDocument.Descendants("item"))
		{
			string text = Val(item, "executed_file");
			string date = Val(item, "last_executed_on");
			list.Add(new ActivityItem
			{
				Name = Path.GetFileName((text == "—") ? "" : text),
				Path = text,
				Date = date,
				Extension = Path.GetExtension(text).ToLowerInvariant()
			});
		}
		string[] formats = new string[4] { "dd/MM/yyyy HH:mm:ss", "dd.MM.yyyy HH:mm:ss", "M/d/yyyy h:mm:ss tt", "d.M.yyyy H:mm:ss" };
		DateTime result;
		return list.OrderByDescending((ActivityItem x) => (!DateTime.TryParseExact(x.Date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) ? DateTime.MinValue : result).ToList();
	}

	public static async Task<List<RegistryItem>> LoadMuiCacheAsync()
	{
		XDocument xDocument = await AppsLauncher.RunSxmlAsync("MUICacheView.exe", "NiposChecker_MUICache.xml");
		if (xDocument == null)
		{
			return new List<RegistryItem>();
		}
		List<RegistryItem> list = new List<RegistryItem>();
		foreach (XElement item in xDocument.Descendants("item"))
		{
			string name = Val(item, "application_name").Replace(".FriendlyAppName", "");
			string path = Val(item, "application_path").Replace(".ApplicationCompany", "");
			list.Add(new RegistryItem
			{
				Name = name,
				Path = path
			});
		}
		return list;
	}

	private static string Val(XElement el, string tag)
	{
		string text = el.Element(tag)?.Value;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "—";
	}
}
