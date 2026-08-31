using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class ColaboradorRepository : BaseRepository, IColaboradorRepository
{
    private const string BaseSelectQuery =
        "SELECT c.id_colaborador, c.cpf, c.nome, c.nascimento, c.telefone, c.email, " +
        "c.logradouro_id, c.numero, c.complemento, c.senha, c.foto, c.admissao, c.tipo, c.vinculo, " +
        "l.cep AS logradouro_cep, l.nome AS logradouro_nome, l.bairro AS logradouro_bairro, " +
        "l.cidade AS logradouro_cidade, l.estado AS logradouro_estado, l.pais AS logradouro_pais " +
        "FROM tb_colaborador c INNER JOIN tb_logradouro l ON l.id_logradouro = c.logradouro_id ";

    public ColaboradorRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    public async Task<Colaborador?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE c.id_colaborador = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_COLABORADOR_POR_ID", $"Erro ao obter colaborador por ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<Colaborador>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}ORDER BY c.nome", cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS_COLABORADORES", "Erro ao obter todos os colaboradores.", ex);
        }
    }

    public async Task<Colaborador> Adicionar(Colaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string insertSql =
                "INSERT INTO tb_colaborador (cpf, nome, nascimento, telefone, email, logradouro_id, numero, " +
                "complemento, senha, foto, admissao, tipo, vinculo) VALUES (@Cpf, @Nome, @Nascimento, @Telefone, " +
                "@Email, @LogradouroId, @Numero, @Complemento, @Senha, @Foto, @Admissao, @Tipo, @Vinculo)";

            await using var command = await CreateCommandAsync(FormatInsertQuery(insertSql), cancellationToken);
            AdicionarParametros(command, entity);

            var id = await command.ExecuteScalarIdAsync(
                "ERRO_ADICIONAR_COLABORADOR",
                "Falha ao obter ID inserido para o colaborador.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_COLABORADOR", "Erro ao adicionar colaborador.", ex);
        }
    }

    public async Task<Colaborador> Atualizar(Colaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query =
                "UPDATE tb_colaborador SET cpf = @Cpf, nome = @Nome, nascimento = @Nascimento, telefone = @Telefone, " +
                "email = @Email, logradouro_id = @LogradouroId, numero = @Numero, complemento = @Complemento, " +
                "senha = @Senha, foto = @Foto, admissao = @Admissao, tipo = @Tipo, vinculo = @Vinculo " +
                "WHERE id_colaborador = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            AdicionarParametros(command, entity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhum colaborador encontrado com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_COLABORADOR", $"Erro ao atualizar colaborador ID {entity.Id}.", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "DELETE FROM tb_colaborador WHERE id_colaborador = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_COLABORADOR", $"Erro ao remover colaborador ID {id}.", ex);
        }
    }

    public Task<Colaborador?> ObterPorCpf(Cpf cpf, CancellationToken cancellationToken = default) =>
        ObterUnicoAsync("c.cpf = @Valor", "@Valor", cpf.Valor, "ERRO_OBTER_COLABORADOR_POR_CPF", cancellationToken);

    public Task<Colaborador?> ObterPorEmail(Email email, CancellationToken cancellationToken = default) =>
        ObterUnicoAsync("c.email = @Valor", "@Valor", email.Valor, "ERRO_OBTER_COLABORADOR_POR_EMAIL", cancellationToken);

    public Task<bool> CpfJaExiste(Cpf cpf, int? id = null, CancellationToken cancellationToken = default) =>
        ValorJaExisteAsync("cpf", cpf.Valor, id, "ERRO_VERIFICAR_CPF_COLABORADOR", cancellationToken);

    public Task<bool> EmailJaExiste(Email email, int? id = null, CancellationToken cancellationToken = default) =>
        ValorJaExisteAsync("email", email.Valor, id, "ERRO_VERIFICAR_EMAIL_COLABORADOR", cancellationToken);

    public async Task<IEnumerable<Colaborador>> ObterPorTipo(
        ColaboradorTipo tipo,
        CancellationToken cancellationToken = default)
    {
        return await ObterPorFiltroAsync("c.tipo", (int)tipo, "ERRO_OBTER_COLABORADORES_POR_TIPO", cancellationToken);
    }

    public async Task<IEnumerable<Colaborador>> ObterPorVinculo(
        ColaboradorVinculo vinculo,
        CancellationToken cancellationToken = default)
    {
        return await ObterPorFiltroAsync("c.vinculo", (int)vinculo, "ERRO_OBTER_COLABORADORES_POR_VINCULO", cancellationToken);
    }

    public async Task<bool> TrocarSenha(int id, Senha novaSenha, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "UPDATE tb_colaborador SET senha = @Senha WHERE id_colaborador = @Id", cancellationToken);
            command.AddParameter("@Senha", novaSenha.Valor, DbType.String);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_TROCAR_SENHA_COLABORADOR", $"Erro ao trocar senha do colaborador ID {id}.", ex);
        }
    }

    private async Task<Colaborador?> ObterUnicoAsync(
        string filtro,
        string parametro,
        string valor,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE {filtro}", cancellationToken);
            command.AddParameter(parametro, valor, DbType.String);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(errorCode, "Erro ao consultar colaborador.", ex);
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
            var query = $"SELECT COUNT(1) FROM tb_colaborador WHERE {coluna} = @Valor " +
                "AND (@Id IS NULL OR id_colaborador <> @Id)";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Valor", valor, DbType.String);
            command.AddParameter("@Id", id, DbType.Int32);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(errorCode, "Erro ao verificar existencia de dado do colaborador.", ex);
        }
    }

    private async Task<IEnumerable<Colaborador>> ObterPorFiltroAsync(
        string coluna,
        int valor,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE {coluna} = @Valor ORDER BY c.nome", cancellationToken);
            command.AddParameter("@Valor", valor, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(errorCode, "Erro ao filtrar colaboradores.", ex);
        }
    }

    private static Colaborador Map(DbDataReader reader)
    {
        try
        {
            var id = reader.GetInt32Value("id_colaborador");
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

            var result = Colaborador.Criar(
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
                fotoResult.Value!,
                reader.GetDateOnlyValue("admissao"),
                (ColaboradorTipo)reader.GetInt32Value("tipo"),
                (ColaboradorVinculo)reader.GetInt32Value("vinculo"));

            if (result.IsFailure)
                throw CriarErroDominio("colaborador", id, result.Notifications.Select(n => n.Mensagem));

            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_COLABORADOR", "Erro ao mapear dados do colaborador.", ex);
        }
    }

    private static InfrastructureException CriarErroDominio(
        string origem,
        int id,
        IEnumerable<string> notificacoes)
    {
        return new InfrastructureException(
            "ERRO_DOMINIO_MAPEAMENTO",
            $"Erro de dominio ao mapear {origem} do colaborador ID {id}: {string.Join(", ", notificacoes)}");
    }

    private static void AdicionarParametros(DbCommand command, Colaborador entity)
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
        command.AddParameter("@Admissao", entity.DataAdmissao, DbType.Date);
        command.AddParameter("@Tipo", (int)entity.Tipo, DbType.Int32);
        command.AddParameter("@Vinculo", (int)entity.Vinculo, DbType.Int32);
    }

    private static async Task<List<Colaborador>> LerTodosAsync(
        DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var colaboradores = new List<Colaborador>();
        while (await reader.ReadAsync(cancellationToken))
            colaboradores.Add(Map(reader));
        return colaboradores;
    }
}
