using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Application.Mappings;

public static class ColaboradorEnumMappingExtensions
{
    public static ColaboradorTipo ToDomain(this AppColaboradorTipo tipo) => (ColaboradorTipo)tipo;
    public static AppColaboradorTipo ToApplication(this ColaboradorTipo tipo) => (AppColaboradorTipo)tipo;
    public static ColaboradorVinculo ToDomain(this AppColaboradorVinculo vinculo) => (ColaboradorVinculo)vinculo;
    public static AppColaboradorVinculo ToApplication(this ColaboradorVinculo vinculo) => (AppColaboradorVinculo)vinculo;
}
