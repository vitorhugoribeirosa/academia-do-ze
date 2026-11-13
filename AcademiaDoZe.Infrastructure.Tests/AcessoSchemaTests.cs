using AcademiaDoZe.Infrastructure.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoSchemaTests
{
    [Theory(DisplayName = "Scripts: os três bancos possuem o novo modelo de acesso")]
    [InlineData(DatabaseType.Sqlite)]
    [InlineData(DatabaseType.MySql)]
    [InlineData(DatabaseType.SqlServer)]
    public void Scripts_Devem_Possuir_Entrada_E_Saida(DatabaseType databaseType)
    {
        var script = DbInitializer.ObterScript(databaseType);

        Assert.Contains("data_hora_entrada", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data_hora_saida", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pessoa_tipo", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pessoa_id", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "SQLite: tabela de acesso possui entrada e saída")]
    public async Task Sqlite_Deve_Criar_Tabela_Acesso_Com_Entrada_E_Saida()
    {
        await using var connection = await CriarBancoSqliteAsync();
        await using var command = DbProvider.CreateCommand(
            "PRAGMA table_info(tb_acesso);", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var colunas = new List<string>();

        while (await reader.ReadAsync())
            colunas.Add(reader.GetString(1));

        Assert.Contains("data_hora_entrada", colunas);
        Assert.Contains("data_hora_saida", colunas);
        Assert.DoesNotContain("data_hora", colunas);
    }

    [Fact(DisplayName = "SQLite: impede dois acessos abertos para a mesma pessoa")]
    public async Task Sqlite_Deve_Impedir_Dois_Acessos_Abertos_Para_Mesma_Pessoa()
    {
        await using var connection = await CriarBancoSqliteAsync();
        const string inserirAcesso = """
            INSERT INTO tb_acesso
                (pessoa_tipo, pessoa_id, data_hora_entrada)
            VALUES
                (1, 1, '2026-11-11 10:00:00');
            """;

        await ExecutarAsync(connection, inserirAcesso);

        await Assert.ThrowsAnyAsync<DbException>(
            () => ExecutarAsync(connection, inserirAcesso));

        await ExecutarAsync(connection, """
            UPDATE tb_acesso
            SET data_hora_saida = '2026-11-11 11:00:00'
            WHERE pessoa_tipo = 1
              AND pessoa_id = 1
              AND data_hora_saida IS NULL;
            """);

        await ExecutarAsync(connection, """
            INSERT INTO tb_acesso
                (pessoa_tipo, pessoa_id, data_hora_entrada)
            VALUES
                (1, 1, '2026-11-11 12:00:00');
            """);
    }

    [Fact(DisplayName = "SQLite: rejeita saída anterior à entrada")]
    public async Task Sqlite_Deve_Rejeitar_Saida_Anterior_A_Entrada()
    {
        await using var connection = await CriarBancoSqliteAsync();

        await Assert.ThrowsAnyAsync<DbException>(() => ExecutarAsync(connection, """
            INSERT INTO tb_acesso
                (pessoa_tipo, pessoa_id, data_hora_entrada, data_hora_saida)
            VALUES
                (2, 1, '2026-11-11 10:00:00', '2026-11-11 09:00:00');
            """));
    }

    [Fact(DisplayName = "SQLite: migra tabela legada de acesso preservando dados")]
    public async Task Sqlite_Deve_Migrar_Tabela_Legada_De_Acesso()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"academia_legado_{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = DbProvider.CreateConnection(
                $"Data Source={caminho};Pooling=False;", DatabaseType.Sqlite))
            {
                await connection.OpenAsync();
                await ExecutarAsync(connection, """
                    CREATE TABLE tb_acesso (
                        id_acesso INTEGER PRIMARY KEY AUTOINCREMENT,
                        pessoa_tipo INTEGER NOT NULL,
                        pessoa_id INTEGER NOT NULL,
                        data_hora TEXT NOT NULL
                    );
                    INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora)
                    VALUES (1, 10, '2026-11-10 08:00:00');
                    """);
            }

            await using var repository = new AcademiaDoZe.Infrastructure.Repositories.AcessoAlunoRepository(
                $"Data Source={caminho};Pooling=False;", DatabaseType.Sqlite);
            var acessos = (await repository.ObterTodos()).ToList();

            Assert.Single(acessos);
            Assert.Equal(new DateTime(2026, 11, 10, 8, 0, 0), acessos[0].DataHoraEntrada);
            Assert.Equal(acessos[0].DataHoraEntrada, acessos[0].DataHoraSaida);
        }
        finally
        {
            foreach (var arquivo in new[] { caminho, caminho + "-wal", caminho + "-shm" })
                if (File.Exists(arquivo)) File.Delete(arquivo);
        }
    }

    private static async Task<DbConnection> CriarBancoSqliteAsync()
    {
        var connection = DbProvider.CreateConnection(
            "Data Source=:memory:", DatabaseType.Sqlite);
        await connection.OpenAsync();
        await ExecutarAsync(connection, DbInitializer.ObterScript(DatabaseType.Sqlite));
        return connection;
    }

    private static async Task ExecutarAsync(DbConnection connection, string sql)
    {
        await using var command = DbProvider.CreateCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
