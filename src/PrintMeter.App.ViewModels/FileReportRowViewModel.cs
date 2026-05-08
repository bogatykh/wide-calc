using CommunityToolkit.Mvvm.ComponentModel;

namespace PrintMeter.App.ViewModels;

public sealed partial class FileReportRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private double _totalLengthMeters;

    [ObservableProperty]
    private string _formatsSummary = string.Empty;

    [ObservableProperty]
    private string? _issueText;
}
