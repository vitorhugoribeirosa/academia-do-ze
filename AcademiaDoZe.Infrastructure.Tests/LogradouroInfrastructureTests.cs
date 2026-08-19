using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class LogradouroInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _repository;

    public LogradouroInfrastructureTests()
    {
        _repository = new LogradouroRepository(ConnectionString, DatabaseType);
    }

    private static async Task<Logradouro> CriarEInserirLogradouroAsync(LogradouroRepository repository)
    {
        var result = Logradouro.Criar(0, GerarCep(), "Rua de Teste", "Bairro Teste", "Lages", "SC", "Brasil");
        if (result.IsFailure)
            throw new Exception($"Falha ao criar Logradouro: {string.Join(", ", result.Notifications.Select(notification => notification.Mensagem))}");

        return await repository.Adicionar(result.Value!);
    }

    [Fact]
    public async Task Logradouro_Adicionar_E_ObterPorId_Sucesso()
    {
        var cep = GerarCep();
        var logradouro = Logradouro.Criar(0, cep, "Rua das Flores", "Centro", "Lages", "SC", "Brasil").Value!;

        var inserido = await _repository.Adicionar(logradouro);

        Assert.True(inserido.Id > 0);
        Assert.Equal(cep, inserido.Cep.Valor);
        Assert.Equal("Rua das Flores", inserido.Nome);

        var obtido = await _repository.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(inserido.Id, obtido.Id);
        Assert.Equal(cep, obtido.Cep.Valor);
    }

    [Fact]
    public async Task Logradouro_ObterPorId_RetornaNuloQuandoInexistente()
    {
        Assert.Null(await _repository.ObterPorId(999999));
    }

    [Fact]
    public async Task Logradouro_ObterTodos_Sucesso()
    {
        await CriarEInserirLogradouroAsync(_repository);
        Assert.NotEmpty(await _repository.ObterTodos());
    }

    [Fact]
    public async Task Logradouro_Atualizar_Sucesso()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_repository);
        var atualizado = Logradouro.Criar(logradouro.Id, GerarCep(), "Rua Nova", "Bairro Novo", "Florianopolis", "SC", "Brasil").Value!;

        var resultado = await _repository.Atualizar(atualizado);

        Assert.Equal("Rua Nova", resultado.Nome);
        Assert.Equal("Bairro Novo", resultado.Bairro);
        Assert.Equal("Florianopolis", resultado.Cidade);
        Assert.Equal("Rua Nova", (await _repository.ObterPorId(logradouro.Id))!.Nome);
    }

    [Fact]
    public async Task Logradouro_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var inexistente = Logradouro.Criar(999999, GerarCep(), "Rua Fake", "Bairro Fake", "Cidade Fake", "SC", "Brasil").Value!;
        var exception = await Assert.ThrowsAsync<InfrastructureException>(() => _repository.Atualizar(inexistente));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", exception.ErrorCode);
    }

    [Fact]
    public async Task Logradouro_Remover_Sucesso()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_repository);
        Assert.True(await _repository.Remover(logradouro.Id));
        Assert.Null(await _repository.ObterPorId(logradouro.Id));
    }

    [Fact]
    public async Task Logradouro_Remover_RetornaFalseQuandoInexistente()
    {
        Assert.False(await _repository.Remover(999999));
    }

    [Fact]
    public async Task Logradouro_ObterPorCep_SucessoENulo()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_repository);
        var obtido = await _repository.ObterPorCep(logradouro.Cep);
        Assert.NotNull(obtido);
        Assert.Equal(logradouro.Id, obtido.Id);

        var inexistente = Cep.Criar("99999999").Value!;
        Assert.Null(await _repository.ObterPorCep(inexistente));
    }

    [Fact]
    public async Task Logradouro_CepJaExiste_ValidacaoCorreta()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_repository);
        Assert.True(await _repository.CepJaExiste(logradouro.Cep));
        Assert.False(await _repository.CepJaExiste(logradouro.Cep, logradouro.Id));
        Assert.False(await _repository.CepJaExiste(Cep.Criar(GerarCep()).Value!));
    }

    [Fact]
    public async Task Logradouro_ObterPorCidade_FiltragemCorreta()
    {
        var cidade = $"CidadeUnica_{Guid.NewGuid().ToString("N")[..5]}";
        var logradouro = Logradouro.Criar(0, GerarCep(), "Rua X", "Bairro Y", cidade, "SC", "Brasil").Value!;
        await _repository.Adicionar(logradouro);

        var resultados = await _repository.ObterPorCidade(cidade.ToLowerInvariant());
        Assert.Single(resultados);
        Assert.Equal(cidade, resultados.First().Cidade);
        Assert.Empty(await _repository.ObterPorCidade("CidadeInexistente_123"));
    }

    [Fact]
    public async Task Logradouro_ObterPorBairro_FiltragemCorreta()
    {
        var cidade = $"Cidade_{Guid.NewGuid().ToString("N")[..5]}";
        var bairro = $"Bairro_{Guid.NewGuid().ToString("N")[..5]}";
        var logradouro = Logradouro.Criar(0, GerarCep(), "Rua Z", bairro, cidade, "SC", "Brasil").Value!;
        await _repository.Adicionar(logradouro);

        Assert.Single(await _repository.ObterPorBairro(cidade, bairro));
        Assert.Empty(await _repository.ObterPorBairro(cidade, "BairroInexistente"));
    }
}
