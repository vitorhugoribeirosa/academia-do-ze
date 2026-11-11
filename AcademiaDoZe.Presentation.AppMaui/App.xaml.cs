using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Presentation.AppMaui.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
        Resources["LocalizedStrings"] = LocalizationManager.Instance;
        AplicarTema();

        WeakReferenceMessenger.Default.Register<TemaPreferencesUpdatedMessage>(this, (_, _) =>
        {
            AplicarTema();
        });

        WeakReferenceMessenger.Default.Register<CulturaPreferencesUpdatedMessage>(this, (_, message) =>
        {
            LocalizationManager.Instance.SetCulture(message.Value);
            foreach (var window in Windows)
            {
                window.Title = LocalizationManager.Instance.TranslateSource("Academia do Zé");
#if WINDOWS || MACCATALYST
                if (window.TitleBar is TitleBar titleBar)
                {
                    titleBar.Title = LocalizationManager.Instance.TranslateSource("Academia do Zé");
                    titleBar.Subtitle = LocalizationManager.Instance.TranslateSource("Gestão administrativa");
                }
#endif
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Title = LocalizationManager.Instance.TranslateSource("Academia do Zé")
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
            Title = LocalizationManager.Instance.TranslateSource("Academia do Zé"),
            Subtitle = LocalizationManager.Instance.TranslateSource("Gestão administrativa"),
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
