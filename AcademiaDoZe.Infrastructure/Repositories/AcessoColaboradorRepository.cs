using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoColaboradorRepository
    : AcessoRepositoryBase<AcessoColaborador>, IAcessoColaboradorRepository
{
    protected override int PessoaTipo => 2;
    protected override string NomeEntidade => "ACESSO_COLABORADOR";

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    protected override int ObterPessoaId(AcessoColaborador acesso) =>
        acesso.ColaboradorId;
    protected override DateTime ObterDataHoraEntrada(AcessoColaborador acesso) =>
        acesso.DataHoraEntrada;
    protected override DateTime? ObterDataHoraSaida(AcessoColaborador acesso) =>
        acesso.DataHoraSaida;
    protected override Result<AcessoColaborador> Restaurar(
        int id, int pessoaId, DateTime entrada, DateTime? saida) =>
        AcessoColaborador.Restaurar(id, pessoaId, entrada, saida);

    public Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(
        int? colaboradorId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default) =>
        ObterPorPessoaPeriodoAsync(colaboradorId, inicio, fim, cancellationToken);

    public Task<AcessoColaborador?> ObterUltimoAcesso(
        int colaboradorId, CancellationToken cancellationToken = default) =>
        ObterUltimoAsync(colaboradorId, cancellationToken);

    public Task<int> RemoverTodos(CancellationToken cancellationToken = default) =>
        RemoverTodosDoTipoAsync(cancellationToken);

    public Task<TimeSpan> ObterHorasTrabalhadasNoDia(
        int colaboradorId,
        DateOnly data,
        CancellationToken cancellationToken = default) =>
        ObterPermanenciaNoDiaAsync(colaboradorId, data, cancellationToken);
}
