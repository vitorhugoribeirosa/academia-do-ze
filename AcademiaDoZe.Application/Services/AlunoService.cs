using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Application.Security;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Services;

public class AlunoService : IAlunoService
{
    private readonly Func<IAlunoRepository> _repoFactory;
    private readonly Func<ILogradouroRepository> _logradouroRepoFactory;

    public AlunoService(
        Func<IAlunoRepository> repoFactory,
        Func<ILogradouroRepository> logradouroRepoFactory)
    {
        _repoFactory = repoFactory ?? throw new ArgumentNullException(nameof(repoFactory));
        _logradouroRepoFactory = logradouroRepoFactory ?? throw new ArgumentNullException(nameof(logradouroRepoFactory));
    }

    public async Task<AlunoDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aluno = await _repoFactory().ObterPorId(id, cancellationToken);
        return aluno == null ? null : await MapearComEnderecoAsync(aluno, cancellationToken);
    }

    public async Task<IEnumerable<AlunoDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
    {
        var alunos = await _repoFactory().ObterTodos(cancellationToken);
        return await MapearComEnderecosAsync(alunos, cancellationToken);
    }

    public async Task<AlunoDto?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var aluno = await _repoFactory().ObterPorCpf(CriarCpf(cpf), cancellationToken);
        return aluno == null ? null : await MapearComEnderecoAsync(aluno, cancellationToken);
    }

    public async Task<AlunoDto?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var aluno = await _repoFactory().ObterPorEmail(CriarEmail(email), cancellationToken);
        return aluno == null ? null : await MapearComEnderecoAsync(aluno, cancellationToken);
    }

    public async Task<IEnumerable<AlunoDto>> ObterPorNomeAsync(
        string nome,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome não pode ser vazio.", nameof(nome));

        var alunos = await _repoFactory().ObterPorNome(nome.Trim(), cancellationToken);
        return await MapearComEnderecosAsync(alunos, cancellationToken);
    }

    public async Task<bool> CpfJaExisteAsync(
        string cpf,
        int? id = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;
        var result = Cpf.Criar(cpf);
        return result.IsSuccess && await _repoFactory().CpfJaExiste(result.Value!, id, cancellationToken);
    }

    public async Task<bool> EmailJaExisteAsync(
        string email,
        int? id = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        var result = Email.Criar(email);
        return result.IsSuccess && await _repoFactory().EmailJaExiste(result.Value!, id, cancellationToken);
    }

    public async Task<AlunoDto> AdicionarAsync(AlunoDto alunoDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alunoDto);

        var cpf = CriarCpf(alunoDto.Cpf);
        if (await _repoFactory().CpfJaExiste(cpf, null, cancellationToken))
            throw new InvalidOperationException($"Já existe um aluno cadastrado com o CPF {alunoDto.Cpf}.");

        var email = CriarEmail(alunoDto.Email ?? string.Empty);
        if (await _repoFactory().EmailJaExiste(email, null, cancellationToken))
            throw new InvalidOperationException($"Já existe um aluno cadastrado com o e-mail {alunoDto.Email}.");

        ValidarEAplicarHash(alunoDto, senhaObrigatoria: true);
        var logradouro = await ObterLogradouroAsync(alunoDto.Endereco, cancellationToken);
        var adicionado = await _repoFactory().Adicionar(alunoDto.ToEntity(logradouro), cancellationToken);
        return adicionado.ToDto(logradouro);
    }

    public async Task<AlunoDto> AtualizarAsync(AlunoDto alunoDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alunoDto);

        var existente = await _repoFactory().ObterPorId(alunoDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Aluno com ID {alunoDto.Id} não encontrado.");
        var cpf = CriarCpf(alunoDto.Cpf);
        if (cpf != existente.Cpf && await _repoFactory().CpfJaExiste(cpf, alunoDto.Id, cancellationToken))
            throw new InvalidOperationException($"Já existe outro aluno cadastrado com o CPF {alunoDto.Cpf}.");

        var email = CriarEmail(alunoDto.Email ?? string.Empty);
        if (email != existente.Email && await _repoFactory().EmailJaExiste(email, alunoDto.Id, cancellationToken))
            throw new InvalidOperationException($"Já existe outro aluno cadastrado com o e-mail {alunoDto.Email}.");

        ValidarEAplicarHash(alunoDto, senhaObrigatoria: false);
        var logradouroId = alunoDto.Endereco?.Id > 0 ? alunoDto.Endereco.Id : existente.Endereco.LogradouroId;
        var logradouro = await ObterLogradouroAsync(logradouroId, cancellationToken);
        var atualizado = await _repoFactory().Atualizar(existente.UpdateFromDto(alunoDto, logradouro), cancellationToken);
        return atualizado.ToDto(logradouro);
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _repoFactory().ObterPorId(id, cancellationToken) == null)
            return false;
        return await _repoFactory().Remover(id, cancellationToken);
    }

    public async Task<bool> TrocarSenhaAsync(
        int id,
        string novaSenha,
        CancellationToken cancellationToken = default)
    {
        var senha = CriarSenha(novaSenha);
        var hashResult = Senha.Criar(PasswordHasher.Hash(senha.Valor));
        if (hashResult.IsFailure)
            throw new InvalidOperationException("Falha ao gerar o hash da nova senha.");
        return await _repoFactory().TrocarSenha(id, hashResult.Value!, cancellationToken);
    }

    private async Task<AlunoDto> MapearComEnderecoAsync(Aluno aluno, CancellationToken cancellationToken)
    {
        var logradouro = await _logradouroRepoFactory().ObterPorId(aluno.Endereco.LogradouroId, cancellationToken)
            ?? throw new InvalidOperationException($"Logradouro associado ao aluno {aluno.Id} não encontrado.");
        return aluno.ToDto(logradouro);
    }

    private async Task<IEnumerable<AlunoDto>> MapearComEnderecosAsync(
        IEnumerable<Aluno> alunos,
        CancellationToken cancellationToken)
    {
        var resultado = new List<AlunoDto>();
        foreach (var aluno in alunos)
            resultado.Add(await MapearComEnderecoAsync(aluno, cancellationToken));
        return resultado;
    }

    private async Task<Logradouro> ObterLogradouroAsync(
        LogradouroDto? endereco,
        CancellationToken cancellationToken)
    {
        if (endereco == null || endereco.Id <= 0)
            throw new InvalidOperationException("Logradouro/Endereço é obrigatório para o Aluno.");
        return await ObterLogradouroAsync(endereco.Id, cancellationToken);
    }

    private async Task<Logradouro> ObterLogradouroAsync(int id, CancellationToken cancellationToken) =>
        await _logradouroRepoFactory().ObterPorId(id, cancellationToken)
        ?? throw new KeyNotFoundException($"Logradouro com ID {id} não encontrado.");

    private static void ValidarEAplicarHash(AlunoDto dto, bool senhaObrigatoria)
    {
        if (string.IsNullOrWhiteSpace(dto.Senha))
        {
            if (senhaObrigatoria)
                throw new ArgumentException("Senha é obrigatória.", nameof(dto));
            return;
        }

        var senha = CriarSenha(dto.Senha);
        dto.Senha = PasswordHasher.Hash(senha.Valor);
    }

    private static Cpf CriarCpf(string cpf)
    {
        var result = Cpf.Criar(cpf);
        if (result.IsFailure)
            throw new ArgumentException($"CPF inválido: {FormatarErros(result.Notifications)}", nameof(cpf));
        return result.Value!;
    }

    private static Email CriarEmail(string email)
    {
        var result = Email.Criar(email);
        if (result.IsFailure)
            throw new ArgumentException($"E-mail inválido: {FormatarErros(result.Notifications)}", nameof(email));
        return result.Value!;
    }

    private static Senha CriarSenha(string senha)
    {
        var result = Senha.Criar(senha);
        if (result.IsFailure)
            throw new ArgumentException($"Senha inválida: {FormatarErros(result.Notifications)}", nameof(senha));
        return result.Value!;
    }

    private static string FormatarErros(IEnumerable<Domain.Common.Notification> notifications) =>
        string.Join(", ", notifications.Select(notification => notification.Mensagem));
}
