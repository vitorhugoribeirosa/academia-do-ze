// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;

namespace AcademiaDoZe.Domain.Entities;

public sealed class AcessoAluno : Entity, IAggregateRoot
{
    public int AlunoId { get; private set; }
    public DateTime DataHoraEntrada { get; private set; }
    public DateTime? DataHoraSaida { get; private set; }
    public bool EstaEmAberto => DataHoraSaida is null;

    private AcessoAluno(int id, int alunoId, DateTime dataHoraEntrada) : base(id)
    {
        AlunoId = alunoId;
        DataHoraEntrada = dataHoraEntrada;
    }

    public static Result<AcessoAluno> Criar(int id, Aluno aluno, DateTime dataHoraEntrada) =>
        CriarEntrada(id, aluno, dataHoraEntrada);

    public static Result<AcessoAluno> CriarEntrada(
        int id, Aluno aluno, DateTime dataHoraEntrada)
    {
        var notifications = new List<Notification>();

        if (aluno is null)
            notifications.Add(new Notification("Aluno", "ALUNO_INVALIDO"));

        ValidarHorario(dataHoraEntrada, "DataHoraEntrada", notifications);

        if (notifications.Count != 0)
            return Result<AcessoAluno>.Failure(notifications);

        return Result<AcessoAluno>.Success(
            new AcessoAluno(id, aluno!.Id, dataHoraEntrada));
    }

    public static Result<AcessoAluno> Restaurar(
        int id, int alunoId, DateTime dataHoraEntrada, DateTime? dataHoraSaida)
    {
        var notifications = new List<Notification>();

        if (alunoId <= 0)
            notifications.Add(new Notification("AlunoId", "ALUNO_INVALIDO"));

        ValidarHorario(dataHoraEntrada, "DataHoraEntrada", notifications);

        if (dataHoraSaida.HasValue)
        {
            ValidarHorario(dataHoraSaida.Value, "DataHoraSaida", notifications);
            if (dataHoraSaida.Value < dataHoraEntrada)
            {
                notifications.Add(
                    new Notification("DataHoraSaida", "SAIDA_ANTERIOR_A_ENTRADA"));
            }
        }

        if (notifications.Count != 0)
            return Result<AcessoAluno>.Failure(notifications);

        var acesso = new AcessoAluno(id, alunoId, dataHoraEntrada)
        {
            DataHoraSaida = dataHoraSaida
        };
        return Result<AcessoAluno>.Success(acesso);
    }

    public Result<AcessoAluno> RegistrarSaida(Aluno aluno, DateTime dataHoraSaida)
    {
        var notifications = new List<Notification>();

        if (aluno is null)
            notifications.Add(new Notification("Aluno", "ALUNO_INVALIDO"));
        else if (aluno.Id != AlunoId)
            notifications.Add(new Notification("Aluno", "ALUNO_DIFERENTE_DA_ENTRADA"));

        if (DataHoraSaida.HasValue)
            notifications.Add(new Notification("DataHoraSaida", "SAIDA_JA_REGISTRADA"));

        ValidarHorario(dataHoraSaida, "DataHoraSaida", notifications);

        if (dataHoraSaida < DataHoraEntrada)
            notifications.Add(new Notification("DataHoraSaida", "SAIDA_ANTERIOR_A_ENTRADA"));

        if (notifications.Count != 0)
            return Result<AcessoAluno>.Failure(notifications);

        DataHoraSaida = dataHoraSaida;
        return Result<AcessoAluno>.Success(this);
    }

    private static void ValidarHorario(
        DateTime dataHora, string propriedade, ICollection<Notification> notifications)
    {
        if (dataHora.TimeOfDay < new TimeSpan(6, 0, 0) ||
            dataHora.TimeOfDay > new TimeSpan(22, 0, 0))
        {
            notifications.Add(
                new Notification(propriedade, "DATA_HORA_INTERVALO_INVALIDO"));
        }
    }
}
