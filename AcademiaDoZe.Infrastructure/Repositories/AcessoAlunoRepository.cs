using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoAlunoRepository : AcessoRepositoryBase<AcessoAluno>, IAcessoAlunoRepository
{
    protected override int PessoaTipo => 1;
    protected override string NomeEntidade => "ACESSO_ALUNO";

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    protected override int ObterPessoaId(AcessoAluno acesso) => acesso.AlunoId;
    protected override DateTime ObterDataHoraEntrada(AcessoAluno acesso) =>
        acesso.DataHoraEntrada;
    protected override DateTime? ObterDataHoraSaida(AcessoAluno acesso) =>
        acesso.DataHoraSaida;
    protected override Result<AcessoAluno> Restaurar(
        int id, int pessoaId, DateTime entrada, DateTime? saida) =>
        AcessoAluno.Restaurar(id, pessoaId, entrada, saida);

    public Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(
        int? alunoId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default) =>
        ObterPorPessoaPeriodoAsync(alunoId, inicio, fim, cancellationToken);

    public Task<AcessoAluno?> ObterUltimoAcesso(
        int alunoId, CancellationToken cancellationToken = default) =>
        ObterUltimoAsync(alunoId, cancellationToken);

    public Task<bool> EstaNaAcademia(
        int alunoId, CancellationToken cancellationToken = default) =>
        EstaNaAcademiaAsync(alunoId, cancellationToken);

    public Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(
        int mes, CancellationToken cancellationToken = default) =>
        ObterHorariosPorMesAsync(mes, cancellationToken);

    public Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(
        int mes, CancellationToken cancellationToken = default) =>
        ObterPermanenciaPorMesAsync(mes, cancellationToken);

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(
        int dias, CancellationToken cancellationToken = default)
    {
        if (dias < 0)
            throw new ArgumentOutOfRangeException(nameof(dias));

        try
        {
            var limite = DatabaseType switch
            {
                DatabaseType.SqlServer => "DATEADD(day, -@Dias, GETDATE())",
                DatabaseType.MySql => "DATE_SUB(NOW(), INTERVAL @Dias DAY)",
                DatabaseType.Sqlite => "DATETIME('now', '-' || @Dias || ' days')",
                _ => throw new ArgumentOutOfRangeException()
            };
            var query =
                "SELECT a.id_aluno FROM tb_aluno a " +
                "WHERE NOT EXISTS (SELECT 1 FROM tb_acesso ac " +
                "WHERE ac.pessoa_tipo = 1 AND ac.pessoa_id = a.id_aluno " +
                $"AND ac.data_hora_entrada >= {limite}) ORDER BY a.id_aluno";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Dias", dias, DbType.Int32);
            var ids = new List<int>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    ids.Add(Convert.ToInt32(reader["id_aluno"]));
            }

            await using var alunoRepository =
                new AlunoRepository(ConnectionString, DatabaseType);
            var alunos = new List<Aluno>();
            foreach (var id in ids)
            {
                var aluno = await alunoRepository.ObterPorId(id, cancellationToken);
                if (aluno is not null)
                    alunos.Add(aluno);
            }
            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_ALUNOS_SEM_ACESSO",
                "Erro ao obter alunos sem acesso recente.",
                ex);
        }
    }
}
