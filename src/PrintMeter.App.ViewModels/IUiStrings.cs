namespace PrintMeter.App.ViewModels;

/// <summary>Форматированные строки UI (MRT/RESW в приложении, заглушка в тестах).</summary>
public interface IUiStrings
{
    string Format(string resourceKey, params object[] args);
}
