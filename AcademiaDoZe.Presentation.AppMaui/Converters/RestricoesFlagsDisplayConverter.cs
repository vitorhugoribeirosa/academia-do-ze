using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

public sealed class RestricoesFlagsDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppMatriculaRestricoes restricoes)
            return string.Empty;

        if (restricoes == AppMatriculaRestricoes.None)
            return LocalizationManager.Instance.TranslateSource(AppMatriculaRestricoes.None.GetDisplayName());

        return string.Join(", ", Enum.GetValues<AppMatriculaRestricoes>()
            .Where(item => item != AppMatriculaRestricoes.None && restricoes.HasFlag(item))
            .Select(item => LocalizationManager.Instance.TranslateSource(item.GetDisplayName())));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A conversão de texto para restrições não é suportada.");
}
