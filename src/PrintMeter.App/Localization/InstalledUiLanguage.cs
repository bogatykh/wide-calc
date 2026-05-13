using System.Globalization;
using Microsoft.Win32;
using Microsoft.Windows.Globalization;

namespace PrintMeter.App.Localization;

internal static class InstalledUiLanguage
{
    private const string RegistrySubKey = @"SOFTWARE\PrintMeter";
    private const string ValueName = "UiLanguage";

    /// <summary>
    /// Читает язык, записанный установщиком (english / russian), и задаёт язык UI по умолчанию en-US.
    /// Вызывать до загрузки ресурсов UI.
    /// </summary>
    public static void ApplyPreferredFromRegistry()
    {
        var bcp47 = "en-US";
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(RegistrySubKey, writable: false);
            var raw = k?.GetValue(ValueName)?.ToString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (raw.Equals("russian", StringComparison.OrdinalIgnoreCase))
                {
                    bcp47 = "ru-RU";
                }
                else if (raw.Equals("english", StringComparison.OrdinalIgnoreCase))
                {
                    bcp47 = "en-US";
                }
            }
        }
        catch
        {
            // нет прав на чтение HKLM — остаётся en-US
        }

        ApplicationLanguages.PrimaryLanguageOverride = bcp47;
        var ci = CultureInfo.GetCultureInfo(bcp47);
        CultureInfo.DefaultThreadCurrentUICulture = ci;
        CultureInfo.DefaultThreadCurrentCulture = ci;
    }
}
