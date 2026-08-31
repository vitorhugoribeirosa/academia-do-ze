using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AlunoInfrastructureTests : TestBase
{
    private readonly AlunoRepository _repository;
    private readonly LogradouroRepository _logradouroRepository;

    public AlunoInfrastructureTests()
    {
        _repository = new AlunoRepository(ConnectionString, DatabaseType);
        _logradouroRepository = new LogradouroRepository(ConnectionString, DatabaseType);
    }

    private string SenhaDoBanco => $"Senha{DatabaseType}123";

    private async Task<Aluno> CriarAlunoAsync(string? cpf = null, string? email = null, string? nome = null)
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

        return Aluno.Criar(
            0,
            nome ?? "Vitor Hugo Ribeiro Sa",
            cpf ?? GerarCpf(),
            new DateOnly(1998, 8, 15),
            GerarTelefone(),
            email ?? GerarEmail(),
            logradouro,
            "123",
            "Ribeiro Sa",
            SenhaDoBanco,
            Arquivo.Criar([1, 2, 3]).Value!).Value!;
    }

    private async Task<Aluno> CriarEInserirAlunoAsync(string? nome = null)
    {
        return await _repository.Adicionar(await CriarAlunoAsync(nome: nome));
    }

    [Fact]
    public async Task Aluno_Adicionar_E_ObterPorId_Sucesso()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.True(inserido.Id > 0);
        var obtido = await _repository.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal("Vitor Hugo Ribeiro Sa", obtido.Nome);
        Assert.Equal("Ribeiro Sa", obtido.Endereco.Complemento);
        Assert.Equal(SenhaDoBanco, obtido.Senha.Valor);
    }

    [Fact]
    public async Task Aluno_ObterPorId_RetornaNuloQuandoInexistente()
    {
        Assert.Null(await _repository.ObterPorId(int.MaxValue));
    }

    [Fact]
    public async Task Aluno_ObterTodos_Sucesso()
    {
        var inserido = await CriarEInserirAlunoAsync();
        Assert.Contains(await _repository.ObterTodos(), item => item.Id == inserido.Id);
    }

    [Fact]
    public async Task Aluno_Atualizar_Sucesso()
    {
        var inserido = await CriarEInserirAlunoAsync();
        var logradouro = await _logradouroRepository.ObterPorId(inserido.Endereco.LogradouroId);
        var atualizado = Aluno.Criar(
            inserido.Id,
            "Vitor Hugo Ribeiro Sa Atualizado",
            GerarCpf(),
            new DateOnly(1997, 7, 10),
            GerarTelefone(),
            GerarEmail(),
            logradouro!,
            "456",
            "Ribeiro Sa",
            SenhaDoBanco,
            Arquivo.Criar([4, 5, 6]).Value!).Value!;

        await _repository.Atualizar(atualizado);
        var obtido = await _repository.ObterPorId(inserido.Id);

        Assert.NotNull(obtido);
        Assert.Equal("Vitor Hugo Ribeiro Sa Atualizado", obtido.Nome);
        Assert.Equal("456", obtido.Endereco.Numero);
        Assert.Equal("Ribeiro Sa", obtido.Endereco.Complemento);
    }

    [Fact]
    public async Task Aluno_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var aluno = await CriarAlunoAsync();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(aluno, int.MaxValue);

        var exception = await Assert.ThrowsAsync<InfrastructureException>(() => _repository.Atualizar(aluno));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", exception.ErrorCode);
    }

    [Fact]
    public async Task Aluno_Remover_Sucesso_E_RetornaFalseQuandoInexistente()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.True(await _repository.Remover(inserido.Id));
        Assert.Null(await _repository.ObterPorId(inserido.Id));
        Assert.False(await _repository.Remover(int.MaxValue));
    }

    [Fact]
    public async Task Aluno_ObterPorCpf_Sucesso_E_Nulo()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.Equal(inserido.Id, (await _repository.ObterPorCpf(inserido.Cpf))!.Id);
        Assert.Null(await _repository.ObterPorCpf(Cpf.Criar(GerarCpf()).Value!));
    }

    [Fact]
    public async Task Aluno_ObterPorEmail_Sucesso_E_Nulo()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.Equal(inserido.Id, (await _repository.ObterPorEmail(inserido.Email))!.Id);
        Assert.Null(await _repository.ObterPorEmail(Email.Criar(GerarEmail()).Value!));
    }

    [Fact]
    public async Task Aluno_CpfJaExiste_ValidacaoCorreta()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.True(await _repository.CpfJaExiste(inserido.Cpf));
        Assert.False(await _repository.CpfJaExiste(inserido.Cpf, inserido.Id));
        Assert.False(await _repository.CpfJaExiste(Cpf.Criar(GerarCpf()).Value!));
    }

    [Fact]
    public async Task Aluno_EmailJaExiste_ValidacaoCorreta()
    {
        var inserido = await CriarEInserirAlunoAsync();

        Assert.True(await _repository.EmailJaExiste(inserido.Email));
        Assert.False(await _repository.EmailJaExiste(inserido.Email, inserido.Id));
        Assert.False(await _repository.EmailJaExiste(Email.Criar(GerarEmail()).Value!));
    }

    [Fact]
    public async Task Aluno_ObterPorNome_FiltragemCorreta()
    {
        var identificador = Guid.NewGuid().ToString("N")[..6];
        var nome = $"Vitor Hugo Ribeiro Sa {identificador}";
        var inserido = await CriarEInserirAlunoAsync(nome);

        var resultados = await _repository.ObterPorNome(identificador.ToLowerInvariant());
        Assert.Single(resultados);
        Assert.Equal(inserido.Id, resultados.First().Id);
        Assert.Empty(await _repository.ObterPorNome("NomeInexistente_999999"));
    }

    [Fact]
    public async Task Aluno_TrocarSenha_Sucesso_E_FalseQuandoInexistente()
    {
        var inserido = await CriarEInserirAlunoAsync();
        var novaSenha = Senha.Criar($"NovaSenha{DatabaseType}456").Value!;

        Assert.True(await _repository.TrocarSenha(inserido.Id, novaSenha));
        Assert.Equal(novaSenha.Valor, (await _repository.ObterPorId(inserido.Id))!.Senha.Valor);
        Assert.False(await _repository.TrocarSenha(int.MaxValue, novaSenha));
    }
}
