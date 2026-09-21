using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Application.Security;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Services;

public class ColaboradorService : IColaboradorService
{
    private readonly Func<IColaboradorRepository> _repoFactory;
    private readonly Func<ILogradouroRepository> _logradouroRepoFactory;

    public ColaboradorService(
        Func<IColaboradorRepository> repoFactory,
        Func<ILogradouroRepository> logradouroRepoFactory)
    {
        _repoFactory = repoFactory ?? throw new ArgumentNullException(nameof(repoFactory));
        _logradouroRepoFactory = logradouroRepoFactory ?? throw new ArgumentNullException(nameof(logradouroRepoFactory));
    }

    public async Task<ColaboradorDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var colaborador = await _repoFactory().ObterPorId(id, cancellationToken);
        return colaborador == null ? null : await MapearComEnderecoAsync(colaborador, cancellationToken);
    }

    public async Task<IEnumerable<ColaboradorDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
    {
        var colaboradores = await _repoFactory().ObterTodos(cancellationToken);
        return await MapearComEnderecosAsync(colaboradores, cancellationToken);
    }

    public async Task<ColaboradorDto?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var colaborador = await _repoFactory().ObterPorCpf(CriarCpf(cpf), cancellationToken);
        return colaborador == null ? null : await MapearComEnderecoAsync(colaborador, cancellationToken);
    }

    public async Task<ColaboradorDto?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var colaborador = await _repoFactory().ObterPorEmail(CriarEmail(email), cancellationToken);
        return colaborador == null ? null : await MapearComEnderecoAsync(colaborador, cancellationToken);
    }

    public async Task<IEnumerable<ColaboradorDto>> ObterPorTipoAsync(
        AppColaboradorTipo tipo,
        CancellationToken cancellationToken = default)
    {
        ValidarTipo(tipo);
        var colaboradores = await _repoFactory().ObterPorTipo(tipo.ToDomain(), cancellationToken);
        return await MapearComEnderecosAsync(colaboradores, cancellationToken);
    }

    public async Task<IEnumerable<ColaboradorDto>> ObterPorVinculoAsync(
        AppColaboradorVinculo vinculo,
        CancellationToken cancellationToken = default)
    {
        ValidarVinculo(vinculo);
        var colaboradores = await _repoFactory().ObterPorVinculo(vinculo.ToDomain(), cancellationToken);
        return await MapearComEnderecosAsync(colaboradores, cancellationToken);
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

    public async Task<ColaboradorDto> AdicionarAsync(
        ColaboradorDto colaboradorDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(colaboradorDto);
        ValidarEnums(colaboradorDto);

        var cpf = CriarCpf(colaboradorDto.Cpf);
        if (await _repoFactory().CpfJaExiste(cpf, null, cancellationToken))
            throw new InvalidOperationException($"Já existe um colaborador cadastrado com o CPF {colaboradorDto.Cpf}.");

        var email = CriarEmail(colaboradorDto.Email ?? string.Empty);
        if (await _repoFactory().EmailJaExiste(email, null, cancellationToken))
            throw new InvalidOperationException($"Já existe um colaborador cadastrado com o e-mail {colaboradorDto.Email}.");

        ValidarEAplicarHash(colaboradorDto, senhaObrigatoria: true);
        var logradouro = await ObterLogradouroAsync(colaboradorDto.Endereco, cancellationToken);
        var adicionado = await _repoFactory().Adicionar(colaboradorDto.ToEntity(logradouro), cancellationToken);
        return adicionado.ToDto(logradouro);
    }

    public async Task<ColaboradorDto> AtualizarAsync(
        ColaboradorDto colaboradorDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(colaboradorDto);
        ValidarEnums(colaboradorDto);

        var existente = await _repoFactory().ObterPorId(colaboradorDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Colaborador com ID {colaboradorDto.Id} não encontrado.");
        var cpf = CriarCpf(colaboradorDto.Cpf);
        if (cpf != existente.Cpf && await _repoFactory().CpfJaExiste(cpf, colaboradorDto.Id, cancellationToken))
            throw new InvalidOperationException($"Já existe outro colaborador cadastrado com o CPF {colaboradorDto.Cpf}.");

        var email = CriarEmail(colaboradorDto.Email ?? string.Empty);
        if (email != existente.Email && await _repoFactory().EmailJaExiste(email, colaboradorDto.Id, cancellationToken))
            throw new InvalidOperationException($"Já existe outro colaborador cadastrado com o e-mail {colaboradorDto.Email}.");

        ValidarEAplicarHash(colaboradorDto, senhaObrigatoria: false);
        var logradouroId = colaboradorDto.Endereco?.Id > 0
            ? colaboradorDto.Endereco.Id
            : existente.Endereco.LogradouroId;
        var logradouro = await ObterLogradouroAsync(logradouroId, cancellationToken);
        var atualizado = await _repoFactory().Atualizar(
            existente.UpdateFromDto(colaboradorDto, logradouro), cancellationToken);
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

    private async Task<ColaboradorDto> MapearComEnderecoAsync(
        Colaborador colaborador,
        CancellationToken cancellationToken)
    {
        var logradouro = await _logradouroRepoFactory().ObterPorId(
            colaborador.Endereco.LogradouroId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Logradouro associado ao colaborador {colaborador.Id} não encontrado.");
        return colaborador.ToDto(logradouro);
    }

    private async Task<IEnumerable<ColaboradorDto>> MapearComEnderecosAsync(
        IEnumerable<Colaborador> colaboradores,
        CancellationToken cancellationToken)
    {
        var resultado = new List<ColaboradorDto>();
        foreach (var colaborador in colaboradores)
            resultado.Add(await MapearComEnderecoAsync(colaborador, cancellationToken));
        return resultado;
    }

    private async Task<Logradouro> ObterLogradouroAsync(
        LogradouroDto? endereco,
        CancellationToken cancellationToken)
    {
        if (endereco == null || endereco.Id <= 0)
            throw new InvalidOperationException("Logradouro/Endereço é obrigatório para o Colaborador.");
        return await ObterLogradouroAsync(endereco.Id, cancellationToken);
    }

    private async Task<Logradouro> ObterLogradouroAsync(int id, CancellationToken cancellationToken) =>
        await _logradouroRepoFactory().ObterPorId(id, cancellationToken)
        ?? throw new KeyNotFoundException($"Logradouro com ID {id} não encontrado.");

    private static void ValidarEAplicarHash(ColaboradorDto dto, bool senhaObrigatoria)
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

    private static void ValidarEnums(ColaboradorDto dto)
    {
        ValidarTipo(dto.Tipo);
        ValidarVinculo(dto.Vinculo);
    }

    private static void ValidarTipo(AppColaboradorTipo tipo)
    {
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo de colaborador inválido.");
    }

    private static void ValidarVinculo(AppColaboradorVinculo vinculo)
    {
        if (!Enum.IsDefined(vinculo))
            throw new ArgumentOutOfRangeException(nameof(vinculo), "Vínculo de colaborador inválido.");
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
