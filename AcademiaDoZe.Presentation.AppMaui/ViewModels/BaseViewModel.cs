using CommunityToolkit.Mvvm.ComponentModel;
using AcademiaDoZe.Presentation.AppMaui.Helpers;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    protected BaseViewModel()
    {
        LocalizationManager.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }
    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    private string titleKey = string.Empty;
    public string Title
    {
        get => LocalizationManager.Instance.TranslateSource(titleKey);
        set => SetProperty(ref titleKey, value);
    }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }
}
