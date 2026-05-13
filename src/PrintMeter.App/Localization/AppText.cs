using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PrintMeter.App.Localization;

/// <summary>Строки до построения DI (Program, ранние ошибки).</summary>
internal static class AppText
{
    private static ResourceLoader? _loader;

    private static ResourceLoader Loader => _loader ??= new ResourceLoader();

    public static string Get(string key) => Loader.GetString(key) ?? key;

    public static string Format(string key, params object[] args)
    {
        var template = Get(key);
        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentUICulture, template, args);
    }
}
