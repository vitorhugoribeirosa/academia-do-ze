using System.Globalization;
using System.Resources;

namespace AcademiaDoZe.Presentation.AppMaui.Resources.Strings;

public static class AppResources
{
    private static readonly ResourceManager Manager = new(
        "AcademiaDoZe.Presentation.AppMaui.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string GetString(string key) =>
        Manager.GetString(key, Culture ?? CultureInfo.CurrentUICulture) ?? key;
}
