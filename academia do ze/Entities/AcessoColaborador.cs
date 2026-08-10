// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Domain.Entities;

public sealed class AcessoColaborador : Entity, IAggregateRoot
{
    public Colaborador Colaborador { get; }
    public DateTime DataHoraEntrada { get; }
    public DateTime? DataHoraSaida { get; private set; }
    public TimeSpan? TempoPermanencia => DataHoraSaida - DataHoraEntrada;
    public TimeSpan LimiteDiario => Colaborador.Vinculo == ColaboradorVinculo.CLT
        ? TimeSpan.FromHours(8)
        : TimeSpan.FromHours(6);

    private AcessoColaborador(
        int id,
        Colaborador colaborador,
        DateTime dataHoraEntrada) : base(id)
    {
        Colaborador = colaborador;
        DataHoraEntrada = dataHoraEntrada;
    }

    public static Result<AcessoColaborador> Criar(
        int id,
        Colaborador? colaborador,
        DateTime dataHoraEntrada)
    {
        var notifications = new List<Notification>();

        if (colaborador is null)
            notifications.Add(new Notification("Colaborador", "COLABORADOR_OBRIGATORIO"));

        if (dataHoraEntrada == default)
            notifications.Add(new Notification("DataHoraEntrada", "DATA_HORA_ENTRADA_OBRIGATORIA"));

        if (notifications.Count != 0)
            return Result<AcessoColaborador>.Failure(notifications);

        return Result<AcessoColaborador>.Success(
            new AcessoColaborador(id, colaborador!, dataHoraEntrada));
    }

    public Result<TimeSpan> RegistrarSaida(
        DateTime dataHoraSaida,
        TimeSpan tempoTrabalhadoAnteriormenteNoDia)
    {
        if (DataHoraSaida is not null)
            return Result<TimeSpan>.Failure("DataHoraSaida", "SAIDA_JA_REGISTRADA");

        if (dataHoraSaida < DataHoraEntrada)
            return Result<TimeSpan>.Failure("DataHoraSaida", "SAIDA_ANTERIOR_ENTRADA");

        if (tempoTrabalhadoAnteriormenteNoDia < TimeSpan.Zero)
            return Result<TimeSpan>.Failure("TempoTrabalhado", "TEMPO_TRABALHADO_INVALIDO");

        var tempoTotal = tempoTrabalhadoAnteriormenteNoDia + (dataHoraSaida - DataHoraEntrada);
        if (tempoTotal > LimiteDiario)
            return Result<TimeSpan>.Failure("TempoTrabalhado", "LIMITE_DIARIO_ULTRAPASSADO");

        DataHoraSaida = dataHoraSaida;
        return Result<TimeSpan>.Success(tempoTotal);
    }
}
