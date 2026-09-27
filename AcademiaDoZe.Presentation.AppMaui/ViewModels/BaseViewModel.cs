using CommunityToolkit.Mvvm.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    private string title = string.Empty;
    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }
}
