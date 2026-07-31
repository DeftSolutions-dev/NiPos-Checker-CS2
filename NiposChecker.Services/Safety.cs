using System;

namespace NiposChecker.Services;

public static class Safety
{
	public static bool IsSafeWebUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri result))
		{
			return false;
		}
		if (!(result.Scheme == Uri.UriSchemeHttp))
		{
			return result.Scheme == Uri.UriSchemeHttps;
		}
		return true;
	}
}
