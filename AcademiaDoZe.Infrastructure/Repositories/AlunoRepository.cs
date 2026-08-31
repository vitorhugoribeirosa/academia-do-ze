using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class AlunoRepository : BaseRepository, IAlunoRepository
{
    private const string BaseSelectQuery =
        "SELECT a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email, " +
        "a.logradouro_id, a.numero, a.complemento, a.senha, a.foto, " +
        "l.cep AS logradouro_cep, l.nome AS logradouro_nome, l.bairro AS logradouro_bairro, " +
        "l.cidade AS logradouro_cidade, l.estado AS logradouro_estado, l.pais AS logradouro_pais " +
        "FROM tb_aluno a INNER JOIN tb_logradouro l ON l.id_logradouro = a.logradouro_id ";

    public AlunoRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    public async Task<Aluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE a.id_aluno = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ALUNO_POR_ID", $"Erro ao obter aluno por ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}ORDER BY a.nome", cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS_ALUNOS", "Erro ao obter todos os alunos.", ex);
        }
    }

    public async Task<Aluno> Adicionar(Aluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string insertSql =
                "INSERT INTO tb_aluno (cpf, nome, nascimento, telefone, email, logradouro_id, numero, complemento, " +
                "senha, foto) VALUES (@Cpf, @Nome, @Nascimento, @Telefone, @Email, @LogradouroId, @Numero, " +
                "@Complemento, @Senha, @Foto)";

            await using var command = await CreateCommandAsync(FormatInsertQuery(insertSql), cancellationToken);
            AdicionarParametros(command, entity);

            var id = await command.ExecuteScalarIdAsync(
                "ERRO_ADICIONAR_ALUNO",
                "Falha ao obter ID inserido para o aluno.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ALUNO", "Erro ao adicionar aluno.", ex);
        }
    }

    public async Task<Aluno> Atualizar(Aluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query =
                "UPDATE tb_aluno SET cpf = @Cpf, nome = @Nome, nascimento = @Nascimento, telefone = @Telefone, " +
                "email = @Email, logradouro_id = @LogradouroId, numero = @Numero, complemento = @Complemento, " +
                "senha = @Senha, foto = @Foto WHERE id_aluno = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            AdicionarParametros(command, entity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhum aluno encontrado com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ALUNO", $"Erro ao atualizar aluno ID {entity.Id}.", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "DELETE FROM tb_aluno WHERE id_aluno = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ALUNO", $"Erro ao remover aluno ID {id}.", ex);
        }
    }

    public Task<Aluno?> ObterPorCpf(Cpf cpf, CancellationToken cancellationToken = default) =>
        ObterUnicoAsync("a.cpf = @Valor", cpf.Valor, "ERRO_OBTER_ALUNO_POR_CPF", cancellationToken);

    public Task<Aluno?> ObterPorEmail(Email email, CancellationToken cancellationToken = default) =>
        ObterUnicoAsync("a.email = @Valor", email.Valor, "ERRO_OBTER_ALUNO_POR_EMAIL", cancellationToken);

    public Task<bool> CpfJaExiste(Cpf cpf, int? id = null, CancellationToken cancellationToken = default) =>
        ValorJaExisteAsync("cpf", cpf.Valor, id, "ERRO_VERIFICAR_CPF_ALUNO", cancellationToken);

    public Task<bool> EmailJaExiste(Email email, int? id = null, CancellationToken cancellationToken = default) =>
        ValorJaExisteAsync("email", email.Valor, id, "ERRO_VERIFICAR_EMAIL_ALUNO", cancellationToken);

    public async Task<IEnumerable<Aluno>> ObterPorNome(
        string nome,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comparacao = DatabaseType == DatabaseType.Sqlite
                ? "a.nome LIKE '%' || @Nome || '%' COLLATE NOCASE"
                : "a.nome LIKE CONCAT('%', @Nome, '%')";

            if (DatabaseType == DatabaseType.SqlServer)
                comparacao = "a.nome LIKE '%' + @Nome + '%'";

            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE {comparacao} ORDER BY a.nome", cancellationToken);
            command.AddParameter("@Nome", nome, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ALUNOS_POR_NOME", $"Erro ao obter alunos pelo nome {nome}.", ex);
        }
    }

    public async Task<bool> TrocarSenha(int id, Senha novaSenha, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "UPDATE tb_aluno SET senha = @Senha WHERE id_aluno = @Id", cancellationToken);
            command.AddParameter("@Senha", novaSenha.Valor, DbType.String);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_TROCAR_SENHA_ALUNO", $"Erro ao trocar senha do aluno ID {id}.", ex);
        }
    }

    private async Task<Aluno?> ObterUnicoAsync(
        string filtro,
        string valor,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE {filtro}", cancellationToken);
            command.AddParameter("@Valor", valor, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(errorCode, "Erro ao consultar aluno.", ex);
        }
    }

    private async Task<bool> ValorJaExisteAsync(
        string coluna,
        string valor,
        int? id,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = $"SELECT COUNT(1) FROM tb_aluno WHERE {coluna} = @Valor " +
                "AND (@Id IS NULL OR id_aluno <> @Id)";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Valor", valor, DbType.String);
            command.AddParameter("@Id", id, DbType.Int32);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(errorCode, "Erro ao verificar existencia de dado do aluno.", ex);
        }
    }

    private static Aluno Map(DbDataReader reader)
    {
        try
        {
            var id = reader.GetInt32Value("id_aluno");
            var logradouroResult = Logradouro.Criar(
                reader.GetInt32Value("logradouro_id"),
                reader.GetStringValue("logradouro_cep"),
                reader.GetStringValue("logradouro_nome"),
                reader.GetStringValue("logradouro_bairro"),
                reader.GetStringValue("logradouro_cidade"),
                reader.GetStringValue("logradouro_estado"),
                reader.GetStringValue("logradouro_pais"));

            if (logradouroResult.IsFailure)
                throw CriarErroDominio("logradouro", id, logradouroResult.Notifications.Select(n => n.Mensagem));

            var fotoResult = Arquivo.Criar(reader.GetNullableBytes("foto") ?? []);
            if (fotoResult.IsFailure)
                throw CriarErroDominio("foto", id, fotoResult.Notifications.Select(n => n.Mensagem));

            var result = Aluno.Criar(
                id,
                reader.GetStringValue("nome"),
                reader.GetStringValue("cpf"),
                reader.GetDateOnlyValue("nascimento"),
                reader.GetStringValue("telefone"),
                reader.GetStringValue("email"),
                logradouroResult.Value!,
                reader.GetStringValue("numero"),
                reader.GetNullableString("complemento"),
                reader.GetStringValue("senha"),
                fotoResult.Value!);

            if (result.IsFailure)
                throw CriarErroDominio("aluno", id, result.Notifications.Select(n => n.Mensagem));

            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ALUNO", "Erro ao mapear dados do aluno.", ex);
        }
    }

    private static InfrastructureException CriarErroDominio(
        string origem,
        int id,
        IEnumerable<string> notificacoes)
    {
        return new InfrastructureException(
            "ERRO_DOMINIO_MAPEAMENTO",
            $"Erro de dominio ao mapear {origem} do aluno ID {id}: {string.Join(", ", notificacoes)}");
    }

    private static void AdicionarParametros(DbCommand command, Aluno entity)
    {
        command.AddParameter("@Cpf", entity.Cpf.Valor, DbType.String);
        command.AddParameter("@Nome", entity.Nome, DbType.String);
        command.AddParameter("@Nascimento", entity.DataNascimento, DbType.Date);
        command.AddParameter("@Telefone", entity.Telefone.Valor, DbType.String);
        command.AddParameter("@Email", entity.Email.Valor, DbType.String);
        command.AddParameter("@LogradouroId", entity.Endereco.LogradouroId, DbType.Int32);
        command.AddParameter("@Numero", entity.Endereco.Numero, DbType.String);
        command.AddParameter("@Complemento", entity.Endereco.Complemento, DbType.String);
        command.AddParameter("@Senha", entity.Senha.Valor, DbType.String);
        command.AddParameter("@Foto", entity.Foto.Conteudo, DbType.Binary);
    }

    private static async Task<List<Aluno>> LerTodosAsync(
        DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var alunos = new List<Aluno>();
        while (await reader.ReadAsync(cancellationToken))
            alunos.Add(Map(reader));
        return alunos;
    }
}
