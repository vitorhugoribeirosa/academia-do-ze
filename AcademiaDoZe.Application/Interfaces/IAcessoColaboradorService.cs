using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoColaboradorService
{
    Task<AcessoColaboradorDto?> ObterPorIdAsync(
        int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AcessoColaboradorDto>> ObterTodosAsync(
        CancellationToken cancellationToken = default);
    Task<IEnumerable<AcessoColaboradorDto>> ObterPorColaboradorPeriodoAsync(
        int? colaboradorId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto?> ObterUltimoAcessoAsync(
        int colaboradorId, CancellationToken cancellationToken = default);
    Task<bool> EstaNaAcademiaAsync(
        int colaboradorId, CancellationToken cancellationToken = default);
    Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(
        int colaboradorId,
        DateOnly data,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto> RegistrarEntradaAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto> RegistrarSaidaAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto> RegistrarMovimentacaoAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto> AdicionarAsync(
        AcessoColaboradorDto acessoDto,
        CancellationToken cancellationToken = default);
    Task<AcessoColaboradorDto> AtualizarAsync(
        AcessoColaboradorDto acessoDto,
        CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(
        int id, CancellationToken cancellationToken = default);
    Task<int> LimparRegistrosAsync(CancellationToken cancellationToken = default);
}
