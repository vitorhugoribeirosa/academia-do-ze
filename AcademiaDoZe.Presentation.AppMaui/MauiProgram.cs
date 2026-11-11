using AcademiaDoZe.Presentation.AppMaui.Configuration;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;
using AcademiaDoZe.Presentation.AppMaui.Views;
#if WINDOWS
using CommunityToolkit.Maui;
#endif
using Microsoft.Extensions.Logging;
#if WINDOWS
using Microsoft.Maui.Handlers;
#endif

namespace AcademiaDoZe.Presentation.AppMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var savedCulture = Preferences.Default.Get(
            "Cultura",
            LocalizationManager.NormalizeCulture(System.Globalization.CultureInfo.InstalledUICulture.Name));
        LocalizationManager.Instance.SetCulture(savedCulture, persist: false);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

#if WINDOWS
        builder.UseMauiCommunityToolkitCamera();

        // FormEntry already sits inside our own Border. Remove the native
        // Windows TextBox chrome so it does not render a second raised box
        // or the default blue focus underline inside the designed field.
        EntryHandler.Mapper.AppendToMapping("FlatFormEntry", (handler, entry) =>
        {
            if (entry is not Entry formEntry || formEntry.ClassId != "FlatFormEntry")
            {
                return;
            }

            handler.PlatformView.Background =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(0);

            var transparentBrush =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var noBorder = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Resources["TextControlBorderBrush"] = transparentBrush;
            handler.PlatformView.Resources["TextControlBorderBrushPointerOver"] = transparentBrush;
            handler.PlatformView.Resources["TextControlBorderBrushFocused"] = transparentBrush;
            handler.PlatformView.Resources["TextControlBorderThemeThickness"] = noBorder;
            handler.PlatformView.Resources["TextControlBorderThemeThicknessFocused"] = noBorder;
        });
#endif

        ConfigurationHelper.ConfigureServices(builder.Services);

        builder.Services.AddTransient<DashboardListViewModel>();
        builder.Services.AddTransient<AlunoListViewModel>();
        builder.Services.AddTransient<AlunoViewModel>();
        builder.Services.AddTransient<MatriculaListViewModel>();
        builder.Services.AddTransient<MatriculaViewModel>();
        builder.Services.AddTransient<ColaboradorListViewModel>();
        builder.Services.AddTransient<ColaboradorViewModel>();
        builder.Services.AddTransient<LogradouroListViewModel>();
        builder.Services.AddTransient<LogradouroViewModel>();

        builder.Services.AddTransient<DashboardListPage>();
        builder.Services.AddTransient<AlunoListPage>();
        builder.Services.AddTransient<AlunoPage>();
        builder.Services.AddTransient<MatriculaListPage>();
        builder.Services.AddTransient<MatriculaPage>();
        builder.Services.AddTransient<ColaboradorListPage>();
        builder.Services.AddTransient<ColaboradorPage>();
        builder.Services.AddTransient<LogradouroListPage>();
        builder.Services.AddTransient<LogradouroPage>();
        builder.Services.AddTransient<ConfigPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
