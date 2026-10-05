using AcademiaDoZe.Presentation.AppMaui.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
        AplicarTema();

        WeakReferenceMessenger.Default.Register<TemaPreferencesUpdatedMessage>(this, (_, _) =>
        {
            AplicarTema();
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Academia do Zé"
        };
    }

    private void AplicarTema()
    {
        UserAppTheme = Preferences.Default.Get("Tema", "system") switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
