using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Mappings;

public static class MatriculaMappingExtensions
{
    public static MatriculaDto ToDto(this Matricula matricula, AlunoDto alunoDto)
    {
        ArgumentNullException.ThrowIfNull(matricula);
        ArgumentNullException.ThrowIfNull(alunoDto);

        return new MatriculaDto
        {
            Id = matricula.Id,
            AlunoMatricula = alunoDto,
            Plano = matricula.Plano.ToApplication(),
            DataInicio = matricula.DataInicio,
            DataFim = matricula.DataFim,
            Objetivo = matricula.Objetivo,
            RestricoesMedicas = matricula.RestricoesMedicas.ToApplication(),
            ObservacoesRestricoes = matricula.ObservacoesRestricoes,
            LaudoMedico = matricula.LaudoMedico == null
                ? null
                : new ArquivoDto { Conteudo = matricula.LaudoMedico.Conteudo }
        };
    }

    public static Matricula ToEntity(this MatriculaDto matriculaDto, Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(matriculaDto);
        ArgumentNullException.ThrowIfNull(aluno);

        var laudo = CriarArquivoOpcional(matriculaDto.LaudoMedico?.Conteudo);
        var result = Matricula.Criar(
            matriculaDto.Id,
            aluno,
            matriculaDto.Plano.ToDomain(),
            matriculaDto.DataInicio,
            matriculaDto.Objetivo,
            matriculaDto.RestricoesMedicas.ToDomain(),
            laudo,
            matriculaDto.ObservacoesRestricoes ?? string.Empty);

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao converter Matrícula: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    public static Matricula UpdateFromDto(this Matricula matricula, MatriculaDto matriculaDto, Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(matricula);
        ArgumentNullException.ThrowIfNull(matriculaDto);
        ArgumentNullException.ThrowIfNull(aluno);

        var laudo = matriculaDto.LaudoMedico?.Conteudo != null
            ? CriarArquivoOpcional(matriculaDto.LaudoMedico.Conteudo)
            : matricula.LaudoMedico;
        var result = Matricula.Criar(
            matricula.Id,
            aluno,
            matriculaDto.Plano.ToDomain(),
            matriculaDto.DataInicio,
            matriculaDto.Objetivo,
            matriculaDto.RestricoesMedicas.ToDomain(),
            laudo,
            matriculaDto.ObservacoesRestricoes ?? string.Empty);

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao atualizar Matrícula: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    private static Arquivo? CriarArquivoOpcional(byte[]? conteudo)
    {
        if (conteudo == null)
            return null;

        var result = Arquivo.Criar(conteudo);
        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação do laudo: {FormatarErros(result.Notifications)}");
        return result.Value!;
    }

    private static string FormatarErros(IEnumerable<Domain.Common.Notification> notifications) =>
        string.Join(", ", notifications.Select(notification => notification.Mensagem));
}
