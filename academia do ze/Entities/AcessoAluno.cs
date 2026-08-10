// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;

namespace AcademiaDoZe.Domain.Entities;

public sealed class AcessoAluno : Entity, IAggregateRoot
{
    public Aluno Aluno { get; }
    public DateTime DataHoraEntrada { get; }
    public DateTime? DataHoraSaida { get; private set; }
    public TimeSpan? TempoPermanencia => DataHoraSaida - DataHoraEntrada;

    private AcessoAluno(int id, Aluno aluno, DateTime dataHoraEntrada) : base(id)
    {
        Aluno = aluno;
        DataHoraEntrada = dataHoraEntrada;
    }

    public static Result<AcessoAluno> Criar(
        int id,
        Aluno? aluno,
        DateTime dataHoraEntrada)
    {
        var notifications = new List<Notification>();

        if (aluno is null)
            notifications.Add(new Notification("Aluno", "ALUNO_OBRIGATORIO"));

        if (dataHoraEntrada == default)
            notifications.Add(new Notification("DataHoraEntrada", "DATA_HORA_ENTRADA_OBRIGATORIA"));

        if (notifications.Count != 0)
            return Result<AcessoAluno>.Failure(notifications);

        return Result<AcessoAluno>.Success(new AcessoAluno(id, aluno!, dataHoraEntrada));
    }

    public Result<TimeSpan> RegistrarSaida(DateTime dataHoraSaida)
    {
        if (DataHoraSaida is not null)
            return Result<TimeSpan>.Failure("DataHoraSaida", "SAIDA_JA_REGISTRADA");

        if (dataHoraSaida < DataHoraEntrada)
            return Result<TimeSpan>.Failure("DataHoraSaida", "SAIDA_ANTERIOR_ENTRADA");

        DataHoraSaida = dataHoraSaida;
        return Result<TimeSpan>.Success(dataHoraSaida - DataHoraEntrada);
    }
}
