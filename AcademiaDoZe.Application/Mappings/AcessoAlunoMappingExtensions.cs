using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Application.Mappings;

public static class AcessoAlunoMappingExtensions
{
    public static AcessoAlunoDto ToDto(
        this AcessoAluno acesso, AlunoDto alunoDto)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(alunoDto);

        return new AcessoAlunoDto
        {
            Id = acesso.Id,
            AlunoAcesso = alunoDto,
            DataHoraEntrada = acesso.DataHoraEntrada,
            DataHoraSaida = acesso.DataHoraSaida
        };
    }

    public static AcessoAluno ToEntity(
        this AcessoAlunoDto acessoDto, Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        ArgumentNullException.ThrowIfNull(aluno);

        return CriarEntidade(
            acessoDto.Id,
            aluno,
            acessoDto.DataHoraEntrada,
            acessoDto.DataHoraSaida,
            "converter");
    }

    public static AcessoAluno UpdateFromDto(
        this AcessoAluno acesso,
        AcessoAlunoDto acessoDto,
        Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(acessoDto);
        ArgumentNullException.ThrowIfNull(aluno);

        return CriarEntidade(
            acesso.Id,
            aluno,
            acessoDto.DataHoraEntrada,
            acessoDto.DataHoraSaida,
            "atualizar");
    }

    private static AcessoAluno CriarEntidade(
        int id,
        Aluno aluno,
        DateTime entrada,
        DateTime? saida,
        string operacao)
    {
        var result = AcessoAluno.Restaurar(id, aluno.Id, entrada, saida);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Erro de validação ao {operacao} acesso do aluno: " +
                string.Join(", ", result.Notifications.Select(n => n.Mensagem)));
        }
        return result.Value!;
    }
}
