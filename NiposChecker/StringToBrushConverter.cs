using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NiposChecker;

public class StringToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && text.StartsWith("#"))
		{
			try
			{
				return new SolidColorBrush((Color)ColorConverter.ConvertFromString(text));
			}
			catch
			{
			}
		}
		return new SolidColorBrush(Colors.White);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
