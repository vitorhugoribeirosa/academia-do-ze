using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Repositories;

public abstract class AcessoRepositoryBase<TAcesso> : BaseRepository
    where TAcesso : Entity, IAggregateRoot
{
    private const string BaseSelectQuery =
        "SELECT id_acesso, pessoa_id, data_hora_entrada, data_hora_saida " +
        "FROM tb_acesso ";

    protected abstract int PessoaTipo { get; }
    protected abstract string NomeEntidade { get; }
    protected abstract int ObterPessoaId(TAcesso acesso);
    protected abstract DateTime ObterDataHoraEntrada(TAcesso acesso);
    protected abstract DateTime? ObterDataHoraSaida(TAcesso acesso);
    protected abstract Result<TAcesso> Restaurar(
        int id, int pessoaId, DateTime entrada, DateTime? saida);

    protected AcessoRepositoryBase(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    public async Task<TAcesso?> ObterPorId(
        int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo",
                cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            AdicionarPessoaTipo(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : default;
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_POR_ID", $"Erro ao obter {NomeEntidade} por ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<TAcesso>> ObterTodos(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE pessoa_tipo = @PessoaTipo " +
                "ORDER BY data_hora_entrada DESC, id_acesso DESC",
                cancellationToken);
            AdicionarPessoaTipo(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_TODOS", $"Erro ao obter acessos de {NomeEntidade}.", ex);
        }
    }

    public async Task<TAcesso> Adicionar(
        TAcesso entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string insertSql =
                "INSERT INTO tb_acesso " +
                "(pessoa_tipo, pessoa_id, data_hora_entrada, data_hora_saida) " +
                "VALUES (@PessoaTipo, @PessoaId, @DataHoraEntrada, @DataHoraSaida)";

            await using var command = await CreateCommandAsync(
                FormatInsertQuery(insertSql), cancellationToken);
            AdicionarParametros(command, entity);

            var id = await command.ExecuteScalarIdAsync(
                $"ERRO_ADICIONAR_{NomeEntidade.ToUpperInvariant()}",
                $"Falha ao obter ID inserido para {NomeEntidade}.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty(
                nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw CriarErro("ADICIONAR", $"Erro ao adicionar {NomeEntidade}.", ex);
        }
    }

    public async Task<TAcesso> Atualizar(
        TAcesso entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query =
                "UPDATE tb_acesso SET pessoa_id = @PessoaId, " +
                "data_hora_entrada = @DataHoraEntrada, data_hora_saida = @DataHoraSaida " +
                "WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            AdicionarParametros(command, entity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhum {NomeEntidade} encontrado com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw CriarErro(
                "ATUALIZAR", $"Erro ao atualizar {NomeEntidade} ID {entity.Id}.", ex);
        }
    }

    public async Task<bool> Remover(
        int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo",
                cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            AdicionarPessoaTipo(command);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw CriarErro("REMOVER", $"Erro ao remover {NomeEntidade} ID {id}.", ex);
        }
    }

    protected async Task<IEnumerable<TAcesso>> ObterPorPessoaPeriodoAsync(
        int? pessoaId,
        DateOnly? inicio,
        DateOnly? fim,
        CancellationToken cancellationToken)
    {
        try
        {
            var filtros = new List<string> { "pessoa_tipo = @PessoaTipo" };
            if (pessoaId.HasValue)
                filtros.Add("pessoa_id = @PessoaId");
            if (inicio.HasValue)
                filtros.Add("data_hora_entrada >= @Inicio");
            if (fim.HasValue)
                filtros.Add("data_hora_entrada < @FimExclusivo");

            var query = BaseSelectQuery + "WHERE " + string.Join(" AND ", filtros) +
                " ORDER BY data_hora_entrada DESC, id_acesso DESC";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            if (pessoaId.HasValue)
                command.AddParameter("@PessoaId", pessoaId.Value, DbType.Int32);
            if (inicio.HasValue)
                command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            if (fim.HasValue)
            {
                command.AddParameter(
                    "@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            }
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_POR_PERIODO", $"Erro ao consultar {NomeEntidade} por periodo.", ex);
        }
    }

    protected async Task<TAcesso?> ObterUltimoAsync(
        int pessoaId, CancellationToken cancellationToken)
    {
        try
        {
            var select = DatabaseType == DatabaseType.SqlServer
                ? "SELECT TOP 1 id_acesso, pessoa_id, data_hora_entrada, data_hora_saida FROM tb_acesso "
                : BaseSelectQuery;
            var limite = DatabaseType == DatabaseType.SqlServer ? string.Empty : " LIMIT 1";
            var query = select +
                "WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @PessoaId " +
                $"ORDER BY data_hora_entrada DESC, id_acesso DESC{limite}";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            command.AddParameter("@PessoaId", pessoaId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : default;
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_ULTIMO", $"Erro ao obter ultimo {NomeEntidade}.", ex);
        }
    }

    protected async Task<bool> EstaNaAcademiaAsync(
        int pessoaId, CancellationToken cancellationToken)
    {
        try
        {
            const string query =
                "SELECT COUNT(1) FROM tb_acesso " +
                "WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @PessoaId " +
                "AND data_hora_saida IS NULL";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            command.AddParameter("@PessoaId", pessoaId, DbType.Int32);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        catch (DbException ex)
        {
            throw CriarErro("VERIFICAR_ACESSO_ABERTO", "Erro ao verificar acesso aberto.", ex);
        }
    }

    protected async Task<Dictionary<TimeOnly, int>> ObterHorariosPorMesAsync(
        int mes, CancellationToken cancellationToken)
    {
        if (mes is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(mes));

        try
        {
            var hora = GetDateHourExpression("data_hora_entrada");
            var mesSql = GetDateMonthExpression("data_hora_entrada");
            var query = $"SELECT {hora} AS hora, COUNT(1) AS quantidade " +
                "FROM tb_acesso WHERE pessoa_tipo = @PessoaTipo " +
                $"AND {mesSql} = @Mes GROUP BY {hora} ORDER BY {hora}";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            command.AddParameter("@Mes", mes, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var resultado = new Dictionary<TimeOnly, int>();
            while (await reader.ReadAsync(cancellationToken))
            {
                resultado[new TimeOnly(Convert.ToInt32(reader["hora"]), 0)] =
                    Convert.ToInt32(reader["quantidade"]);
            }
            return resultado;
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_HORARIOS", "Erro ao obter horarios de acesso.", ex);
        }
    }

    protected async Task<Dictionary<int, TimeSpan>> ObterPermanenciaPorMesAsync(
        int mes, CancellationToken cancellationToken)
    {
        if (mes is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(mes));

        try
        {
            var dia = GetDateDayExpression("data_hora_entrada");
            var mesSql = GetDateMonthExpression("data_hora_entrada");
            var segundos = DatabaseType switch
            {
                DatabaseType.SqlServer =>
                    "DATEDIFF(SECOND, data_hora_entrada, data_hora_saida)",
                DatabaseType.MySql =>
                    "TIMESTAMPDIFF(SECOND, data_hora_entrada, data_hora_saida)",
                DatabaseType.Sqlite =>
                    "ROUND((julianday(data_hora_saida) - " +
                    "julianday(data_hora_entrada)) * 86400.0)",
                _ => throw new ArgumentOutOfRangeException()
            };
            var query = $"SELECT {dia} AS dia, AVG({segundos}) AS segundos " +
                "FROM tb_acesso WHERE pessoa_tipo = @PessoaTipo " +
                $"AND data_hora_saida IS NOT NULL AND {mesSql} = @Mes " +
                $"GROUP BY {dia} ORDER BY {dia}";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            command.AddParameter("@Mes", mes, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var resultado = new Dictionary<int, TimeSpan>();
            while (await reader.ReadAsync(cancellationToken))
            {
                resultado[Convert.ToInt32(reader["dia"])] =
                    TimeSpan.FromSeconds(Convert.ToDouble(reader["segundos"]));
            }
            return resultado;
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_PERMANENCIA", "Erro ao obter permanencia media.", ex);
        }
    }

    protected async Task<TimeSpan> ObterPermanenciaNoDiaAsync(
        int pessoaId, DateOnly data, CancellationToken cancellationToken)
    {
        try
        {
            var segundos = DatabaseType switch
            {
                DatabaseType.SqlServer =>
                    "DATEDIFF(SECOND, data_hora_entrada, data_hora_saida)",
                DatabaseType.MySql =>
                    "TIMESTAMPDIFF(SECOND, data_hora_entrada, data_hora_saida)",
                DatabaseType.Sqlite =>
                    "ROUND((julianday(data_hora_saida) - " +
                    "julianday(data_hora_entrada)) * 86400.0)",
                _ => throw new ArgumentOutOfRangeException()
            };
            var query = $"SELECT COALESCE(SUM({segundos}), 0) FROM tb_acesso " +
                "WHERE pessoa_tipo = @PessoaTipo AND pessoa_id = @PessoaId " +
                "AND data_hora_saida IS NOT NULL " +
                "AND data_hora_entrada >= @Inicio AND data_hora_entrada < @Fim";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            AdicionarPessoaTipo(command);
            command.AddParameter("@PessoaId", pessoaId, DbType.Int32);
            command.AddParameter("@Inicio", data.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            command.AddParameter(
                "@Fim", data.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            var total = await command.ExecuteScalarAsync(cancellationToken);
            return TimeSpan.FromSeconds(Convert.ToDouble(total));
        }
        catch (DbException ex)
        {
            throw CriarErro("OBTER_PERMANENCIA_DIA", "Erro ao obter permanencia no dia.", ex);
        }
    }

    private TAcesso Map(DbDataReader reader)
    {
        var id = reader.GetInt32Value("id_acesso");
        var result = Restaurar(
            id,
            reader.GetInt32Value("pessoa_id"),
            reader.GetDateTimeValue("data_hora_entrada"),
            reader.GetNullableDateTime("data_hora_saida"));

        if (result.IsFailure)
        {
            throw new InfrastructureException(
                "ERRO_DOMINIO_MAPEAMENTO",
                $"Erro de dominio ao mapear {NomeEntidade} ID {id}: " +
                string.Join(", ", result.Notifications.Select(n => n.Mensagem)));
        }

        return result.Value!;
    }

    private async Task<List<TAcesso>> LerTodosAsync(
        DbDataReader reader, CancellationToken cancellationToken)
    {
        var acessos = new List<TAcesso>();
        while (await reader.ReadAsync(cancellationToken))
            acessos.Add(Map(reader));
        return acessos;
    }

    private void AdicionarParametros(DbCommand command, TAcesso acesso)
    {
        AdicionarPessoaTipo(command);
        command.AddParameter("@PessoaId", ObterPessoaId(acesso), DbType.Int32);
        command.AddParameter(
            "@DataHoraEntrada", ObterDataHoraEntrada(acesso), DbType.DateTime);
        command.AddParameter(
            "@DataHoraSaida", ObterDataHoraSaida(acesso), DbType.DateTime);
    }

    private void AdicionarPessoaTipo(DbCommand command) =>
        command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);

    private InfrastructureException CriarErro(
        string operacao, string mensagem, Exception ex) =>
        new($"ERRO_{operacao}_{NomeEntidade.ToUpperInvariant()}", mensagem, ex);
}
