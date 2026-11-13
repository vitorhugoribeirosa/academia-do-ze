using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoColaboradorService : IAcessoColaboradorService
{
    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;

    public AcessoColaboradorService(
        Func<IAcessoColaboradorRepository> acessoRepoFactory,
        Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory
            ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory
            ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }

    public async Task<AcessoColaboradorDto?> ObterPorIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterPorId(id, cancellationToken);
        return acesso is null ? null : await MapearAsync(acesso, cancellationToken);
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterTodosAsync(
        CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await MapearTodosAsync(
            await repository.ObterTodos(cancellationToken), cancellationToken);
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterPorColaboradorPeriodoAsync(
        int? colaboradorId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default)
    {
        ValidarPeriodo(inicio, fim);
        await using var repository = _acessoRepoFactory();
        var acessos = await repository.ObterAcessosPorColaboradorPeriodo(
            colaboradorId, inicio, fim, cancellationToken);
        return await MapearTodosAsync(acessos, cancellationToken);
    }

    public async Task<AcessoColaboradorDto?> ObterUltimoAcessoAsync(
        int colaboradorId, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterUltimoAcesso(
            colaboradorId, cancellationToken);
        return acesso is null ? null : await MapearAsync(acesso, cancellationToken);
    }

    public async Task<bool> EstaNaAcademiaAsync(
        int colaboradorId, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var ultimo = await repository.ObterUltimoAcesso(
            colaboradorId, cancellationToken);
        return ultimo?.EstaEmAberto == true;
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(
        int colaboradorId,
        DateOnly data,
        CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await repository.ObterHorasTrabalhadasNoDia(
            colaboradorId, data, cancellationToken);
    }

    public async Task<AcessoColaboradorDto> RegistrarEntradaAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        var colaborador = await ObterColaboradorAsync(
            colaboradorId, cancellationToken);
        await using var repository = _acessoRepoFactory();
        var ultimo = await repository.ObterUltimoAcesso(
            colaboradorId, cancellationToken);
        if (ultimo?.EstaEmAberto == true)
        {
            throw new InvalidOperationException(
                "A entrada deste colaborador já foi registrada. " +
                "Registre a saída antes de iniciar um novo acesso.");
        }

        var dataRegistro = dataHora ?? DateTime.Now;
        var limite = colaborador.Vinculo == ColaboradorVinculo.CLT
            ? TimeSpan.FromHours(8)
            : TimeSpan.FromHours(6);
        var totalHoje = await repository.ObterHorasTrabalhadasNoDia(
            colaboradorId, DateOnly.FromDateTime(dataRegistro), cancellationToken);
        if (totalHoje >= limite)
        {
            throw new InvalidOperationException(
                $"Acesso negado: o colaborador já atingiu o limite diário de {limite.TotalHours:0} horas.");
        }

        var result = AcessoColaborador.CriarEntrada(
            0, colaborador, dataRegistro);
        if (result.IsFailure)
            throw CriarErroDominio("registrar a entrada", result.Notifications);

        var acesso = await repository.Adicionar(
            result.Value!, cancellationToken);
        var dto = acesso.ToDto(colaborador.ToDto());
        dto.LimiteJornada = limite;
        dto.PermanenciaTotalDia = totalHoje;
        return dto;
    }

    public async Task<AcessoColaboradorDto> RegistrarSaidaAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        var colaborador = await ObterColaboradorAsync(
            colaboradorId, cancellationToken);
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterUltimoAcesso(
            colaboradorId, cancellationToken);
        if (acesso is null || !acesso.EstaEmAberto)
        {
            throw new InvalidOperationException(
                "Este colaborador não possui uma entrada em aberto. " +
                "Registre a entrada antes da saída.");
        }

        var result = acesso.RegistrarSaida(
            colaborador, dataHora ?? DateTime.Now);
        if (result.IsFailure)
            throw CriarErroDominio("registrar a saída", result.Notifications);

        var atualizado = await repository.Atualizar(
            result.Value!, cancellationToken);
        var totalHoje = await repository.ObterHorasTrabalhadasNoDia(
            colaboradorId,
            DateOnly.FromDateTime(atualizado.DataHoraEntrada),
            cancellationToken);
        var dto = atualizado.ToDto(colaborador.ToDto());
        dto.PermanenciaTotalDia = totalHoje;
        dto.LimiteJornada = colaborador.Vinculo == ColaboradorVinculo.CLT
            ? TimeSpan.FromHours(8)
            : TimeSpan.FromHours(6);
        return dto;
    }

    public async Task<AcessoColaboradorDto> RegistrarMovimentacaoAsync(
        int colaboradorId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        return await EstaNaAcademiaAsync(colaboradorId, cancellationToken)
            ? await RegistrarSaidaAsync(colaboradorId, dataHora, cancellationToken)
            : await RegistrarEntradaAsync(colaboradorId, dataHora, cancellationToken);
    }

    public async Task<AcessoColaboradorDto> AdicionarAsync(
        AcessoColaboradorDto acessoDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        if (acessoDto.DataHoraSaida.HasValue)
        {
            throw new InvalidOperationException(
                "Um novo acesso deve ser iniciado pela entrada, sem saída preenchida.");
        }
        return await RegistrarEntradaAsync(
            acessoDto.ColaboradorAcesso?.Id ?? 0,
            acessoDto.DataHoraEntrada,
            cancellationToken);
    }

    public async Task<AcessoColaboradorDto> AtualizarAsync(
        AcessoColaboradorDto acessoDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        await using var repository = _acessoRepoFactory();
        var existente = await repository.ObterPorId(
            acessoDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Acesso de colaborador com ID {acessoDto.Id} não encontrado.");
        if (acessoDto.ColaboradorAcesso?.Id != existente.ColaboradorId)
        {
            throw new InvalidOperationException(
                "O colaborador do acesso não pode ser alterado.");
        }
        if (acessoDto.DataHoraEntrada != existente.DataHoraEntrada)
        {
            throw new InvalidOperationException(
                "O horário de entrada de um acesso registrado não pode ser alterado.");
        }
        if (!existente.EstaEmAberto)
        {
            throw new InvalidOperationException(
                "Um acesso encerrado não pode ser reaberto ou alterado.");
        }
        if (!acessoDto.DataHoraSaida.HasValue)
        {
            var colaborador = await ObterColaboradorAsync(
                existente.ColaboradorId, cancellationToken);
            return existente.ToDto(colaborador.ToDto());
        }
        return await RegistrarSaidaAsync(
            existente.ColaboradorId, acessoDto.DataHoraSaida, cancellationToken);
    }

    public async Task<bool> RemoverAsync(
        int id, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        if (await repository.ObterPorId(id, cancellationToken) is null)
            return false;
        return await repository.Remover(id, cancellationToken);
    }

    private async Task<AcessoColaboradorDto> MapearAsync(
        AcessoColaborador acesso, CancellationToken cancellationToken)
    {
        var colaborador = await ObterColaboradorAsync(
            acesso.ColaboradorId, cancellationToken);
        return acesso.ToDto(colaborador.ToDto());
    }

    private async Task<IEnumerable<AcessoColaboradorDto>> MapearTodosAsync(
        IEnumerable<AcessoColaborador> acessos,
        CancellationToken cancellationToken)
    {
        var lista = acessos.ToList();
        var colaboradores = new Dictionary<int, Colaborador>();
        foreach (var colaboradorId in lista.Select(acesso => acesso.ColaboradorId).Distinct())
        {
            colaboradores[colaboradorId] = await ObterColaboradorAsync(
                colaboradorId, cancellationToken);
        }
        return [.. lista.Select(acesso =>
            acesso.ToDto(colaboradores[acesso.ColaboradorId].ToDto()))];
    }

    private async Task<Colaborador> ObterColaboradorAsync(
        int colaboradorId, CancellationToken cancellationToken)
    {
        if (colaboradorId <= 0)
        {
            throw new InvalidOperationException(
                "Colaborador não informado para o acesso.");
        }
        return await _colaboradorRepoFactory().ObterPorId(
            colaboradorId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Colaborador com ID {colaboradorId} não encontrado.");
    }

    private static void ValidarPeriodo(DateOnly? inicio, DateOnly? fim)
    {
        if (inicio.HasValue && fim.HasValue && fim < inicio)
            throw new ArgumentException("A data final não pode ser anterior à inicial.");
    }

    private static InvalidOperationException CriarErroDominio(
        string operacao,
        IEnumerable<Domain.Common.Notification> notifications) =>
        new(string.Join(", ", notifications.Select(TraduzirNotificacao)));

    private static string TraduzirNotificacao(Domain.Common.Notification notification) =>
        notification.Mensagem switch
        {
            "SAIDA_ANTERIOR_A_ENTRADA" =>
                "O horário de saída não pode ser anterior ao horário de entrada.",
            "SAIDA_JA_REGISTRADA" => "A saída deste acesso já foi registrada.",
            "COLABORADOR_DIFERENTE_DA_ENTRADA" =>
                "A saída deve ser registrada para o mesmo colaborador que entrou.",
            "COLABORADOR_INVALIDO" =>
                "O colaborador informado para o acesso é inválido.",
            _ => $"Não foi possível {notification.Propriedade}: {notification.Mensagem}."
        };
}
