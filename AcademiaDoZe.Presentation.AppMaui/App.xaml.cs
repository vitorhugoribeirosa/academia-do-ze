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
        var window = new Window(new AppShell())
        {
            Title = "Academia do Zé"
        };

#if WINDOWS || MACCATALYST
        window.TitleBar = CriarBarraDeTitulo();
#endif

        return window;
    }

#if WINDOWS || MACCATALYST
    private static TitleBar CriarBarraDeTitulo()
    {
        return new TitleBar
        {
            Title = "Academia do Zé",
            Subtitle = "Gestão administrativa",
            BackgroundColor = Color.FromArgb("#24211D"),
            ForegroundColor = Colors.White,
            HeightRequest = 48
        };
    }
#endif

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
