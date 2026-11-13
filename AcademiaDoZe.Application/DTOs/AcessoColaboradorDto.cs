namespace AcademiaDoZe.Application.DTOs;

public class AcessoColaboradorDto
{
    public int Id { get; set; }
    public required ColaboradorDto ColaboradorAcesso { get; set; }
    public required DateTime DataHoraEntrada { get; set; }
    public DateTime? DataHoraSaida { get; set; }
    public bool EstaEmAberto => DataHoraSaida is null;
    public TimeSpan? PermanenciaTotalDia { get; set; }
    public TimeSpan? LimiteJornada { get; set; }
}
