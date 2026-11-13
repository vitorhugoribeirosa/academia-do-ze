namespace AcademiaDoZe.Application.DTOs;

public class AcessoAlunoDto
{
    public int Id { get; set; }
    public required AlunoDto AlunoAcesso { get; set; }
    public required DateTime DataHoraEntrada { get; set; }
    public DateTime? DataHoraSaida { get; set; }
    public bool EstaEmAberto => DataHoraSaida is null;
    public int? DiasRestantesPlano { get; set; }
    public DateOnly? DataFimPlano { get; set; }
    public TimeSpan? Permanencia { get; set; }
}
