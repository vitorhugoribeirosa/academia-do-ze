using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class MatriculaService : IMatriculaService
{
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;

    public MatriculaService(
        Func<IMatriculaRepository> matriculaRepoFactory,
        Func<IAlunoRepository> alunoRepoFactory)
    {
        _matriculaRepoFactory = matriculaRepoFactory
            ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
        _alunoRepoFactory = alunoRepoFactory
            ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
    }

    public async Task<MatriculaDto?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var matricula = await _matriculaRepoFactory().ObterPorId(id, cancellationToken);
        return matricula == null ? null : await MapearComAlunoAsync(matricula, cancellationToken);
    }

    public async Task<IEnumerable<MatriculaDto>> ObterTodasAsync(
        CancellationToken cancellationToken = default)
    {
        var matriculas = await _matriculaRepoFactory().ObterTodos(cancellationToken);
        return await MapearComAlunosAsync(matriculas, cancellationToken);
    }

    public async Task<IEnumerable<MatriculaDto>> ObterPorAlunoIdAsync(
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        var aluno = await ObterAlunoAsync(alunoId, cancellationToken);
        var matriculas = await _matriculaRepoFactory().ObterPorAluno(alunoId, cancellationToken);
        return [.. matriculas.Select(matricula => matricula.ToDto(aluno.ToDto()))];
    }

    public async Task<MatriculaDto?> ObterMatriculaAtivaPorAlunoAsync(
        int alunoId,
        CancellationToken cancellationToken = default)
    {
        var matricula = await _matriculaRepoFactory().ObterMatriculaAtivaPorAluno(alunoId, cancellationToken);
        return matricula == null ? null : await MapearComAlunoAsync(matricula, cancellationToken);
    }

    public Task<bool> PossuiMatriculaAtivaAsync(
        int alunoId,
        CancellationToken cancellationToken = default) =>
        _matriculaRepoFactory().PossuiMatriculaAtiva(alunoId, cancellationToken);

    public async Task<IEnumerable<MatriculaDto>> ObterAtivasAsync(
        int alunoId = 0,
        CancellationToken cancellationToken = default)
    {
        var matriculas = await _matriculaRepoFactory().ObterAtivas(alunoId, cancellationToken);
        return await MapearComAlunosAsync(matriculas, cancellationToken);
    }

    public async Task<IEnumerable<MatriculaDto>> ObterVencendoEmDiasAsync(
        int dias,
        CancellationToken cancellationToken = default)
    {
        if (dias < 0)
            throw new ArgumentOutOfRangeException(nameof(dias), "A quantidade de dias não pode ser negativa.");

        var matriculas = await _matriculaRepoFactory().ObterVencendoEmDias(dias, cancellationToken);
        return await MapearComAlunosAsync(matriculas, cancellationToken);
    }

    public async Task<IEnumerable<MatriculaDto>> ObterPorPlanoAsync(
        AppMatriculaPlano plano,
        CancellationToken cancellationToken = default)
    {
        ValidarPlano(plano);
        var matriculas = await _matriculaRepoFactory().ObterPorPlano(plano.ToDomain(), cancellationToken);
        return await MapearComAlunosAsync(matriculas, cancellationToken);
    }

    public async Task<MatriculaDto> AdicionarAsync(
        MatriculaDto matriculaDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matriculaDto);
        ValidarEnums(matriculaDto);

        if (matriculaDto.AlunoMatricula == null || matriculaDto.AlunoMatricula.Id <= 0)
            throw new InvalidOperationException("Aluno não informado ou com ID inválido para a matrícula.");

        var aluno = await ObterAlunoAsync(matriculaDto.AlunoMatricula.Id, cancellationToken);
        if (await _matriculaRepoFactory().PossuiMatriculaAtiva(aluno.Id, cancellationToken))
            throw new InvalidOperationException("Já existe uma matrícula ativa para este aluno.");

        ValidarLaudo(matriculaDto, aluno, matriculaExistente: null);
        var adicionada = await _matriculaRepoFactory().Adicionar(
            matriculaDto.ToEntity(aluno), cancellationToken);
        return adicionada.ToDto(aluno.ToDto());
    }

    public async Task<MatriculaDto> AtualizarAsync(
        MatriculaDto matriculaDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matriculaDto);
        ValidarEnums(matriculaDto);

        var existente = await _matriculaRepoFactory().ObterPorId(matriculaDto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Matrícula com ID {matriculaDto.Id} não encontrada.");
        var aluno = await ObterAlunoAsync(existente.AlunoId, cancellationToken);
        ValidarLaudo(matriculaDto, aluno, existente);

        var atualizada = await _matriculaRepoFactory().Atualizar(
            existente.UpdateFromDto(matriculaDto, aluno), cancellationToken);
        return atualizada.ToDto(aluno.ToDto());
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _matriculaRepoFactory().ObterPorId(id, cancellationToken) == null)
            return false;
        return await _matriculaRepoFactory().Remover(id, cancellationToken);
    }

    private async Task<MatriculaDto> MapearComAlunoAsync(
        Matricula matricula,
        CancellationToken cancellationToken)
    {
        var aluno = await ObterAlunoAsync(matricula.AlunoId, cancellationToken);
        return matricula.ToDto(aluno.ToDto());
    }

    private async Task<IEnumerable<MatriculaDto>> MapearComAlunosAsync(
        IEnumerable<Matricula> matriculas,
        CancellationToken cancellationToken)
    {
        var lista = matriculas.ToList();
        if (lista.Count == 0)
            return [];

        var alunos = new Dictionary<int, Aluno>();
        foreach (var alunoId in lista.Select(matricula => matricula.AlunoId).Distinct())
            alunos[alunoId] = await ObterAlunoAsync(alunoId, cancellationToken);

        return [.. lista.Select(matricula => matricula.ToDto(alunos[matricula.AlunoId].ToDto()))];
    }

    private async Task<Aluno> ObterAlunoAsync(int alunoId, CancellationToken cancellationToken) =>
        await _alunoRepoFactory().ObterPorId(alunoId, cancellationToken)
        ?? throw new InvalidOperationException($"Aluno com ID {alunoId} não encontrado.");

    private static void ValidarLaudo(
        MatriculaDto dto,
        Aluno aluno,
        Matricula? matriculaExistente)
    {
        var possuiNovoLaudo = dto.LaudoMedico?.Conteudo is { Length: > 0 };
        var possuiLaudo = possuiNovoLaudo || (dto.LaudoMedico == null && matriculaExistente?.LaudoMedico != null);
        var menorDe16 = aluno.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));

        if (menorDe16 && !possuiLaudo)
            throw new InvalidOperationException(
                "Alunos menores de 16 anos devem apresentar um laudo médico que autorize atividades físicas.");
        if (dto.RestricoesMedicas != AppMatriculaRestricoes.None && !possuiLaudo)
            throw new InvalidOperationException(
                "Alunos com restrições de saúde devem apresentar um parecer médico.");
    }

    private static void ValidarEnums(MatriculaDto dto)
    {
        ValidarPlano(dto.Plano);
        const AppMatriculaRestricoes todasRestricoes =
            AppMatriculaRestricoes.Diabetes |
            AppMatriculaRestricoes.PressaoAlta |
            AppMatriculaRestricoes.Labirintite |
            AppMatriculaRestricoes.Alergias |
            AppMatriculaRestricoes.ProblemasRespiratorios |
            AppMatriculaRestricoes.RemedioContinuo;
        if ((dto.RestricoesMedicas & ~todasRestricoes) != 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "Restrição médica inválida.");
    }

    private static void ValidarPlano(AppMatriculaPlano plano)
    {
        if (!Enum.IsDefined(plano))
            throw new ArgumentOutOfRangeException(nameof(plano), "Plano de matrícula inválido.");
    }
}
