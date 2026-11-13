// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;

namespace AcademiaDoZe.Domain.Entities;

public sealed class AcessoColaborador : Entity, IAggregateRoot
{
    public int ColaboradorId { get; private set; }
    public DateTime DataHoraEntrada { get; private set; }
    public DateTime? DataHoraSaida { get; private set; }
    public bool EstaEmAberto => DataHoraSaida is null;

    private AcessoColaborador(
        int id, int colaboradorId, DateTime dataHoraEntrada) : base(id)
    {
        ColaboradorId = colaboradorId;
        DataHoraEntrada = dataHoraEntrada;
    }

    public static Result<AcessoColaborador> Criar(
        int id, Colaborador colaborador, DateTime dataHoraEntrada) =>
        CriarEntrada(id, colaborador, dataHoraEntrada);

    public static Result<AcessoColaborador> CriarEntrada(
        int id, Colaborador colaborador, DateTime dataHoraEntrada)
    {
        var notifications = new List<Notification>();

        if (colaborador is null)
            notifications.Add(new Notification("Colaborador", "COLABORADOR_INVALIDO"));

        if (notifications.Count != 0)
            return Result<AcessoColaborador>.Failure(notifications);

        return Result<AcessoColaborador>.Success(
            new AcessoColaborador(id, colaborador!.Id, dataHoraEntrada));
    }

    public static Result<AcessoColaborador> Restaurar(
        int id, int colaboradorId, DateTime dataHoraEntrada, DateTime? dataHoraSaida)
    {
        var notifications = new List<Notification>();

        if (colaboradorId <= 0)
        {
            notifications.Add(
                new Notification("ColaboradorId", "COLABORADOR_INVALIDO"));
        }

        if (dataHoraSaida.HasValue)
        {
            if (dataHoraSaida.Value < dataHoraEntrada)
            {
                notifications.Add(
                    new Notification("DataHoraSaida", "SAIDA_ANTERIOR_A_ENTRADA"));
            }
        }

        if (notifications.Count != 0)
            return Result<AcessoColaborador>.Failure(notifications);

        var acesso = new AcessoColaborador(id, colaboradorId, dataHoraEntrada)
        {
            DataHoraSaida = dataHoraSaida
        };
        return Result<AcessoColaborador>.Success(acesso);
    }

    public Result<AcessoColaborador> RegistrarSaida(
        Colaborador colaborador, DateTime dataHoraSaida)
    {
        var notifications = new List<Notification>();

        if (colaborador is null)
            notifications.Add(new Notification("Colaborador", "COLABORADOR_INVALIDO"));
        else if (colaborador.Id != ColaboradorId)
            notifications.Add(
                new Notification("Colaborador", "COLABORADOR_DIFERENTE_DA_ENTRADA"));

        if (DataHoraSaida.HasValue)
            notifications.Add(new Notification("DataHoraSaida", "SAIDA_JA_REGISTRADA"));

        if (dataHoraSaida < DataHoraEntrada)
            notifications.Add(new Notification("DataHoraSaida", "SAIDA_ANTERIOR_A_ENTRADA"));

        if (notifications.Count != 0)
            return Result<AcessoColaborador>.Failure(notifications);

        DataHoraSaida = dataHoraSaida;
        return Result<AcessoColaborador>.Success(this);
    }
}
