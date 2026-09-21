using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Services;

public class LogradouroService : ILogradouroService
{
    private readonly Func<ILogradouroRepository> _repoFactory;

    public LogradouroService(Func<ILogradouroRepository> repoFactory)
    {
        _repoFactory = repoFactory ?? throw new ArgumentNullException(nameof(repoFactory));
    }

    public async Task<LogradouroDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var logradouro = await _repoFactory().ObterPorId(id, cancellationToken);
        return logradouro?.ToDto();
    }

    public async Task<IEnumerable<LogradouroDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
    {
        var logradouros = await _repoFactory().ObterTodos(cancellationToken);
        return [.. logradouros.Select(logradouro => logradouro.ToDto())];
    }

    public async Task<LogradouroDto?> ObterPorCepAsync(string cep, CancellationToken cancellationToken = default)
    {
        var cepValue = CriarCep(cep);
        var logradouro = await _repoFactory().ObterPorCep(cepValue, cancellationToken);
        return logradouro?.ToDto();
    }

    public async Task<bool> CepJaExisteAsync(
        string cep,
        int? id = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return false;

        var result = Cep.Criar(cep);
        return result.IsSuccess && await _repoFactory().CepJaExiste(result.Value!, id, cancellationToken);
    }

    public async Task<IEnumerable<LogradouroDto>> ObterPorCidadeAsync(
        string cidade,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cidade))
            throw new ArgumentException("Cidade não pode ser vazia.", nameof(cidade));

        var logradouros = await _repoFactory().ObterPorCidade(cidade.Trim(), cancellationToken);
        return [.. logradouros.Select(logradouro => logradouro.ToDto())];
    }

    public async Task<IEnumerable<LogradouroDto>> ObterPorBairroAsync(
        string cidade,
        string bairro,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cidade))
            throw new ArgumentException("Cidade não pode ser vazia.", nameof(cidade));
        if (string.IsNullOrWhiteSpace(bairro))
            throw new ArgumentException("Bairro não pode ser vazio.", nameof(bairro));

        var logradouros = await _repoFactory().ObterPorBairro(cidade.Trim(), bairro.Trim(), cancellationToken);
        return [.. logradouros.Select(logradouro => logradouro.ToDto())];
    }

    public async Task<LogradouroDto> AdicionarAsync(
        LogradouroDto logradouroDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logradouroDto);

        var cep = CriarCep(logradouroDto.Cep);
        if (await _repoFactory().CepJaExiste(cep, null, cancellationToken))
            throw new InvalidOperationException($"Já existe um logradouro cadastrado com o CEP {logradouroDto.Cep}.");

        var adicionado = await _repoFactory().Adicionar(logradouroDto.ToEntity(), cancellationToken);
        return adicionado.ToDto();
    }

    public async Task<LogradouroDto> AtualizarAsync(
        LogradouroDto logradouroDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logradouroDto);

        var existente = await _repoFactory().ObterPorId(logradouroDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Logradouro com ID {logradouroDto.Id} não encontrado.");
        var cep = CriarCep(logradouroDto.Cep);
        if (await _repoFactory().CepJaExiste(cep, logradouroDto.Id, cancellationToken))
            throw new InvalidOperationException($"Já existe outro logradouro cadastrado com o CEP {logradouroDto.Cep}.");

        var atualizado = await _repoFactory().Atualizar(existente.UpdateFromDto(logradouroDto), cancellationToken);
        return atualizado.ToDto();
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _repoFactory().ObterPorId(id, cancellationToken) == null)
            return false;
        return await _repoFactory().Remover(id, cancellationToken);
    }

    private static Cep CriarCep(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            throw new ArgumentException("CEP não pode ser vazio.", nameof(cep));

        var result = Cep.Criar(cep);
        if (result.IsFailure)
            throw new ArgumentException($"CEP inválido: {FormatarErros(result.Notifications)}", nameof(cep));
        return result.Value!;
    }

    private static string FormatarErros(IEnumerable<Domain.Common.Notification> notifications) =>
        string.Join(", ", notifications.Select(notification => notification.Mensagem));
}
