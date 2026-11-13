using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;

    public AcessoAlunoService(
        Func<IAcessoAlunoRepository> acessoRepoFactory,
        Func<IAlunoRepository> alunoRepoFactory,
        Func<IMatriculaRepository> matriculaRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory
            ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory
            ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory
            ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
    }

    public async Task<AcessoAlunoDto?> ObterPorIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterPorId(id, cancellationToken);
        return acesso is null ? null : await MapearAsync(acesso, cancellationToken);
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterTodosAsync(
        CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await MapearTodosAsync(
            await repository.ObterTodos(cancellationToken), cancellationToken);
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterPorAlunoPeriodoAsync(
        int? alunoId = null,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        CancellationToken cancellationToken = default)
    {
        ValidarPeriodo(inicio, fim);
        await using var repository = _acessoRepoFactory();
        var acessos = await repository.ObterAcessosPorAlunoPeriodo(
            alunoId, inicio, fim, cancellationToken);
        return await MapearTodosAsync(acessos, cancellationToken);
    }

    public async Task<AcessoAlunoDto?> ObterUltimoAcessoAsync(
        int alunoId, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterUltimoAcesso(
            alunoId, cancellationToken);
        return acesso is null ? null : await MapearAsync(acesso, cancellationToken);
    }

    public async Task<bool> EstaNaAcademiaAsync(
        int alunoId, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await repository.EstaNaAcademia(alunoId, cancellationToken);
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMesAsync(
        int mes, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await repository.ObterHorarioMaisProcuradoPorMes(mes, cancellationToken);
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMesAsync(
        int mes, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        return await repository.ObterPermanenciaMediaPorMes(mes, cancellationToken);
    }

    public async Task<IEnumerable<AlunoDto>> ObterAlunosSemAcessoNosUltimosDiasAsync(
        int dias, CancellationToken cancellationToken = default)
    {
        if (dias < 0)
            throw new ArgumentOutOfRangeException(nameof(dias));
        await using var repository = _acessoRepoFactory();
        var alunos = await repository.ObterAlunosSemAcessoNosUltimosDias(
            dias, cancellationToken);
        return [.. alunos.Select(aluno => aluno.ToDto())];
    }

    public async Task<AcessoAlunoDto> RegistrarEntradaAsync(
        int alunoId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        var aluno = await ObterAlunoAsync(alunoId, cancellationToken);
        await using var repository = _acessoRepoFactory();
        if (await repository.EstaNaAcademia(alunoId, cancellationToken))
        {
            throw new InvalidOperationException(
                "A entrada deste aluno já foi registrada. " +
                "Registre a saída antes de iniciar um novo acesso.");
        }

        var matricula = await _matriculaRepoFactory().ObterMatriculaAtivaPorAluno(
            alunoId, cancellationToken);
        if (matricula is null)
        {
            throw new InvalidOperationException(
                "Acesso negado: o aluno não possui matrícula ativa.");
        }

        var result = AcessoAluno.CriarEntrada(
            0, aluno, dataHora ?? DateTime.Now);
        if (result.IsFailure)
            throw CriarErroDominio("registrar a entrada", result.Notifications);

        var acesso = await repository.Adicionar(
            result.Value!, cancellationToken);
        var dto = acesso.ToDto(aluno.ToDto());
        var hoje = DateOnly.FromDateTime((dataHora ?? DateTime.Now).Date);
        dto.DataFimPlano = matricula.DataFim;
        dto.DiasRestantesPlano = Math.Max(0, matricula.DataFim.DayNumber - hoje.DayNumber);
        return dto;
    }

    public async Task<AcessoAlunoDto> RegistrarSaidaAsync(
        int alunoId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        var aluno = await ObterAlunoAsync(alunoId, cancellationToken);
        await using var repository = _acessoRepoFactory();
        var acesso = await repository.ObterUltimoAcesso(
            alunoId, cancellationToken);
        if (acesso is null || !acesso.EstaEmAberto)
        {
            throw new InvalidOperationException(
                "Este aluno não possui uma entrada em aberto. " +
                "Registre a entrada antes da saída.");
        }

        var result = acesso.RegistrarSaida(aluno, dataHora ?? DateTime.Now);
        if (result.IsFailure)
            throw CriarErroDominio("registrar a saída", result.Notifications);

        var atualizado = await repository.Atualizar(
            result.Value!, cancellationToken);
        var dto = atualizado.ToDto(aluno.ToDto());
        dto.Permanencia = atualizado.DataHoraSaida - atualizado.DataHoraEntrada;
        return dto;
    }

    public async Task<AcessoAlunoDto> RegistrarMovimentacaoAsync(
        int alunoId,
        DateTime? dataHora = null,
        CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        var estaNaAcademia = await repository.EstaNaAcademia(
            alunoId, cancellationToken);
        // Libera esta conexão antes de iniciar a operação de entrada ou saída,
        // que abrirá uma conexão independente.
        await repository.DisposeAsync();
        return estaNaAcademia
            ? await RegistrarSaidaAsync(alunoId, dataHora, cancellationToken)
            : await RegistrarEntradaAsync(alunoId, dataHora, cancellationToken);
    }

    public async Task<AcessoAlunoDto> AdicionarAsync(
        AcessoAlunoDto acessoDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        if (acessoDto.DataHoraSaida.HasValue)
        {
            throw new InvalidOperationException(
                "Um novo acesso deve ser iniciado pela entrada, sem saída preenchida.");
        }
        return await RegistrarEntradaAsync(
            acessoDto.AlunoAcesso?.Id ?? 0,
            acessoDto.DataHoraEntrada,
            cancellationToken);
    }

    public async Task<AcessoAlunoDto> AtualizarAsync(
        AcessoAlunoDto acessoDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acessoDto);
        await using var repository = _acessoRepoFactory();
        var existente = await repository.ObterPorId(
            acessoDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Acesso de aluno com ID {acessoDto.Id} não encontrado.");
        if (acessoDto.AlunoAcesso?.Id != existente.AlunoId)
        {
            throw new InvalidOperationException(
                "O aluno do acesso não pode ser alterado.");
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
            var aluno = await ObterAlunoAsync(existente.AlunoId, cancellationToken);
            return existente.ToDto(aluno.ToDto());
        }
        return await RegistrarSaidaAsync(
            existente.AlunoId, acessoDto.DataHoraSaida, cancellationToken);
    }

    public async Task<bool> RemoverAsync(
        int id, CancellationToken cancellationToken = default)
    {
        await using var repository = _acessoRepoFactory();
        if (await repository.ObterPorId(id, cancellationToken) is null)
            return false;
        return await repository.Remover(id, cancellationToken);
    }

    private async Task<AcessoAlunoDto> MapearAsync(
        AcessoAluno acesso, CancellationToken cancellationToken)
    {
        var aluno = await ObterAlunoAsync(acesso.AlunoId, cancellationToken);
        return acesso.ToDto(aluno.ToDto());
    }

    private async Task<IEnumerable<AcessoAlunoDto>> MapearTodosAsync(
        IEnumerable<AcessoAluno> acessos,
        CancellationToken cancellationToken)
    {
        var lista = acessos.ToList();
        var alunos = new Dictionary<int, Aluno>();
        foreach (var alunoId in lista.Select(acesso => acesso.AlunoId).Distinct())
            alunos[alunoId] = await ObterAlunoAsync(alunoId, cancellationToken);
        return [.. lista.Select(acesso => acesso.ToDto(alunos[acesso.AlunoId].ToDto()))];
    }

    private async Task<Aluno> ObterAlunoAsync(
        int alunoId, CancellationToken cancellationToken)
    {
        if (alunoId <= 0)
            throw new InvalidOperationException("Aluno não informado para o acesso.");
        return await _alunoRepoFactory().ObterPorId(alunoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aluno com ID {alunoId} não encontrado.");
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
            "DATA_HORA_INTERVALO_INVALIDO" =>
                "O registro de entrada ou saída só pode ser feito entre 06h e 22h.",
            "SAIDA_ANTERIOR_A_ENTRADA" =>
                "O horário de saída não pode ser anterior ao horário de entrada.",
            "SAIDA_JA_REGISTRADA" => "A saída deste acesso já foi registrada.",
            "ALUNO_DIFERENTE_DA_ENTRADA" =>
                "A saída deve ser registrada para o mesmo aluno que entrou.",
            "ALUNO_INVALIDO" => "O aluno informado para o acesso é inválido.",
            _ => $"Não foi possível {notification.Propriedade}: {notification.Mensagem}."
        };
}
