using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(ColaboradorId), "Id")]
public partial class ColaboradorViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;
    private readonly ILogradouroService _logradouroService;

    public IReadOnlyList<AppColaboradorTipo> ColaboradorTipos { get; } =
        Enum.GetValues<AppColaboradorTipo>();

    public IReadOnlyList<AppColaboradorVinculo> ColaboradorVinculos { get; } =
        Enum.GetValues<AppColaboradorVinculo>();

    private ColaboradorDto _colaborador = CriarNovoColaborador();
    public ColaboradorDto Colaborador
    {
        get => _colaborador;
        set
        {
            if (SetProperty(ref _colaborador, value))
            {
                OnPropertyChanged(nameof(HasEnderecoVinculado));
                OnPropertyChanged(nameof(HasNoEnderecoVinculado));
            }
        }
    }

    private int _colaboradorId;
    public int ColaboradorId
    {
        get => _colaboradorId;
        set => SetProperty(ref _colaboradorId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    public bool HasEnderecoVinculado => Colaborador.Endereco?.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public ColaboradorViewModel(
        IColaboradorService colaboradorService,
        ILogradouroService logradouroService)
    {
        _colaboradorService = colaboradorService;
        _logradouroService = logradouroService;
        Title = "Novo Colaborador";
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;

        if (ColaboradorId > 0)
        {
            IsEditMode = true;
            Title = "Editar Colaborador";
            await LoadColaboradorAsync();
            return;
        }

        IsEditMode = false;
        Title = "Novo Colaborador";
        Colaborador = CriarNovoColaborador();
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task LoadColaboradorAsync()
    {
        if (ColaboradorId <= 0 || IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var colaborador = await _colaboradorService.ObterPorIdAsync(ColaboradorId, cts.Token);
            if (colaborador is null)
            {
                await InAppDialogService.ShowAsync(
                    "Não Encontrado",
                    $"O colaborador de ID {ColaboradorId} não foi encontrado.",
                    "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            colaborador.Senha = string.Empty;
            colaborador.Foto ??= new ArquivoDto { Conteudo = [] };
            colaborador.Endereco ??= CriarEnderecoVazio();
            Colaborador = colaborador;
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "O carregamento do colaborador expirou. Verifique a conexão.",
                "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao carregar colaborador: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchByCepAsync()
    {
        if (IsBusy)
            return;

        var cep = ApenasDigitos(Colaborador.Endereco?.Cep);
        if (cep.Length != 8)
        {
            await InAppDialogService.ShowAsync(
                "Validação",
                "Informe um CEP com exatamente 8 dígitos.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var endereco = await _logradouroService.ObterPorCepAsync(cep, cts.Token);
            if (endereco is null)
            {
                await InAppDialogService.ShowAsync(
                    "CEP Não Encontrado",
                    "Cadastre primeiro esse logradouro na tela de Logradouros.",
                    "OK");
                return;
            }

            Colaborador.Endereco = endereco;
            OnPropertyChanged(nameof(Colaborador));
            OnPropertyChanged(nameof(HasEnderecoVinculado));
            OnPropertyChanged(nameof(HasNoEnderecoVinculado));
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "A busca do CEP expirou. Verifique a conexão.",
                "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao buscar CEP: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CapturePhotoAsync()
    {
        await InAppDialogService.ShowAsync(
            "Câmera",
            "A captura pela câmera será ativada na Parte 4.",
            "OK");
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        await InAppDialogService.ShowAsync(
            "Galeria",
            "A seleção de imagem da galeria será ativada na Parte 4.",
            "OK");
    }

    [RelayCommand]
    private async Task SaveColaboradorAsync()
    {
        if (IsBusy || !await ValidateColaboradorAsync())
            return;

        NormalizarDados();

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            if (IsEditMode)
            {
                await _colaboradorService.AtualizarAsync(Colaborador, cts.Token);
                await InAppDialogService.ShowAsync(
                    "Sucesso",
                    "Colaborador atualizado com sucesso!",
                    "OK");
            }
            else
            {
                await _colaboradorService.AdicionarAsync(Colaborador, cts.Token);
                await InAppDialogService.ShowAsync(
                    "Sucesso",
                    "Colaborador cadastrado com sucesso!",
                    "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "A gravação do colaborador expirou. Verifique a conexão.",
                "OK");
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
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao salvar colaborador: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ValidateColaboradorAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Colaborador.Nome))
            errors.Add("- Nome é obrigatório.");

        if (ApenasDigitos(Colaborador.Cpf).Length != 11)
            errors.Add("- CPF deve conter 11 dígitos.");

        var limiteNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-12));
        if (Colaborador.DataNascimento == default || Colaborador.DataNascimento > limiteNascimento)
            errors.Add("- O colaborador deve ter pelo menos 12 anos.");

        if (ApenasDigitos(Colaborador.Telefone).Length != 11)
            errors.Add("- Telefone deve conter 11 dígitos, incluindo o DDD.");

        if (string.IsNullOrWhiteSpace(Colaborador.Email) ||
            !Colaborador.Email.Contains('@') ||
            !Colaborador.Email[(Colaborador.Email.IndexOf('@') + 1)..].Contains('.'))
        {
            errors.Add("- Informe um e-mail válido.");
        }

        if (Colaborador.Endereco?.Id is null or <= 0)
            errors.Add("- Busque e vincule um logradouro pelo CEP.");

        if (string.IsNullOrWhiteSpace(Colaborador.Numero))
            errors.Add("- Número do endereço é obrigatório.");

        if (Colaborador.DataAdmissao == default ||
            Colaborador.DataAdmissao > DateOnly.FromDateTime(DateTime.Today))
        {
            errors.Add("- Data de admissão deve ser igual ou anterior à data atual.");
        }

        var informouSenha = !string.IsNullOrWhiteSpace(Colaborador.Senha);
        if (!IsEditMode && !informouSenha)
            errors.Add("- Senha é obrigatória no cadastro.");

        if (informouSenha)
        {
            if (Colaborador.Senha!.Trim().Length < 6 || !Colaborador.Senha.Any(char.IsUpper))
                errors.Add("- Senha deve ter pelo menos 6 caracteres e uma letra maiúscula.");

            if (!string.Equals(Colaborador.Senha, ConfirmarSenha, StringComparison.Ordinal))
                errors.Add("- Senha e confirmação devem ser iguais.");
        }

        if (errors.Count == 0)
            return true;

        await InAppDialogService.ShowAsync(
            "Erros de Validação",
            "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors),
            "OK");
        return false;
    }

    private void NormalizarDados()
    {
        Colaborador.Nome = Colaborador.Nome.Trim();
        Colaborador.Cpf = ApenasDigitos(Colaborador.Cpf);
        Colaborador.Telefone = ApenasDigitos(Colaborador.Telefone);
        Colaborador.Email = Colaborador.Email?.Trim();
        Colaborador.Numero = Colaborador.Numero.Trim();
        Colaborador.Complemento = Colaborador.Complemento?.Trim();
        Colaborador.Senha = string.IsNullOrWhiteSpace(Colaborador.Senha)
            ? null
            : Colaborador.Senha.Trim();
        Colaborador.Foto ??= new ArquivoDto { Conteudo = [] };
    }

    private static string ApenasDigitos(string? value) =>
        new([.. (value ?? string.Empty).Where(char.IsDigit)]);

    private static ColaboradorDto CriarNovoColaborador() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Endereco = CriarEnderecoVazio(),
        Numero = string.Empty,
        Complemento = string.Empty,
        Senha = string.Empty,
        Foto = new ArquivoDto { Conteudo = [] },
        DataAdmissao = DateOnly.FromDateTime(DateTime.Today),
        Tipo = AppColaboradorTipo.Atendente,
        Vinculo = AppColaboradorVinculo.CLT
    };

    private static LogradouroDto CriarEnderecoVazio() => new()
    {
        Cep = string.Empty,
        Nome = string.Empty,
        Bairro = string.Empty,
        Cidade = string.Empty,
        Estado = string.Empty,
        Pais = "Brasil"
    };
}
