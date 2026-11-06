using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(MatriculaId), "Id")]
public partial class MatriculaViewModel : BaseViewModel
{
    private const int MaxLaudoSizeBytes = 15 * 1024 * 1024;

    private readonly IMatriculaService _matriculaService;
    private readonly IAlunoService _alunoService;
    private bool _initialized;
    private bool _sincronizandoRestricoes;

    public IReadOnlyList<AppMatriculaPlano> PlanoTipos { get; } =
        [.. Enum.GetValues<AppMatriculaPlano>()];

    public ObservableCollection<RestricaoSelectable> RestricoesMulti { get; } = [];
    public ObservableCollection<RestricaoSelectable> RestricoesFiltradas { get; } = [];

    private string _restricaoSearchText = string.Empty;
    public string RestricaoSearchText
    {
        get => _restricaoSearchText;
        set
        {
            if (SetProperty(ref _restricaoSearchText, value))
                FiltrarRestricoes();
        }
    }

    public bool HasRestricoesFiltradas => RestricoesFiltradas.Count > 0;
    public bool HasNoRestricoesFiltradas => !HasRestricoesFiltradas;

    private MatriculaDto _matricula = CriarNovaMatricula();
    public MatriculaDto Matricula
    {
        get => _matricula;
        set
        {
            if (SetProperty(ref _matricula, value))
                NotificarPropriedadesVisuais();
        }
    }

    private int _matriculaId;
    public int MatriculaId
    {
        get => _matriculaId;
        set => SetProperty(ref _matriculaId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _cpfBusca = string.Empty;
    public string CpfBusca
    {
        get => _cpfBusca;
        set => SetProperty(ref _cpfBusca, value);
    }

    private AppMatriculaPlano _planoSelecionado = AppMatriculaPlano.Mensal;
    public AppMatriculaPlano PlanoSelecionado
    {
        get => _planoSelecionado;
        set
        {
            if (!SetProperty(ref _planoSelecionado, value))
                return;

            Matricula.Plano = value;
            RecalcularDataFim();
        }
    }

    private DateOnly _dataInicio = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly DataInicio
    {
        get => _dataInicio;
        set
        {
            if (!SetProperty(ref _dataInicio, value))
                return;

            Matricula.DataInicio = value;
            RecalcularDataFim();
        }
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (SetProperty(ref _validationMessage, value))
                OnPropertyChanged(nameof(HasValidationErrors));
        }
    }

    public string DataFimFormatada => Matricula.DataFim.ToString("dd/MM/yyyy");
    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool HasAlunoVinculado => Matricula.AlunoMatricula.Id > 0;
    public bool HasNoAlunoVinculado => !HasAlunoVinculado;
    public bool IsMenor16 => HasAlunoVinculado &&
        Matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
    public bool HasRestricoes => Matricula.RestricoesMedicas != AppMatriculaRestricoes.None;
    public bool ExigeLaudo => IsMenor16 || HasRestricoes;
    public bool HasLaudo => Matricula.LaudoMedico?.Conteudo is { Length: > 0 };
    public bool HasNoLaudo => !HasLaudo;
    public bool LaudoPendente => ExigeLaudo && !HasLaudo;

    public MatriculaViewModel(IMatriculaService matriculaService, IAlunoService alunoService)
    {
        _matriculaService = matriculaService;
        _alunoService = alunoService;
        Title = "Nova Matrícula";
        BuildRestricoesMulti();
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        ValidationMessage = string.Empty;

        if (MatriculaId > 0)
        {
            IsEditMode = true;
            Title = "Editar Matrícula";
            await LoadMatriculaAsync();
            return;
        }

        IsEditMode = false;
        Title = "Nova Matrícula";
        Matricula = CriarNovaMatricula();
        CpfBusca = string.Empty;
        PlanoSelecionado = Matricula.Plano;
        DataInicio = Matricula.DataInicio;
        BuildRestricoesMulti();
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task LoadMatriculaAsync()
    {
        if (MatriculaId <= 0 || IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var matricula = await _matriculaService.ObterPorIdAsync(MatriculaId, cts.Token);

            if (matricula is null)
            {
                await InAppDialogService.ShowAsync(
                    "Não Encontrada",
                    $"A matrícula de ID {MatriculaId} não foi encontrada.",
                    "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            Matricula = matricula;
            CpfBusca = matricula.AlunoMatricula.Cpf;
            PlanoSelecionado = matricula.Plano;
            DataInicio = matricula.DataInicio;
            BuildRestricoesMulti();
            NotificarPropriedadesVisuais();
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "O carregamento da matrícula expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao carregar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchByCpfAsync()
    {
        if (IsBusy)
            return;

        var cpf = ApenasDigitos(CpfBusca);
        if (cpf.Length != 11)
        {
            await InAppDialogService.ShowAsync("Validação", "Informe um CPF com 11 dígitos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var aluno = await _alunoService.ObterPorCpfAsync(cpf, cts.Token);

            if (aluno is null)
            {
                await InAppDialogService.ShowAsync(
                    "Aluno Não Encontrado",
                    "Nenhum aluno foi encontrado com o CPF informado.",
                    "OK");
                return;
            }

            if (!IsEditMode && await _matriculaService.PossuiMatriculaAtivaAsync(aluno.Id, cts.Token))
            {
                await InAppDialogService.ShowAsync(
                    "Matrícula Ativa",
                    $"{aluno.Nome} já possui uma matrícula ativa.",
                    "OK");
                return;
            }

            Matricula.AlunoMatricula = aluno;
            CpfBusca = aluno.Cpf;
            ValidationMessage = string.Empty;
            OnPropertyChanged(nameof(Matricula));
            NotificarPropriedadesVisuais();
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A busca do aluno expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao buscar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickLaudoAsync()
    {
        if (IsBusy)
            return;

        try
        {
            var arquivo = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione a imagem do laudo médico",
                FileTypes = FilePickerFileType.Images
            });

            if (arquivo is not null)
                await AplicarLaudoAsync(arquivo);
        }
        catch (OperationCanceledException)
        {
            // O usuário cancelou a seleção.
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Não foi possível selecionar o laudo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task CaptureLaudoAsync()
    {
        if (IsBusy)
            return;

        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await InAppDialogService.ShowAsync("Câmera Indisponível", "Este dispositivo não suporta captura de fotos.", "OK");
            return;
        }

        try
        {
            var arquivo = await PhotoCaptureService.CaptureAsync("Fotografar laudo médico");
            if (arquivo is not null)
                await AplicarLaudoAsync(arquivo);
        }
        catch (OperationCanceledException)
        {
            // O usuário cancelou a captura.
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Não foi possível fotografar o laudo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void RemoveLaudo()
    {
        Matricula.LaudoMedico = null;
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    private async Task SaveMatriculaAsync()
    {
        if (IsBusy)
            return;

        RecomputeRestricoesFlags();
        Matricula.Plano = PlanoSelecionado;
        Matricula.DataInicio = DataInicio;
        RecalcularDataFim();

        if (!await ValidateMatriculaAsync())
            return;

        Matricula.Objetivo = Matricula.Objetivo.Trim();
        Matricula.ObservacoesRestricoes = Matricula.ObservacoesRestricoes?.Trim();

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            if (IsEditMode)
            {
                await _matriculaService.AtualizarAsync(Matricula, cts.Token);
                await InAppDialogService.ShowAsync("Sucesso", "Matrícula atualizada com sucesso!", "OK");
            }
            else
            {
                await _matriculaService.AdicionarAsync(Matricula, cts.Token);
                await InAppDialogService.ShowAsync("Sucesso", "Matrícula cadastrada com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A gravação da matrícula expirou.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await InAppDialogService.ShowAsync("Regra de Negócio", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            await InAppDialogService.ShowAsync("Validação", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao salvar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ValidateMatriculaAsync()
    {
        var erros = new List<string>();

        if (!HasAlunoVinculado)
            erros.Add("- Busque e selecione um aluno pelo CPF.");

        if (DataInicio == default)
            erros.Add("- Data de início é obrigatória.");

        if (string.IsNullOrWhiteSpace(Matricula.Objetivo))
            erros.Add("- Objetivo da matrícula é obrigatório.");

        if (ExigeLaudo && !HasLaudo)
            erros.Add("- O laudo médico é obrigatório para este aluno.");

        if (erros.Count == 0)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        ValidationMessage = string.Join("\n", erros);
        await InAppDialogService.ShowAsync(
            "Erros de Validação",
            "Por favor, corrija os seguintes campos:\n\n" + ValidationMessage,
            "OK");
        return false;
    }

    private void BuildRestricoesMulti()
    {
        _sincronizandoRestricoes = true;
        try
        {
            foreach (var item in RestricoesMulti)
                item.PropertyChanged -= RestricaoItemPropertyChanged;

            RestricoesMulti.Clear();
            foreach (var value in Enum.GetValues<AppMatriculaRestricoes>())
            {
                if (value == AppMatriculaRestricoes.None)
                    continue;

                var item = new RestricaoSelectable
                {
                    Value = value,
                    IsSelected = Matricula.RestricoesMedicas.HasFlag(value)
                };
                item.PropertyChanged += RestricaoItemPropertyChanged;
                RestricoesMulti.Add(item);
            }
        }
        finally
        {
            _sincronizandoRestricoes = false;
        }

        FiltrarRestricoes();
    }

    private void FiltrarRestricoes()
    {
        var termo = NormalizarTexto(RestricaoSearchText);
        var itens = string.IsNullOrEmpty(termo)
            ? RestricoesMulti
            : RestricoesMulti.Where(item =>
                NormalizarTexto(item.Value.GetDisplayName()).Contains(termo, StringComparison.Ordinal));

        RestricoesFiltradas.Clear();
        foreach (var item in itens)
            RestricoesFiltradas.Add(item);

        OnPropertyChanged(nameof(HasRestricoesFiltradas));
        OnPropertyChanged(nameof(HasNoRestricoesFiltradas));
    }

    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var caracteres = texto.Trim().Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(caracteres).ToUpperInvariant();
    }

    private void RestricaoItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_sincronizandoRestricoes || e.PropertyName != nameof(RestricaoSelectable.IsSelected))
            return;

        RecomputeRestricoesFlags();
        NotificarPropriedadesVisuais();
    }

    private void RecomputeRestricoesFlags()
    {
        var restricoes = AppMatriculaRestricoes.None;
        foreach (var item in RestricoesMulti.Where(item => item.IsSelected))
            restricoes |= item.Value;

        Matricula.RestricoesMedicas = restricoes;
        OnPropertyChanged(nameof(Matricula));
    }

    private void RecalcularDataFim()
    {
        Matricula.DataFim = PlanoSelecionado switch
        {
            AppMatriculaPlano.Mensal => DataInicio.AddMonths(1),
            AppMatriculaPlano.Trimestral => DataInicio.AddMonths(3),
            AppMatriculaPlano.Semestral => DataInicio.AddMonths(6),
            AppMatriculaPlano.Anual => DataInicio.AddMonths(12),
            _ => DataInicio
        };
        OnPropertyChanged(nameof(DataFimFormatada));
        OnPropertyChanged(nameof(Matricula));
    }

    private async Task AplicarLaudoAsync(FileResult arquivo)
    {
        await using var source = await PhotoCaptureService.OpenReadAsync(arquivo);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        var totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            totalBytes += bytesRead;
            if (totalBytes > MaxLaudoSizeBytes)
            {
                await InAppDialogService.ShowAsync("Arquivo Muito Grande", "O laudo deve possuir no máximo 15 MB.", "OK");
                return;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        if (totalBytes == 0)
        {
            await InAppDialogService.ShowAsync("Arquivo Inválido", "O arquivo selecionado está vazio.", "OK");
            return;
        }

        Matricula.LaudoMedico = new ArquivoDto { Conteudo = destination.ToArray() };
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    private void NotificarPropriedadesVisuais()
    {
        OnPropertyChanged(nameof(HasAlunoVinculado));
        OnPropertyChanged(nameof(HasNoAlunoVinculado));
        OnPropertyChanged(nameof(IsMenor16));
        OnPropertyChanged(nameof(HasRestricoes));
        OnPropertyChanged(nameof(ExigeLaudo));
        OnPropertyChanged(nameof(HasLaudo));
        OnPropertyChanged(nameof(HasNoLaudo));
        OnPropertyChanged(nameof(LaudoPendente));
        OnPropertyChanged(nameof(DataFimFormatada));
    }

    private static string ApenasDigitos(string? value) =>
        new([.. (value ?? string.Empty).Where(char.IsDigit)]);

    private static MatriculaDto CriarNovaMatricula()
    {
        var inicio = DateOnly.FromDateTime(DateTime.Today);
        return new MatriculaDto
        {
            AlunoMatricula = CriarAlunoVazio(),
            Plano = AppMatriculaPlano.Mensal,
            DataInicio = inicio,
            DataFim = inicio.AddMonths(1),
            Objetivo = string.Empty,
            RestricoesMedicas = AppMatriculaRestricoes.None,
            ObservacoesRestricoes = string.Empty,
            LaudoMedico = null
        };
    }

    private static AlunoDto CriarAlunoVazio() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = default,
        Telefone = string.Empty,
        Numero = string.Empty,
        Foto = new ArquivoDto { Conteudo = [] }
    };
}
