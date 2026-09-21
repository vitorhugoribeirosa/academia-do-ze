using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Application.Mappings;

public static class MatriculaEnumMappingExtensions
{
    public static MatriculaPlano ToDomain(this AppMatriculaPlano plano) => (MatriculaPlano)plano;
    public static AppMatriculaPlano ToApplication(this MatriculaPlano plano) => (AppMatriculaPlano)plano;
    public static MatriculaRestricoes ToDomain(this AppMatriculaRestricoes restricoes) => (MatriculaRestricoes)restricoes;
    public static AppMatriculaRestricoes ToApplication(this MatriculaRestricoes restricoes) => (AppMatriculaRestricoes)restricoes;
}
