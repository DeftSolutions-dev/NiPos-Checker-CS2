using System.Reflection;

namespace NiposChecker;

public static class Config
{
	public static readonly string[] ApiUrls = new string[2] { "https://api.niposproject.ru/", "https://api2.niposproject.ru/" };

	private static volatile string _activeApiUrl = ApiUrls[0];

	private const string DefaultClientKey = "ZKkHMZISvgMEICS";

	public static readonly string ClientKey = ResolveClientKey();

	public const uint AppId = 730u;

	public const int AppIdInt = 730;

	public const int ProjectId = 41;

	public const string Version = "1.1";

	public const long MaxScanFileSize = 62914560L;

	public static readonly string[] CertPinsSha256 = new string[4] { "XdV+OXfztQUvKnPde9YeniQKj3EZaosuXXzblapIApI=", "LoMHBotttiDko50Gi13uXW71eIy7LAttI+rYT8wXF4w=", "s/tdAOmUzd8syaTuqfgGvFcn6DzA5Cmb+Vby1ST+U3Y=", "17TvrvSjkxX47EqqCUVM17JxxJXJ7nx9fSuscjpgYro=" };

	public static string ApiUrl => _activeApiUrl;

	public static void SetActiveApiUrl(string url)
	{
		if (!string.IsNullOrWhiteSpace(url))
		{
			_activeApiUrl = url;
		}
	}

	private static string ResolveClientKey()
	{
		try
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyMetadataAttribute), inherit: false);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				if (customAttributes[i] is AssemblyMetadataAttribute { Key: "ClientKey" } assemblyMetadataAttribute && !string.IsNullOrWhiteSpace(assemblyMetadataAttribute.Value))
				{
					return assemblyMetadataAttribute.Value.Trim();
				}
			}
		}
		catch
		{
		}
		return "ZKkHMZISvgMEICS";
	}
}
