using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Application.Mappings;

public static class AcessoColaboradorMappingExtensions
{
    public static AcessoColaboradorDto ToDto(
        this AcessoColaborador acesso, ColaboradorDto colaboradorDto)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(colaboradorDto);

        return new AcessoColaboradorDto
        {
            Id = acesso.Id,
            ColaboradorAcesso = colaboradorDto,
            DataHoraEntrada = acesso.DataHoraEntrada,
            DataHoraSaida = acesso.DataHoraSaida
        };
    }

    public static AcessoColaborador ToEntity(
        this AcessoColaboradorDto acessoDto, Colaborador colaborador)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        ArgumentNullException.ThrowIfNull(colaborador);

        return CriarEntidade(
            acessoDto.Id,
            colaborador,
            acessoDto.DataHoraEntrada,
            acessoDto.DataHoraSaida,
            "converter");
    }

    public static AcessoColaborador UpdateFromDto(
        this AcessoColaborador acesso,
        AcessoColaboradorDto acessoDto,
        Colaborador colaborador)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(acessoDto);
        ArgumentNullException.ThrowIfNull(colaborador);

        return CriarEntidade(
            acesso.Id,
            colaborador,
            acessoDto.DataHoraEntrada,
            acessoDto.DataHoraSaida,
            "atualizar");
    }

    private static AcessoColaborador CriarEntidade(
        int id,
        Colaborador colaborador,
        DateTime entrada,
        DateTime? saida,
        string operacao)
    {
        var result = AcessoColaborador.Restaurar(
            id, colaborador.Id, entrada, saida);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Erro de validação ao {operacao} acesso do colaborador: " +
                string.Join(", ", result.Notifications.Select(n => n.Mensagem)));
        }
        return result.Value!;
    }
}
