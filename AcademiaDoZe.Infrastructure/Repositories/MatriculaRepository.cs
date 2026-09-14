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

public class MatriculaRepository : BaseRepository, IMatriculaRepository
{
    private const string BaseSelectQuery =
        "SELECT m.id_matricula, m.aluno_id, m.plano, m.data_inicio, m.data_fim, " +
        "m.objetivo, m.restricao_medica, m.obs_restricao, m.laudo_medico, " +
        "a.id_aluno, a.cpf, a.nome AS aluno_nome, a.nascimento, a.telefone, " +
        "a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto, " +
        "l.cep AS logradouro_cep, l.nome AS logradouro_nome, " +
        "l.bairro AS logradouro_bairro, l.cidade AS logradouro_cidade, " +
        "l.estado AS logradouro_estado, l.pais AS logradouro_pais " +
        "FROM tb_matricula m " +
        "INNER JOIN tb_aluno a ON a.id_aluno = m.aluno_id " +
        "INNER JOIN tb_logradouro l ON l.id_logradouro = a.logradouro_id ";

    public MatriculaRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    public async Task<Matricula?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.id_matricula = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULA_POR_ID",
                $"Erro ao obter matricula por ID {id}.",
                ex);
        }
    }

    public async Task<IEnumerable<Matricula>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}ORDER BY m.data_inicio DESC", cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_TODAS_MATRICULAS",
                "Erro ao obter todas as matriculas.",
                ex);
        }
    }

    public async Task<Matricula> Adicionar(Matricula entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string insertSql =
                "INSERT INTO tb_matricula (aluno_id, plano, data_inicio, data_fim, objetivo, " +
                "restricao_medica, obs_restricao, laudo_medico) " +
                "VALUES (@AlunoId, @Plano, @DataInicio, @DataFim, @Objetivo, " +
                "@RestricaoMedica, @ObsRestricao, @LaudoMedico)";

            await using var command = await CreateCommandAsync(
                FormatInsertQuery(insertSql), cancellationToken);
            AdicionarParametros(command, entity);

            var id = await command.ExecuteScalarIdAsync(
                "ERRO_ADICIONAR_MATRICULA",
                "Falha ao obter ID inserido para a matricula.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty(
                nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_ADICIONAR_MATRICULA",
                "Erro ao adicionar matricula.",
                ex);
        }
    }

    public async Task<Matricula> Atualizar(Matricula entity, CancellationToken cancellationToken = default)
    {
        try
        {
            const string query =
                "UPDATE tb_matricula SET aluno_id = @AlunoId, plano = @Plano, " +
                "data_inicio = @DataInicio, data_fim = @DataFim, objetivo = @Objetivo, " +
                "restricao_medica = @RestricaoMedica, obs_restricao = @ObsRestricao, " +
                "laudo_medico = @LaudoMedico WHERE id_matricula = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            AdicionarParametros(command, entity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhuma matricula encontrada com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_ATUALIZAR_MATRICULA",
                $"Erro ao atualizar matricula ID {entity.Id}.",
                ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                "DELETE FROM tb_matricula WHERE id_matricula = @Id", cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_REMOVER_MATRICULA",
                $"Erro ao remover matricula ID {id}.",
                ex);
        }
    }

    public async Task<IEnumerable<Matricula>> ObterPorAluno(
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.aluno_id = @AlunoId ORDER BY m.data_inicio DESC",
                cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULAS_POR_ALUNO",
                $"Erro ao obter matriculas do aluno ID {alunoId}.",
                ex);
        }
    }

    public async Task<Matricula?> ObterMatriculaAtivaPorAluno(
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataAtual = GetCurrentDateFunction();
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.aluno_id = @AlunoId " +
                $"AND m.data_inicio <= {dataAtual} AND m.data_fim >= {dataAtual} " +
                "ORDER BY m.data_fim DESC",
                cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULA_ATIVA_POR_ALUNO",
                $"Erro ao obter matricula ativa do aluno ID {alunoId}.",
                ex);
        }
    }

    public async Task<bool> PossuiMatriculaAtiva(
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataAtual = GetCurrentDateFunction();
            await using var command = await CreateCommandAsync(
                "SELECT COUNT(1) FROM tb_matricula WHERE aluno_id = @AlunoId " +
                $"AND data_inicio <= {dataAtual} AND data_fim >= {dataAtual}",
                cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_VERIFICAR_MATRICULA_ATIVA",
                $"Erro ao verificar matricula ativa do aluno ID {alunoId}.",
                ex);
        }
    }

    public async Task<IEnumerable<Matricula>> ObterAtivas(
        int alunoId = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataAtual = GetCurrentDateFunction();
            var filtroAluno = alunoId > 0 ? "AND m.aluno_id = @AlunoId " : string.Empty;
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.data_inicio <= {dataAtual} " +
                $"AND m.data_fim >= {dataAtual} {filtroAluno}ORDER BY m.data_fim",
                cancellationToken);

            if (alunoId > 0)
                command.AddParameter("@AlunoId", alunoId, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULAS_ATIVAS",
                "Erro ao obter matriculas ativas.",
                ex);
        }
    }

    public async Task<IEnumerable<Matricula>> ObterVencendoEmDias(
        int dias,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataAtual = GetCurrentDateFunction();
            var dataLimite = GetDateAddDaysExpression(dataAtual, "@Dias");
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.data_fim BETWEEN {dataAtual} AND {dataLimite} " +
                "ORDER BY m.data_fim",
                cancellationToken);
            command.AddParameter("@Dias", dias, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULAS_VENCENDO",
                $"Erro ao obter matriculas vencendo em {dias} dias.",
                ex);
        }
    }

    public async Task<IEnumerable<Matricula>> ObterPorPlano(
        MatriculaPlano plano,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = await CreateCommandAsync(
                $"{BaseSelectQuery}WHERE m.plano = @Plano ORDER BY m.data_inicio DESC",
                cancellationToken);
            command.AddParameter("@Plano", (int)plano, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await LerTodosAsync(reader, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_MATRICULAS_POR_PLANO",
                $"Erro ao obter matriculas do plano {plano}.",
                ex);
        }
    }

    private static Matricula Map(DbDataReader reader)
    {
        try
        {
            var matriculaId = reader.GetInt32Value("id_matricula");
            var alunoId = reader.GetInt32Value("id_aluno");

            var logradouroResult = Logradouro.Criar(
                reader.GetInt32Value("logradouro_id"),
                reader.GetStringValue("logradouro_cep"),
                reader.GetStringValue("logradouro_nome"),
                reader.GetStringValue("logradouro_bairro"),
                reader.GetStringValue("logradouro_cidade"),
                reader.GetStringValue("logradouro_estado"),
                reader.GetStringValue("logradouro_pais"));

            if (logradouroResult.IsFailure)
                throw CriarErroDominio("logradouro", matriculaId, logradouroResult.Notifications.Select(n => n.Mensagem));

            var fotoResult = Arquivo.Criar(reader.GetNullableBytes("foto") ?? []);
            if (fotoResult.IsFailure)
                throw CriarErroDominio("foto do aluno", matriculaId, fotoResult.Notifications.Select(n => n.Mensagem));

            var alunoResult = Aluno.Criar(
                alunoId,
                reader.GetStringValue("aluno_nome"),
                reader.GetStringValue("cpf"),
                reader.GetDateOnlyValue("nascimento"),
                reader.GetStringValue("telefone"),
                reader.GetStringValue("email"),
                logradouroResult.Value!,
                reader.GetStringValue("numero"),
                reader.GetNullableString("complemento"),
                reader.GetStringValue("senha"),
                fotoResult.Value!);

            if (alunoResult.IsFailure)
                throw CriarErroDominio("aluno", matriculaId, alunoResult.Notifications.Select(n => n.Mensagem));

            var laudoBytes = reader.GetNullableBytes("laudo_medico");
            Arquivo? laudoMedico = null;
            if (laudoBytes is not null)
            {
                var laudoResult = Arquivo.Criar(laudoBytes);
                if (laudoResult.IsFailure)
                    throw CriarErroDominio("laudo medico", matriculaId, laudoResult.Notifications.Select(n => n.Mensagem));
                laudoMedico = laudoResult.Value;
            }

            var matriculaResult = Matricula.Criar(
                matriculaId,
                alunoResult.Value!,
                (MatriculaPlano)reader.GetInt32Value("plano"),
                reader.GetDateOnlyValue("data_inicio"),
                reader.GetStringValue("objetivo"),
                (MatriculaRestricoes)reader.GetInt32Value("restricao_medica"),
                laudoMedico,
                reader.GetNullableString("obs_restricao"));

            if (matriculaResult.IsFailure)
                throw CriarErroDominio("matricula", matriculaId, matriculaResult.Notifications.Select(n => n.Mensagem));

            return matriculaResult.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException(
                "ERRO_MAPEAMENTO_MATRICULA",
                "Erro ao mapear dados da matricula.",
                ex);
        }
    }

    private static InfrastructureException CriarErroDominio(
        string origem,
        int id,
        IEnumerable<string> notificacoes)
    {
        return new InfrastructureException(
            "ERRO_DOMINIO_MAPEAMENTO",
            $"Erro de dominio ao mapear {origem} da matricula ID {id}: {string.Join(", ", notificacoes)}");
    }

    private static void AdicionarParametros(DbCommand command, Matricula entity)
    {
        command.AddParameter("@AlunoId", entity.AlunoId, DbType.Int32);
        command.AddParameter("@Plano", (int)entity.Plano, DbType.Int32);
        command.AddParameter("@DataInicio", entity.DataInicio, DbType.Date);
        command.AddParameter("@DataFim", entity.DataFim, DbType.Date);
        command.AddParameter("@Objetivo", entity.Objetivo, DbType.String);
        command.AddParameter("@RestricaoMedica", (int)entity.RestricoesMedicas, DbType.Int32);
        command.AddParameter("@ObsRestricao", entity.ObservacoesRestricoes, DbType.String);
        command.AddParameter("@LaudoMedico", entity.LaudoMedico?.Conteudo, DbType.Binary);
    }

    private static async Task<List<Matricula>> LerTodosAsync(
        DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var matriculas = new List<Matricula>();
        while (await reader.ReadAsync(cancellationToken))
            matriculas.Add(Map(reader));
        return matriculas;
    }
}
