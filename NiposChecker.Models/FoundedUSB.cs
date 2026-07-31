using System;
using System.Globalization;
using System.Windows.Media;

namespace NiposChecker.Models;

public class FoundedUSB
{
	private static readonly string[] _fmts = new string[2] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy H:mm:ss" };

	public ImageSource Icon { get; set; }

	public string Device_name { get; set; }

	public string Description { get; set; }

	public string Created_date { get; set; }

	public string DriverLetter { get; set; }

	public string Last_plug_unplug_date { get; set; }

	public string Vendorid { get; set; }

	public string Productid { get; set; }

	public bool IsLastDisconnected { get; set; }

	private DateTime? DisconnectDate
	{
		get
		{
			if (!DateTime.TryParseExact(Last_plug_unplug_date, _fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			{
				return null;
			}
			return result;
		}
	}

	public bool HasDisconnectDate => DisconnectDate.HasValue;

	public bool IsDisconnectedToday
	{
		get
		{
			DateTime? disconnectDate = DisconnectDate;
			if (disconnectDate.HasValue)
			{
				return disconnectDate.GetValueOrDefault().Date == DateTime.Now.Date;
			}
			return false;
		}
	}

	public Brush RowBar
	{
		get
		{
			if (!IsDisconnectedToday)
			{
				if (!IsLastDisconnected)
				{
					return Brushes.Transparent;
				}
				return new SolidColorBrush(Color.FromRgb(240, 169, 60));
			}
			return new SolidColorBrush(Color.FromRgb(byte.MaxValue, 40, 63));
		}
	}
}
