using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PrintMeter.App.ViewModels;

public sealed partial class FileReportRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    partial void OnFilePathChanged(string value) => OnPropertyChanged(nameof(FileDisplayName));

    public string FileDisplayName =>
        string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private double _totalLengthMeters;

    [ObservableProperty]
    private string _formatsSummary = string.Empty;

    [ObservableProperty]
    private string? _issueText;
}
