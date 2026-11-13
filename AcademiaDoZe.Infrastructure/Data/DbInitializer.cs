using AcademiaDoZe.Infrastructure.Exceptions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DbInitializer
{
    private static readonly ConcurrentDictionary<string, bool> BancosInicializados = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TravasInicializacao = new();

    public static async Task InicializarAsync(
        string connectionString,
        DatabaseType databaseType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var key = $"{databaseType}:{connectionString}";
        if (BancosInicializados.ContainsKey(key))
            return;

        var trava = TravasInicializacao.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(cancellationToken);

        try
        {
            if (BancosInicializados.ContainsKey(key))
                return;

            var scriptSql = ObterScript(databaseType);
            await using var connection = DbProvider.CreateConnection(connectionString, databaseType);
            await connection.OpenAsync(cancellationToken);
            await MigrarTabelaAcessoLegadaAsync(connection, databaseType, cancellationToken);
            await using var command = DbProvider.CreateCommand(scriptSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            BancosInicializados.TryAdd(key, true);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_INICIALIZAR_BANCO", "Erro ao inicializar banco de dados.", ex);
        }
        finally
        {
            trava.Release();
        }
    }

    public static string ObterScript(DatabaseType databaseType)
    {
        var nomeScript = DbProvider.GetScriptName(databaseType);
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(resource => resource.EndsWith(nomeScript, StringComparison.OrdinalIgnoreCase))
            ?? throw new InfrastructureException("SCRIPT_EMBARCADO_NAO_ENCONTRADO", $"Script SQL embarcado '{nomeScript}' nao encontrado.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InfrastructureException("ERRO_LEITURA_SCRIPT", $"Erro ao carregar o script embarcado '{nomeScript}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task MigrarTabelaAcessoLegadaAsync(
        DbConnection connection,
        DatabaseType databaseType,
        CancellationToken cancellationToken)
    {
        var colunas = await ObterColunasAcessoAsync(connection, databaseType, cancellationToken);
        if (colunas.Count == 0)
            return;

        var possuiFormatoLegado = colunas.Contains("data_hora") &&
                                  !colunas.Contains("data_hora_entrada");

        if (possuiFormatoLegado)
        {
            var renomear = databaseType switch
            {
                DatabaseType.Sqlite =>
                    "ALTER TABLE tb_acesso RENAME COLUMN data_hora TO data_hora_entrada",
                DatabaseType.MySql =>
                    "ALTER TABLE tb_acesso CHANGE COLUMN data_hora data_hora_entrada DATETIME NOT NULL",
                DatabaseType.SqlServer =>
                    "EXEC sp_rename 'dbo.tb_acesso.data_hora', 'data_hora_entrada', 'COLUMN'",
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
            };
            await ExecutarAsync(connection, renomear, cancellationToken);
            colunas.Remove("data_hora");
            colunas.Add("data_hora_entrada");
        }

        if (!colunas.Contains("data_hora_saida"))
        {
            var adicionarSaida = databaseType switch
            {
                DatabaseType.Sqlite =>
                    "ALTER TABLE tb_acesso ADD COLUMN data_hora_saida TEXT NULL",
                DatabaseType.MySql =>
                    "ALTER TABLE tb_acesso ADD COLUMN data_hora_saida DATETIME NULL AFTER data_hora_entrada",
                DatabaseType.SqlServer =>
                    "ALTER TABLE dbo.tb_acesso ADD data_hora_saida DATETIME2 NULL",
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
            };
            await ExecutarAsync(connection, adicionarSaida, cancellationToken);

            // Registros do modelo antigo representavam apenas uma passagem. Eles são
            // preservados como acessos encerrados de duração zero, sem bloquear nova entrada.
            await ExecutarAsync(
                connection,
                "UPDATE tb_acesso SET data_hora_saida = data_hora_entrada WHERE data_hora_saida IS NULL",
                cancellationToken);
        }

        await GarantirIndicesAcessoAsync(connection, databaseType, cancellationToken);
    }

    private static async Task<HashSet<string>> ObterColunasAcessoAsync(
        DbConnection connection,
        DatabaseType databaseType,
        CancellationToken cancellationToken)
    {
        var query = databaseType switch
        {
            DatabaseType.Sqlite => "PRAGMA table_info(tb_acesso)",
            DatabaseType.MySql =>
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_acesso'",
            DatabaseType.SqlServer =>
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tb_acesso'",
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
        };

        await using var command = DbProvider.CreateCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var colunas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            var nome = databaseType == DatabaseType.Sqlite
                ? reader.GetString(1)
                : reader.GetString(0);
            colunas.Add(nome);
        }
        return colunas;
    }

    private static async Task GarantirIndicesAcessoAsync(
        DbConnection connection,
        DatabaseType databaseType,
        CancellationToken cancellationToken)
    {
        if (databaseType == DatabaseType.MySql)
        {
            var colunas = await ObterColunasAcessoAsync(connection, databaseType, cancellationToken);
            if (!colunas.Contains("acesso_em_aberto"))
            {
                await ExecutarAsync(connection,
                    "ALTER TABLE tb_acesso ADD COLUMN acesso_em_aberto TINYINT " +
                    "GENERATED ALWAYS AS (CASE WHEN data_hora_saida IS NULL THEN 1 ELSE NULL END) STORED",
                    cancellationToken);
            }

            await CriarIndiceMySqlSeAusenteAsync(connection, "ix_tb_acesso_pessoa",
                "CREATE INDEX ix_tb_acesso_pessoa ON tb_acesso(pessoa_tipo, pessoa_id)", cancellationToken);
            await CriarIndiceMySqlSeAusenteAsync(connection, "ix_tb_acesso_data_hora_entrada",
                "CREATE INDEX ix_tb_acesso_data_hora_entrada ON tb_acesso(data_hora_entrada)", cancellationToken);
            await CriarIndiceMySqlSeAusenteAsync(connection, "ix_tb_acesso_data_hora_saida",
                "CREATE INDEX ix_tb_acesso_data_hora_saida ON tb_acesso(data_hora_saida)", cancellationToken);
            await CriarIndiceMySqlSeAusenteAsync(connection, "uq_tb_acesso_aberto",
                "CREATE UNIQUE INDEX uq_tb_acesso_aberto ON tb_acesso(pessoa_tipo, pessoa_id, acesso_em_aberto)", cancellationToken);
        }
        else if (databaseType == DatabaseType.SqlServer)
        {
            await ExecutarAsync(connection, """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tb_acesso_pessoa' AND object_id = OBJECT_ID('dbo.tb_acesso'))
                    CREATE INDEX IX_tb_acesso_pessoa ON dbo.tb_acesso(pessoa_tipo, pessoa_id);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tb_acesso_data_hora_entrada' AND object_id = OBJECT_ID('dbo.tb_acesso'))
                    CREATE INDEX IX_tb_acesso_data_hora_entrada ON dbo.tb_acesso(data_hora_entrada);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tb_acesso_data_hora_saida' AND object_id = OBJECT_ID('dbo.tb_acesso'))
                    CREATE INDEX IX_tb_acesso_data_hora_saida ON dbo.tb_acesso(data_hora_saida);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_tb_acesso_aberto' AND object_id = OBJECT_ID('dbo.tb_acesso'))
                    CREATE UNIQUE INDEX UQ_tb_acesso_aberto ON dbo.tb_acesso(pessoa_tipo, pessoa_id) WHERE data_hora_saida IS NULL;
                """, cancellationToken);
        }
    }

    private static async Task CriarIndiceMySqlSeAusenteAsync(
        DbConnection connection,
        string nome,
        string createSql,
        CancellationToken cancellationToken)
    {
        await using var verificar = DbProvider.CreateCommand(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.STATISTICS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tb_acesso' AND INDEX_NAME = @Nome",
            connection);
        verificar.AddParameter("@Nome", nome, System.Data.DbType.String);
        if (Convert.ToInt32(await verificar.ExecuteScalarAsync(cancellationToken)) == 0)
            await ExecutarAsync(connection, createSql, cancellationToken);
    }

    private static async Task ExecutarAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = DbProvider.CreateCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
