using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class LogradouroRepository : BaseRepository, ILogradouroRepository
{
    private const string BaseSelectQuery =
        "SELECT id_logradouro, cep, nome, bairro, cidade, estado, pais FROM tb_logradouro ";

    public LogradouroRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    public async Task<Logradouro?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync($"{BaseSelectQuery}WHERE id_logradouro = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter logradouro por ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync($"{BaseSelectQuery}ORDER BY nome", cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", "Erro ao obter todos os logradouros.", ex);
        }
    }

    public async Task<Logradouro> Adicionar(Logradouro entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string insertSql =
                "INSERT INTO tb_logradouro (cep, nome, bairro, cidade, estado, pais) " +
                "VALUES (@Cep, @Nome, @Bairro, @Cidade, @Estado, @Pais)";

            await using var command = await CreateCommandAsync(FormatInsertQuery(insertSql), cancellationToken);
            AdicionarParametros(command, entity);

            var id = await command.ExecuteScalarIdAsync(
                "ERRO_ADICIONAR_LOGRADOURO",
                "Falha ao obter ID inserido para o logradouro.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_LOGRADOURO", "Erro ao adicionar logradouro.", ex);
        }
    }

    public async Task<Logradouro> Atualizar(Logradouro entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query =
                "UPDATE tb_logradouro SET cep = @Cep, nome = @Nome, bairro = @Bairro, cidade = @Cidade, " +
                "estado = @Estado, pais = @Pais WHERE id_logradouro = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            AdicionarParametros(command, entity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhum logradouro encontrado com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_LOGRADOURO", $"Erro ao atualizar logradouro ID {entity.Id}.", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "DELETE FROM tb_logradouro WHERE id_logradouro = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_LOGRADOURO", $"Erro ao remover logradouro ID {id}.", ex);
        }
    }

    public async Task<Logradouro?> ObterPorCep(Cep cep, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync($"{BaseSelectQuery}WHERE cep = @Cep", cancellationToken);
            command.AddParameter("@Cep", cep.Valor, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_CEP", $"Erro ao obter logradouro por CEP {cep.Valor}.", ex);
        }
    }

    public async Task<bool> CepJaExiste(Cep cep, int? id = null, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query = "SELECT COUNT(1) FROM tb_logradouro " +
                "WHERE cep = @Cep AND (@Id IS NULL OR id_logradouro <> @Id)";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Cep", cep.Valor, DbType.String);
            command.AddParameter("@Id", id, DbType.Int32);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_VERIFICAR_CEP", "Erro ao verificar existencia de CEP.", ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterPorCidade(string cidade, CancellationToken cancellationToken = default)
    {
        try
        {
            var cidadeComparison = DatabaseType == AcademiaDoZe.Infrastructure.Data.DatabaseType.Sqlite
                ? "cidade COLLATE NOCASE = @Cidade"
                : "cidade = @Cidade";
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE {cidadeComparison} ORDER BY bairro, nome", cancellationToken);
            command.AddParameter("@Cidade", cidade, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_CIDADE", $"Erro ao obter logradouros por cidade {cidade}.", ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterPorBairro(
        string cidade,
        string bairro,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE cidade = @Cidade AND bairro = @Bairro ORDER BY nome", cancellationToken);
            command.AddParameter("@Cidade", cidade, DbType.String);
            command.AddParameter("@Bairro", bairro, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_BAIRRO", $"Erro ao obter logradouros por bairro {bairro}.", ex);
        }
    }

    private static Logradouro Map(DbDataReader reader)
    {
        try
        {
            var id = reader.GetInt32Value("id_logradouro");
            var result = Logradouro.Criar(
                id,
                reader.GetStringValue("cep"),
                reader.GetStringValue("nome"),
                reader.GetStringValue("bairro"),
                reader.GetStringValue("cidade"),
                reader.GetStringValue("estado"),
                reader.GetStringValue("pais"));

            if (result.IsFailure)
            {
                var notifications = string.Join(", ", result.Notifications.Select(notification => notification.Mensagem));
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de dominio ao mapear logradouro ID {id}: {notifications}");
            }

            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_LOGRADOURO", "Erro ao mapear dados do logradouro.", ex);
        }
    }

    private static void AdicionarParametros(DbCommand command, Logradouro entity)
    {
        command.AddParameter("@Cep", entity.Cep.Valor, DbType.String);
        command.AddParameter("@Nome", entity.Nome, DbType.String);
        command.AddParameter("@Bairro", entity.Bairro, DbType.String);
        command.AddParameter("@Cidade", entity.Cidade, DbType.String);
        command.AddParameter("@Estado", entity.Estado, DbType.String);
        command.AddParameter("@Pais", entity.Pais, DbType.String);
    }

    private static async Task<List<Logradouro>> LerTodosAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var logradouros = new List<Logradouro>();
        while (await reader.ReadAsync(cancellationToken))
            logradouros.Add(Map(reader));
        return logradouros;
    }
}
