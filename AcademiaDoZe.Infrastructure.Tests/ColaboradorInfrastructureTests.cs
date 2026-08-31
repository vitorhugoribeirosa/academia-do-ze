using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class ColaboradorInfrastructureTests : TestBase
{
    private readonly ColaboradorRepository _repository;
    private readonly LogradouroRepository _logradouroRepository;

    public ColaboradorInfrastructureTests()
    {
        _repository = new ColaboradorRepository(ConnectionString, DatabaseType);
        _logradouroRepository = new LogradouroRepository(ConnectionString, DatabaseType);
    }

    private string SenhaDoBanco => $"Senha{DatabaseType}123";

    private async Task<Colaborador> CriarColaboradorAsync(
        ColaboradorTipo tipo = ColaboradorTipo.Atendente,
        ColaboradorVinculo vinculo = ColaboradorVinculo.CLT,
        string? cpf = null,
        string? email = null)
    {
        var logradouro = Logradouro.Criar(
            0,
            GerarCep(),
            "Rua da Academia",
            "Centro",
            "Lages",
            "SC",
            "Brasil").Value!;
        await _logradouroRepository.Adicionar(logradouro);

        return Colaborador.Criar(
            0,
            "Vitor Hugo Ribeiro Sa",
            cpf ?? GerarCpf(),
            new DateOnly(1995, 5, 20),
            GerarTelefone(),
            email ?? GerarEmail(),
            logradouro,
            "123",
            "Ribeiro Sa",
            SenhaDoBanco,
            Arquivo.Criar([1, 2, 3]).Value!,
            DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            tipo,
            vinculo).Value!;
    }

    private async Task<Colaborador> CriarEInserirColaboradorAsync(
        ColaboradorTipo tipo = ColaboradorTipo.Atendente,
        ColaboradorVinculo vinculo = ColaboradorVinculo.CLT)
    {
        return await _repository.Adicionar(await CriarColaboradorAsync(tipo, vinculo));
    }

    [Fact]
    public async Task Colaborador_Adicionar_E_ObterPorId_Sucesso()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.True(inserido.Id > 0);
        var obtido = await _repository.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal("Vitor Hugo Ribeiro Sa", obtido.Nome);
        Assert.Equal("Ribeiro Sa", obtido.Endereco.Complemento);
        Assert.Equal(SenhaDoBanco, obtido.Senha.Valor);
    }

    [Fact]
    public async Task Colaborador_ObterPorId_RetornaNuloQuandoInexistente()
    {
        Assert.Null(await _repository.ObterPorId(int.MaxValue));
    }

    [Fact]
    public async Task Colaborador_ObterTodos_Sucesso()
    {
        var inserido = await CriarEInserirColaboradorAsync();
        Assert.Contains(await _repository.ObterTodos(), item => item.Id == inserido.Id);
    }

    [Fact]
    public async Task Colaborador_Atualizar_Sucesso()
    {
        var inserido = await CriarEInserirColaboradorAsync();
        var logradouro = await _logradouroRepository.ObterPorId(inserido.Endereco.LogradouroId);
        var atualizado = Colaborador.Criar(
            inserido.Id,
            "Vitor Hugo Ribeiro Sa Atualizado",
            GerarCpf(),
            new DateOnly(1994, 4, 10),
            GerarTelefone(),
            GerarEmail(),
            logradouro!,
            "456",
            "Ribeiro Sa",
            SenhaDoBanco,
            Arquivo.Criar([4, 5, 6]).Value!,
            DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
            ColaboradorTipo.Instrutor,
            ColaboradorVinculo.CLT).Value!;

        await _repository.Atualizar(atualizado);
        var obtido = await _repository.ObterPorId(inserido.Id);

        Assert.NotNull(obtido);
        Assert.Equal("Vitor Hugo Ribeiro Sa Atualizado", obtido.Nome);
        Assert.Equal(ColaboradorTipo.Instrutor, obtido.Tipo);
        Assert.Equal("456", obtido.Endereco.Numero);
    }

    [Fact]
    public async Task Colaborador_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var colaborador = await CriarColaboradorAsync();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(colaborador, int.MaxValue);

        var exception = await Assert.ThrowsAsync<InfrastructureException>(() => _repository.Atualizar(colaborador));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", exception.ErrorCode);
    }

    [Fact]
    public async Task Colaborador_Remover_Sucesso_E_RetornaFalseQuandoInexistente()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.True(await _repository.Remover(inserido.Id));
        Assert.Null(await _repository.ObterPorId(inserido.Id));
        Assert.False(await _repository.Remover(int.MaxValue));
    }

    [Fact]
    public async Task Colaborador_ObterPorCpf_Sucesso_E_Nulo()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.Equal(inserido.Id, (await _repository.ObterPorCpf(inserido.Cpf))!.Id);
        Assert.Null(await _repository.ObterPorCpf(Cpf.Criar(GerarCpf()).Value!));
    }

    [Fact]
    public async Task Colaborador_ObterPorEmail_Sucesso_E_Nulo()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.Equal(inserido.Id, (await _repository.ObterPorEmail(inserido.Email))!.Id);
        Assert.Null(await _repository.ObterPorEmail(Email.Criar(GerarEmail()).Value!));
    }

    [Fact]
    public async Task Colaborador_CpfJaExiste_ValidacaoCorreta()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.True(await _repository.CpfJaExiste(inserido.Cpf));
        Assert.False(await _repository.CpfJaExiste(inserido.Cpf, inserido.Id));
        Assert.False(await _repository.CpfJaExiste(Cpf.Criar(GerarCpf()).Value!));
    }

    [Fact]
    public async Task Colaborador_EmailJaExiste_ValidacaoCorreta()
    {
        var inserido = await CriarEInserirColaboradorAsync();

        Assert.True(await _repository.EmailJaExiste(inserido.Email));
        Assert.False(await _repository.EmailJaExiste(inserido.Email, inserido.Id));
        Assert.False(await _repository.EmailJaExiste(Email.Criar(GerarEmail()).Value!));
    }

    [Fact]
    public async Task Colaborador_ObterPorTipo_FiltragemCorreta()
    {
        var inserido = await CriarEInserirColaboradorAsync(ColaboradorTipo.Instrutor);

        var resultados = await _repository.ObterPorTipo(ColaboradorTipo.Instrutor);
        Assert.Contains(resultados, item => item.Id == inserido.Id);
        Assert.All(resultados, item => Assert.Equal(ColaboradorTipo.Instrutor, item.Tipo));
    }

    [Fact]
    public async Task Colaborador_ObterPorVinculo_FiltragemCorreta()
    {
        var inserido = await CriarEInserirColaboradorAsync(ColaboradorTipo.Atendente, ColaboradorVinculo.Estagio);

        var resultados = await _repository.ObterPorVinculo(ColaboradorVinculo.Estagio);
        Assert.Contains(resultados, item => item.Id == inserido.Id);
        Assert.All(resultados, item => Assert.Equal(ColaboradorVinculo.Estagio, item.Vinculo));
    }

    [Fact]
    public async Task Colaborador_TrocarSenha_Sucesso_E_FalseQuandoInexistente()
    {
        var inserido = await CriarEInserirColaboradorAsync();
        var novaSenha = Senha.Criar($"NovaSenha{DatabaseType}456").Value!;

        Assert.True(await _repository.TrocarSenha(inserido.Id, novaSenha));
        Assert.Equal(novaSenha.Valor, (await _repository.ObterPorId(inserido.Id))!.Senha.Valor);
        Assert.False(await _repository.TrocarSenha(int.MaxValue, novaSenha));
    }
}
