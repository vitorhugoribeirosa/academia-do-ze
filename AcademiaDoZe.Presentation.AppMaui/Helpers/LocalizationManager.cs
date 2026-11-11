using AcademiaDoZe.Presentation.AppMaui.Resources.Strings;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AcademiaDoZe.Presentation.AppMaui.Helpers;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    public const string DefaultCulture = "pt-BR";
    public static readonly string[] SupportedCultures =
        ["pt-BR", "en-US", "es-ES"];

    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => AppResources.GetString(key);
    public string ShortDatePattern => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
    public string LongDatePattern => CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
    public FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    private LocalizationManager()
    {
    }

    public static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return DefaultCulture;

        var exact = SupportedCultures.FirstOrDefault(item =>
            item.Equals(cultureName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        var language = cultureName.Split('-', '_')[0];
        return SupportedCultures.FirstOrDefault(item =>
            item.StartsWith($"{language}-", StringComparison.OrdinalIgnoreCase))
            ?? DefaultCulture;
    }

    public void SetCulture(string? cultureName, bool persist = true)
    {
        var normalized = NormalizeCulture(cultureName);
        var culture = CultureInfo.GetCultureInfo(normalized);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        AppResources.Culture = culture;

        if (persist)
            Preferences.Default.Set("Cultura", normalized);

#if WINDOWS
        try
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = normalized;
        }
        catch
        {
            // A cultura .NET continua ativa caso o ambiente não permita alterar o idioma nativo.
        }
#endif

        if (Shell.Current is not null)
            Shell.Current.FlowDirection = FlowDirection;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public string TranslateSource(string? source)
    {
        if (string.IsNullOrEmpty(source))
            return source ?? string.Empty;

        if (LocalizationCatalog.Exact.TryGetValue(source, out var key))
            return this[key];

        foreach (var template in LocalizationCatalog.Templates)
        {
            var match = Regex.Match(source, template.Pattern, RegexOptions.Singleline);
            if (!match.Success)
                continue;

            var arguments = match.Groups.Cast<Group>().Skip(1).Select(group => group.Value).ToArray();
            return string.Format(CultureInfo.CurrentCulture, this[template.Key], arguments);
        }

        return source;
    }
}
