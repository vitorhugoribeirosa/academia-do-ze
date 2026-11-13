using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class ControleAcessoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly IColaboradorService _colaboradorService;
    private readonly IAcessoAlunoService _acessoAlunoService;
    private readonly IAcessoColaboradorService _acessoColaboradorService;

    private List<PessoaAcessoItem> _todasPessoas = [];
    private List<MovimentacaoAcessoItem> _todasMovimentacoesHoje = [];
    private Task<bool> _statusTask = Task.FromResult(true);

    public ObservableCollection<PessoaAcessoItem> Pessoas { get; } = [];
    public ObservableCollection<MovimentacaoAcessoItem> MovimentacoesHoje { get; } = [];

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial string SearchMovimentacoesText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ExibirAlunos { get; set; } = true;
    [ObservableProperty] public partial PessoaAcessoItem? SelectedPessoa { get; set; }
    [ObservableProperty] public partial bool PessoaEstaNaAcademia { get; set; }
    [ObservableProperty] public partial bool HasSelectedPessoa { get; set; }
    [ObservableProperty] public partial string ActionButtonText { get; set; } = "Registrar entrada";
    [ObservableProperty] public partial string FeedbackMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasFeedback { get; set; }
    [ObservableProperty] public partial bool FeedbackIsSuccess { get; set; }

    public string TipoSelecionado => ExibirAlunos ? "Aluno" : "Colaborador";
    public string PainelMovimentacoesTitulo => ExibirAlunos
        ? "Acessos de alunos hoje"
        : "Acessos de colaboradores hoje";
    public string StatusSelecionado => PessoaEstaNaAcademia ? "Dentro da academia" : "Fora da academia";
    public string SelectedCpf => SelectedPessoa?.CpfFormatado ?? string.Empty;
    public ImageSource? SelectedFoto => SelectedPessoa?.Foto;

    public ControleAcessoViewModel(
        IAlunoService alunoService,
        IColaboradorService colaboradorService,
        IAcessoAlunoService acessoAlunoService,
        IAcessoColaboradorService acessoColaboradorService)
    {
        _alunoService = alunoService;
        _colaboradorService = colaboradorService;
        _acessoAlunoService = acessoAlunoService;
        _acessoColaboradorService = acessoColaboradorService;
        Title = "Controle de acesso";
    }

    partial void OnSearchTextChanged(string value) => AplicarFiltro();
    partial void OnSearchMovimentacoesTextChanged(string value) => AplicarFiltroMovimentacoesHoje();

    partial void OnSelectedPessoaChanged(PessoaAcessoItem? value)
    {
        HasSelectedPessoa = value is not null;
        HasFeedback = false;
        OnPropertyChanged(nameof(SelectedCpf));
        OnPropertyChanged(nameof(SelectedFoto));
        _statusTask = value is null
            ? Task.FromResult(true)
            : AtualizarStatusSelecionadoAsync(value);
    }

    partial void OnPessoaEstaNaAcademiaChanged(bool value)
    {
        ActionButtonText = value ? "Registrar saída" : "Registrar entrada";
        OnPropertyChanged(nameof(StatusSelecionado));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var alunosTask = _alunoService.ObterTodosAsync(cts.Token);
            var colaboradoresTask = _colaboradorService.ObterTodosAsync(cts.Token);
            await Task.WhenAll(alunosTask, colaboradoresTask);

            var alunos = (await alunosTask).Select(a => PessoaAcessoItem.FromAluno(a));
            var colaboradores = (await colaboradoresTask).Select(c => PessoaAcessoItem.FromColaborador(c));
            _todasPessoas = alunos.Concat(colaboradores).OrderBy(p => p.Nome).ToList();
            AplicarFiltro();
            await CarregarMovimentacoesHojeAsync(cts.Token);
        }
        catch (Exception ex)
        {
            MostrarFeedback($"Não foi possível carregar o controle de acesso: {ex.Message}", false);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelecionarAlunos()
    {
        ExibirAlunos = true;
        TrocarTipo();
    }

    [RelayCommand]
    private void SelecionarColaboradores()
    {
        ExibirAlunos = false;
        TrocarTipo();
    }

    [RelayCommand]
    private async Task RegistrarMovimentacaoAsync()
    {
        if (SelectedPessoa is null || IsBusy) return;
        var pessoaSelecionada = SelectedPessoa;
        try
        {
            IsBusy = true;
            if (!await _statusTask || SelectedPessoa != pessoaSelecionada)
                return;

            var eraSaida = PessoaEstaNaAcademia;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (pessoaSelecionada.EhAluno)
            {
                var acesso = await _acessoAlunoService.RegistrarMovimentacaoAsync(
                    pessoaSelecionada.Id, cancellationToken: cts.Token);
                MostrarFeedback(CriarMensagemAluno(acesso, eraSaida), true);
            }
            else
            {
                var acesso = await _acessoColaboradorService.RegistrarMovimentacaoAsync(
                    pessoaSelecionada.Id, cancellationToken: cts.Token);
                MostrarFeedback(CriarMensagemColaborador(acesso, eraSaida), true);
            }

            PessoaEstaNaAcademia = !eraSaida;
            await CarregarMovimentacoesHojeAsync(cts.Token);
        }
        catch (Exception ex)
        {
            var mensagem = ex.Message.StartsWith(
                "Acesso negado:", StringComparison.OrdinalIgnoreCase)
                ? ex.Message
                : $"Acesso negado: {ex.Message}";
            MostrarFeedback(mensagem, false);
        }
        finally { IsBusy = false; }
    }

    private void TrocarTipo()
    {
        SelectedPessoa = null;
        HasFeedback = false;
        OnPropertyChanged(nameof(TipoSelecionado));
        OnPropertyChanged(nameof(PainelMovimentacoesTitulo));
        AplicarFiltro();
        AplicarFiltroMovimentacoesHoje();
    }

    private void AplicarFiltro()
    {
        var termo = Normalizar(SearchText);
        var digitos = ApenasDigitos(SearchText);
        var filtradas = _todasPessoas.Where(p => p.EhAluno == ExibirAlunos)
            .Where(p => string.IsNullOrWhiteSpace(termo) ||
                        Normalizar(p.Nome).Contains(termo, StringComparison.Ordinal) ||
                        ApenasDigitos(p.Cpf).Contains(digitos, StringComparison.Ordinal))
            .Take(50)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pessoas.Clear();
            foreach (var pessoa in filtradas) Pessoas.Add(pessoa);
        });
    }

    private async Task<bool> AtualizarStatusSelecionadoAsync(PessoaAcessoItem pessoa)
    {
        try
        {
            var estaNaAcademia = pessoa.EhAluno
                ? await _acessoAlunoService.EstaNaAcademiaAsync(pessoa.Id)
                : await _acessoColaboradorService.EstaNaAcademiaAsync(pessoa.Id);
            if (SelectedPessoa == pessoa)
                PessoaEstaNaAcademia = estaNaAcademia;
            return true;
        }
        catch (Exception ex)
        {
            if (SelectedPessoa == pessoa)
                MostrarFeedback($"Não foi possível consultar o status: {ex.Message}", false);
            return false;
        }
    }

    private async Task CarregarMovimentacoesHojeAsync(CancellationToken cancellationToken)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        // SQLite trabalha melhor com as leituras sequenciais nesta tela. Além de evitar
        // disputa entre conexões na inicialização, mantém o comportamento igual nos três bancos.
        var acessosAlunos = await _acessoAlunoService.ObterPorAlunoPeriodoAsync(
            null, hoje, hoje, cancellationToken);
        var acessosColaboradores = await _acessoColaboradorService.ObterPorColaboradorPeriodoAsync(
            null, hoje, hoje, cancellationToken);

        var itens = new List<MovimentacaoAcessoItem>();
        foreach (var acesso in acessosAlunos)
        {
            itens.Add(new(acesso.AlunoAcesso.Nome, "Aluno", "Entrada", acesso.DataHoraEntrada));
            if (acesso.DataHoraSaida is { } saida) itens.Add(new(acesso.AlunoAcesso.Nome, "Aluno", "Saída", saida));
        }
        foreach (var acesso in acessosColaboradores)
        {
            itens.Add(new(acesso.ColaboradorAcesso.Nome, "Colaborador", "Entrada", acesso.DataHoraEntrada));
            if (acesso.DataHoraSaida is { } saida) itens.Add(new(acesso.ColaboradorAcesso.Nome, "Colaborador", "Saída", saida));
        }

        _todasMovimentacoesHoje = itens.OrderByDescending(i => i.DataHora).ToList();
        await MainThread.InvokeOnMainThreadAsync(AplicarFiltroMovimentacoesHoje);
    }

    private void AplicarFiltroMovimentacoesHoje()
    {
        var tipo = ExibirAlunos ? "Aluno" : "Colaborador";
        var termo = Normalizar(SearchMovimentacoesText);
        var filtradas = _todasMovimentacoesHoje
            .Where(item => item.Tipo == tipo)
            .Where(item => string.IsNullOrWhiteSpace(termo) ||
                           Normalizar(item.Nome).Contains(termo, StringComparison.Ordinal))
            .OrderByDescending(item => item.DataHora)
            .ToList();

        MovimentacoesHoje.Clear();
        foreach (var item in filtradas)
            MovimentacoesHoje.Add(item);
    }

    private void MostrarFeedback(string mensagem, bool sucesso)
    {
        FeedbackMessage = mensagem;
        FeedbackIsSuccess = sucesso;
        HasFeedback = true;
    }

    private static string CriarMensagemAluno(AcessoAlunoDto acesso, bool eraSaida)
    {
        if (eraSaida)
            return $"Saída registrada. Permanência na academia: {FormatarDuracao(acesso.Permanencia)}.";

        return acesso.DataFimPlano.HasValue
            ? $"Entrada autorizada. Restam {acesso.DiasRestantesPlano ?? 0} dias de plano " +
              $"(vigente até {acesso.DataFimPlano:dd/MM/yyyy})."
            : "Entrada autorizada e registrada.";
    }

    private static string CriarMensagemColaborador(
        AcessoColaboradorDto acesso, bool eraSaida)
    {
        if (eraSaida)
        {
            return "Saída registrada. Tempo total trabalhado hoje: " +
                   $"{FormatarDuracao(acesso.PermanenciaTotalDia)}.";
        }

        return "Entrada registrada. Tempo já trabalhado hoje: " +
               $"{FormatarDuracao(acesso.PermanenciaTotalDia)} de " +
               $"{FormatarDuracao(acesso.LimiteJornada)} permitidas.";
    }

    private static string FormatarDuracao(TimeSpan? duracao)
    {
        var valor = duracao ?? TimeSpan.Zero;
        var horas = (int)valor.TotalHours;
        return $"{horas:00}h {valor.Minutes:00}min";
    }

    private static string ApenasDigitos(string? value) => new([.. (value ?? string.Empty).Where(char.IsDigit)]);
    private static string Normalizar(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var builder = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed class PessoaAcessoItem
{
    public int Id { get; init; }
    public required string Nome { get; init; }
    public required string Cpf { get; init; }
    public bool EhAluno { get; init; }
    public string Tipo => EhAluno ? "Aluno" : "Colaborador";
    public string CpfFormatado => Cpf;
    public ImageSource? Foto { get; init; }

    public static PessoaAcessoItem FromAluno(AlunoDto pessoa) => Criar(pessoa, true);
    public static PessoaAcessoItem FromColaborador(ColaboradorDto pessoa) => Criar(pessoa, false);
    private static PessoaAcessoItem Criar(PessoaDto pessoa, bool aluno) => new()
    {
        Id = pessoa.Id, Nome = pessoa.Nome, Cpf = pessoa.Cpf, EhAluno = aluno,
        Foto = pessoa.Foto?.Conteudo is { Length: > 0 } bytes
            ? ImageSource.FromStream(() => new MemoryStream(bytes)) : null
    };
}

public sealed record MovimentacaoAcessoItem(string Nome, string Tipo, string Movimento, DateTime DataHora)
{
    public string Horario => DataHora.ToString("HH:mm");
}
