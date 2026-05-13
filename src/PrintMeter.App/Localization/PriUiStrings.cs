using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using PrintMeter.App.ViewModels;

namespace PrintMeter.App.Localization;

public sealed class PriUiStrings : IUiStrings
{
    private readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse();

    public string Format(string resourceKey, params object[] args)
    {
        var template = _loader.GetString(resourceKey);
        if (string.IsNullOrEmpty(template))
        {
            return resourceKey;
        }

        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentUICulture, template, args);
    }
}
