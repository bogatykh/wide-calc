using CommunityToolkit.Mvvm.ComponentModel;

namespace PrintMeter.App.ViewModels;

public sealed partial class FormatToggleRowViewModel : ObservableObject
{
    public FormatToggleRowViewModel(string key, string label, double iconSize, string? tooltip = null)
    {
        Key = key;
        Label = label;
        IconSize = iconSize;
        Tooltip = tooltip;
    }

    public string Key { get; }

    public string Label { get; }

    public double IconSize { get; }

    public string? Tooltip { get; }

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _statText = "—";
}
