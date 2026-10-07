using AcademiaDoZe.Application.DTOs;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

public sealed class FotoToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var conteudo = value switch
        {
            ArquivoDto arquivo => arquivo.Conteudo,
            byte[] bytes => bytes,
            _ => null
        };

        return conteudo is { Length: > 0 }
            ? ImageSource.FromStream(() => new MemoryStream(conteudo, writable: false))
            : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A conversão de ImageSource para foto não é suportada.");
}
