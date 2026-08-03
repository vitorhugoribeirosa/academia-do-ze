// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.Entities;

public sealed class AcessoAluno : Entity
{
    public Aluno Aluno { get; private set; }
    public DateTime DataHoraEntrada { get; private set; }
    public DateTime? DataHoraSaida { get; private set; }

    public AcessoAluno(
        int id,
        Aluno aluno,
        DateTime dataHoraEntrada,
        DateTime? dataHoraSaida) : base(id)
    {
        Aluno = aluno;
        DataHoraEntrada = dataHoraEntrada;
        DataHoraSaida = dataHoraSaida;
    }
}
