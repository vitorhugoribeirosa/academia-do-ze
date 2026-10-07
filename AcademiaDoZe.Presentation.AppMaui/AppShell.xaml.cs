using AcademiaDoZe.Presentation.AppMaui.Views;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class AppShell : Shell
{
    private const string LogoResourceName =
        "AcademiaDoZe.Presentation.AppMaui.Resources.Images.academiadoze.png";

    public ImageSource MenuLogoImage { get; } = ImageSource.FromStream(() =>
        typeof(AppShell).Assembly.GetManifestResourceStream(LogoResourceName)
        ?? throw new InvalidOperationException("Imagem da Academia do Zé não encontrada no aplicativo."));

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("colaborador", typeof(ColaboradorPage));
        Routing.RegisterRoute("logradouro", typeof(LogradouroPage));
    }
}
