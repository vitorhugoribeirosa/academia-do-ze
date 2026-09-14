using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class MatriculaInfrastructureTests : TestBase
{
    private readonly MatriculaRepository _repository;
    private readonly AlunoRepository _alunoRepository;
    private readonly LogradouroRepository _logradouroRepository;

    public MatriculaInfrastructureTests()
    {
        _repository = new MatriculaRepository(ConnectionString, DatabaseType);
        _alunoRepository = new AlunoRepository(ConnectionString, DatabaseType);
        _logradouroRepository = new LogradouroRepository(ConnectionString, DatabaseType);
    }

    private string SenhaDoBanco => $"Senha{DatabaseType}123";
    private const string NomeAluno = "Vitor Hugo Ribeiro Sa";
    private string SiglaBanco => DatabaseType switch
    {
        AcademiaDoZe.Infrastructure.Data.DatabaseType.SqlServer => "SQLServer",
        AcademiaDoZe.Infrastructure.Data.DatabaseType.MySql => "MySQL",
        AcademiaDoZe.Infrastructure.Data.DatabaseType.Sqlite => "SQLite",
        _ => throw new ArgumentOutOfRangeException()
    };

    private async Task<Aluno> CriarEInserirAlunoAsync()
    {
        var logradouro = Logradouro.Criar(
            0,
            GerarCep(),
            "Rua da Matricula",
            "Centro",
            "Lages",
            "SC",
            "Brasil").Value!;
        await _logradouroRepository.Adicionar(logradouro);

        var aluno = Aluno.Criar(
            0,
            NomeAluno,
            GerarCpf(),
            new DateOnly(1998, 8, 15),
            GerarTelefone(),
            GerarEmail(),
            logradouro,
            "123",
            "Teste",
            SenhaDoBanco,
            Arquivo.Criar([1, 2, 3]).Value!).Value!;

        return await _alunoRepository.Adicionar(aluno);
    }

    private Matricula CriarMatricula(
        Aluno aluno,
        int id = 0,
        MatriculaPlano plano = MatriculaPlano.Mensal,
        DateOnly? dataInicio = null,
        MatriculaRestricoes restricoes = MatriculaRestricoes.None,
        Arquivo? laudoMedico = null,
        string? observacoesRestricoes = null)
    {
        return Matricula.Criar(
            id,
            aluno,
            plano,
            dataInicio ?? DateOnly.FromDateTime(DateTime.Today),
            NomeAluno,
            restricoes,
            laudoMedico,
            observacoesRestricoes ?? SiglaBanco).Value!;
    }

    private async Task<(Aluno Aluno, Matricula Matricula)> CriarEInserirMatriculaAsync(
        MatriculaPlano plano = MatriculaPlano.Mensal,
        DateOnly? dataInicio = null)
    {
        var aluno = await CriarEInserirAlunoAsync();
        var matricula = CriarMatricula(aluno, plano: plano, dataInicio: dataInicio);
        return (aluno, await _repository.Adicionar(matricula));
    }

    [Fact]
    public async Task Matricula_Adicionar_E_ObterPorId_Sucesso()
    {
        var (aluno, inserida) = await CriarEInserirMatriculaAsync();

        Assert.True(inserida.Id > 0);
        var obtida = await _repository.ObterPorId(inserida.Id);
        Assert.NotNull(obtida);
        Assert.Equal(aluno.Id, obtida.AlunoId);
        Assert.Equal(MatriculaPlano.Mensal, obtida.Plano);
        Assert.Equal(NomeAluno, obtida.Objetivo);
        Assert.Equal("SQLite", obtida.ObservacoesRestricoes);
    }

    [Fact]
    public async Task Matricula_ObterPorId_RetornaNuloQuandoInexistente()
    {
        Assert.Null(await _repository.ObterPorId(int.MaxValue));
    }

    [Fact]
    public async Task Matricula_ObterTodos_Sucesso()
    {
        var (_, inserida) = await CriarEInserirMatriculaAsync();
        var matriculas = (await _repository.ObterTodos()).ToList();

        foreach (var item in matriculas)
        {
            var aluno = await _alunoRepository.ObterPorId(item.AlunoId);
            Assert.NotNull(aluno);

            var possuiRestricao = item.RestricoesMedicas != MatriculaRestricoes.None;
            var observacoes = possuiRestricao
                ? "Treino adaptado conforme avaliacao de saude"
                : SiglaBanco;

            var atualizada = Matricula.Criar(
                item.Id,
                aluno,
                item.Plano,
                item.DataInicio,
                item.Objetivo,
                item.RestricoesMedicas,
                possuiRestricao ? item.LaudoMedico ?? Arquivo.Criar([1, 2, 3]).Value! : null,
                observacoes).Value!;

            await _repository.Atualizar(atualizada);
        }

        Assert.Contains(matriculas, item => item.Id == inserida.Id);
    }

    [Fact]
    public async Task Matricula_Atualizar_Sucesso()
    {
        var (aluno, inserida) = await CriarEInserirMatriculaAsync();
        var laudo = Arquivo.Criar([4, 5, 6]).Value!;
        var atualizada = CriarMatricula(
            aluno,
            inserida.Id,
            MatriculaPlano.Semestral,
            DateOnly.FromDateTime(DateTime.Today),
            MatriculaRestricoes.PressaoAlta,
            laudo,
            "Monitorar pressao antes e depois do treino");

        await _repository.Atualizar(atualizada);
        var obtida = await _repository.ObterPorId(inserida.Id);

        Assert.NotNull(obtida);
        Assert.Equal(MatriculaPlano.Semestral, obtida.Plano);
        Assert.Equal(NomeAluno, obtida.Objetivo);
        Assert.Equal(MatriculaRestricoes.PressaoAlta, obtida.RestricoesMedicas);
        Assert.Equal("Monitorar pressao antes e depois do treino", obtida.ObservacoesRestricoes);
        Assert.Equal(laudo.Conteudo, obtida.LaudoMedico!.Conteudo);
    }

    [Fact]
    public async Task Matricula_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var aluno = await CriarEInserirAlunoAsync();
        var matricula = CriarMatricula(aluno, id: int.MaxValue);

        var exception = await Assert.ThrowsAsync<InfrastructureException>(
            () => _repository.Atualizar(matricula));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", exception.ErrorCode);
    }

    [Fact]
    public async Task Matricula_Remover_Sucesso_E_RetornaFalseQuandoInexistente()
    {
        var (_, inserida) = await CriarEInserirMatriculaAsync();

        Assert.True(await _repository.Remover(inserida.Id));
        Assert.Null(await _repository.ObterPorId(inserida.Id));
        Assert.False(await _repository.Remover(int.MaxValue));
    }

    [Fact]
    public async Task Matricula_ObterPorAluno_FiltragemCorreta()
    {
        var (aluno, inserida) = await CriarEInserirMatriculaAsync();

        var resultados = await _repository.ObterPorAluno(aluno.Id);
        Assert.Contains(resultados, item => item.Id == inserida.Id);
        Assert.All(resultados, item => Assert.Equal(aluno.Id, item.AlunoId));
        Assert.Empty(await _repository.ObterPorAluno(int.MaxValue));
    }

    [Fact]
    public async Task Matricula_ObterMatriculaAtivaPorAluno_Sucesso_E_Nulo()
    {
        var (aluno, inserida) = await CriarEInserirMatriculaAsync(
            MatriculaPlano.Anual,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        Assert.Equal(inserida.Id, (await _repository.ObterMatriculaAtivaPorAluno(aluno.Id))!.Id);
        Assert.Null(await _repository.ObterMatriculaAtivaPorAluno(int.MaxValue));
    }

    [Fact]
    public async Task Matricula_PossuiMatriculaAtiva_ValidacaoCorreta()
    {
        var (aluno, _) = await CriarEInserirMatriculaAsync(
            MatriculaPlano.Anual,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        Assert.True(await _repository.PossuiMatriculaAtiva(aluno.Id));
        Assert.False(await _repository.PossuiMatriculaAtiva(int.MaxValue));
    }

    [Fact]
    public async Task Matricula_ObterAtivas_ComESemFiltroPorAluno()
    {
        var (aluno, inserida) = await CriarEInserirMatriculaAsync(
            MatriculaPlano.Anual,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        Assert.Contains(await _repository.ObterAtivas(), item => item.Id == inserida.Id);
        var filtradas = await _repository.ObterAtivas(aluno.Id);
        Assert.Contains(filtradas, item => item.Id == inserida.Id);
        Assert.All(filtradas, item => Assert.Equal(aluno.Id, item.AlunoId));
    }

    [Fact]
    public async Task Matricula_ObterVencendoEmDias_FiltragemCorreta()
    {
        var dataInicio = DateOnly.FromDateTime(DateTime.Today).AddMonths(-1).AddDays(5);
        var (_, inserida) = await CriarEInserirMatriculaAsync(MatriculaPlano.Mensal, dataInicio);

        var resultados = await _repository.ObterVencendoEmDias(5);
        Assert.Contains(resultados, item => item.Id == inserida.Id);
        Assert.All(resultados, item =>
            Assert.InRange(item.DataFim, DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today.AddDays(5))));
    }

    [Fact]
    public async Task Matricula_ObterPorPlano_FiltragemCorreta()
    {
        var (_, inserida) = await CriarEInserirMatriculaAsync(MatriculaPlano.Trimestral);

        var resultados = await _repository.ObterPorPlano(MatriculaPlano.Trimestral);
        Assert.Contains(resultados, item => item.Id == inserida.Id);
        Assert.All(resultados, item => Assert.Equal(MatriculaPlano.Trimestral, item.Plano));
    }

    [Fact]
    public async Task Matricula_RestricoesMedicas_ComVariacoesMultiplaEscolha_PersisteEObtemCorretamente()
    {
        var aluno = await CriarEInserirAlunoAsync();
        var restricoes = MatriculaRestricoes.Diabetes |
            MatriculaRestricoes.PressaoAlta |
            MatriculaRestricoes.RemedioContinuo;
        var laudo = Arquivo.Criar([7, 8, 9]).Value!;
        var matricula = CriarMatricula(
            aluno,
            plano: MatriculaPlano.Anual,
            restricoes: restricoes,
            laudoMedico: laudo,
            observacoesRestricoes: "Acompanhamento clinico e intensidade moderada");

        var inserida = await _repository.Adicionar(matricula);
        var obtida = await _repository.ObterPorId(inserida.Id);

        Assert.NotNull(obtida);
        Assert.Equal(restricoes, obtida.RestricoesMedicas);
        Assert.True(obtida.RestricoesMedicas.HasFlag(MatriculaRestricoes.Diabetes));
        Assert.True(obtida.RestricoesMedicas.HasFlag(MatriculaRestricoes.PressaoAlta));
        Assert.True(obtida.RestricoesMedicas.HasFlag(MatriculaRestricoes.RemedioContinuo));
        Assert.Equal(NomeAluno, obtida.Objetivo);
        Assert.Equal("Acompanhamento clinico e intensidade moderada", obtida.ObservacoesRestricoes);
        Assert.Equal(laudo.Conteudo, obtida.LaudoMedico!.Conteudo);
    }
}
