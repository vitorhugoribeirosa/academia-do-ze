// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Matricula : Entity, IAggregateRoot
{
    private const MatriculaRestricoes TodasRestricoes =
        MatriculaRestricoes.Diabetes |
        MatriculaRestricoes.PressaoAlta |
        MatriculaRestricoes.Labirintite |
        MatriculaRestricoes.Alergias |
        MatriculaRestricoes.ProblemasRespiratorios |
        MatriculaRestricoes.RemedioContinuo;

    public Aluno Aluno { get; }
    public MatriculaPlano Plano { get; }
    public DateOnly DataInicio { get; }
    public DateOnly DataFim { get; }
    public string Objetivo { get; }
    public MatriculaRestricoes RestricoesMedicas { get; }
    public string ObservacoesRestricoes { get; }
    public Arquivo? LaudoMedico { get; }

    private Matricula(
        int id,
        Aluno aluno,
        MatriculaPlano plano,
        DateOnly dataInicio,
        DateOnly dataFim,
        string objetivo,
        MatriculaRestricoes restricoesMedicas,
        string observacoesRestricoes,
        Arquivo? laudoMedico) : base(id)
    {
        Aluno = aluno;
        Plano = plano;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Objetivo = objetivo;
        RestricoesMedicas = restricoesMedicas;
        ObservacoesRestricoes = observacoesRestricoes;
        LaudoMedico = laudoMedico;
    }

    public bool EstaAtivaEm(DateOnly data) => data >= DataInicio && data <= DataFim;

    public static Result<Matricula> Criar(
        int id,
        Aluno? aluno,
        MatriculaPlano plano,
        DateOnly dataInicio,
        DateOnly dataFim,
        string objetivo,
        MatriculaRestricoes restricoesMedicas,
        string? observacoesRestricoes,
        Arquivo? laudoMedico)
    {
        var notifications = new List<Notification>();

        if (aluno is null)
            notifications.Add(new Notification("Aluno", "ALUNO_OBRIGATORIO"));

        if (!Enum.IsDefined(plano))
            notifications.Add(new Notification("Plano", "PLANO_INVALIDO"));

        if (dataInicio == default)
            notifications.Add(new Notification("DataInicio", "DATA_INICIO_OBRIGATORIA"));

        if (dataFim == default)
            notifications.Add(new Notification("DataFim", "DATA_FIM_OBRIGATORIA"));
        else if (dataInicio != default && dataFim < dataInicio)
            notifications.Add(new Notification("DataFim", "DATA_FIM_ANTERIOR_INICIO"));

        if (NormalizadoService.TextoVazioOuNulo(objetivo))
            notifications.Add(new Notification("Objetivo", "OBJETIVO_OBRIGATORIO"));
        else
            objetivo = NormalizadoService.LimparEspacos(objetivo);

        if ((restricoesMedicas & ~TodasRestricoes) != 0)
            notifications.Add(new Notification("RestricoesMedicas", "RESTRICAO_INVALIDA"));

        observacoesRestricoes = NormalizadoService.LimparEspacos(observacoesRestricoes);

        var exigeLaudoPorIdade = aluno is not null &&
            aluno.DataNascimento <= dataInicio.AddYears(-12) &&
            aluno.DataNascimento > dataInicio.AddYears(-17);
        var possuiRestricao = restricoesMedicas != MatriculaRestricoes.None;

        if ((exigeLaudoPorIdade || possuiRestricao) && laudoMedico is null)
            notifications.Add(new Notification("LaudoMedico", "LAUDO_MEDICO_OBRIGATORIO"));

        if (notifications.Count != 0)
            return Result<Matricula>.Failure(notifications);

        return Result<Matricula>.Success(new Matricula(
            id,
            aluno!,
            plano,
            dataInicio,
            dataFim,
            objetivo,
            restricoesMedicas,
            observacoesRestricoes,
            laudoMedico));
    }
}
