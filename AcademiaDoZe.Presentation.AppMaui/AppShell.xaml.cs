using AcademiaDoZe.Presentation.AppMaui.Views;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("logradouro", typeof(LogradouroPage));
    }
}
