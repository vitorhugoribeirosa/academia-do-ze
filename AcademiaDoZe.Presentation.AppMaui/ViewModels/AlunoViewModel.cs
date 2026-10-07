using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : BaseViewModel
{
    private const int MaxPhotoSizeBytes = 15 * 1024 * 1024;

    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;
    private bool _initialized;

    private AlunoDto _aluno = CriarNovoAluno();
    public AlunoDto Aluno
    {
        get => _aluno;
        set
        {
            if (SetProperty(ref _aluno, value))
            {
                OnPropertyChanged(nameof(HasEnderecoVinculado));
                OnPropertyChanged(nameof(HasNoEnderecoVinculado));
                OnPropertyChanged(nameof(HasFoto));
                OnPropertyChanged(nameof(HasNoFoto));
            }
        }
    }

    private int _alunoId;
    public int AlunoId
    {
        get => _alunoId;
        set => SetProperty(ref _alunoId, value);
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

    public bool HasEnderecoVinculado => Aluno.Endereco?.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;
    public bool HasFoto => Aluno.Foto?.Conteudo is { Length: > 0 };
    public bool HasNoFoto => !HasFoto;

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
        Title = "Novo Aluno";
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        ConfirmarSenha = string.Empty;

        if (AlunoId > 0)
        {
            IsEditMode = true;
            Title = "Editar Aluno";
            await LoadAlunoAsync();
            return;
        }

        IsEditMode = false;
        Title = "Novo Aluno";
        Aluno = CriarNovoAluno();
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task LoadAlunoAsync()
    {
        if (AlunoId <= 0 || IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var aluno = await _alunoService.ObterPorIdAsync(AlunoId, cts.Token);

            if (aluno is null)
            {
                await InAppDialogService.ShowAsync("Não Encontrado", $"O aluno de ID {AlunoId} não foi encontrado.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            aluno.Senha = string.Empty;
            aluno.Foto ??= new ArquivoDto { Conteudo = [] };
            aluno.Endereco ??= CriarEnderecoVazio();
            Aluno = aluno;
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "O carregamento do aluno expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao carregar aluno: {ex.Message}", "OK");
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

        var cep = ApenasDigitos(Aluno.Endereco?.Cep);
        if (cep.Length != 8)
        {
            await InAppDialogService.ShowAsync("Validação", "Informe um CEP com exatamente 8 dígitos.", "OK");
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

            Aluno.Endereco = endereco;
            OnPropertyChanged(nameof(Aluno));
            OnPropertyChanged(nameof(HasEnderecoVinculado));
            OnPropertyChanged(nameof(HasNoEnderecoVinculado));
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A busca do CEP expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao buscar CEP: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CapturePhotoAsync()
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
            var photo = await PhotoCaptureService.CaptureAsync("Tirar foto do aluno");
            if (photo is not null)
                await ApplyPhotoAsync(photo);
        }
        catch (FeatureNotSupportedException)
        {
            await InAppDialogService.ShowAsync("Câmera Indisponível", "A captura não é suportada neste dispositivo.", "OK");
        }
        catch (PermissionException)
        {
            await InAppDialogService.ShowAsync("Permissão Necessária", "Verifique a permissão de câmera do aplicativo.", "OK");
        }
        catch (OperationCanceledException)
        {
            // O usuário cancelou a captura.
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Não foi possível capturar a foto. {DetalharErro(ex)}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        if (IsBusy)
            return;

        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Selecione a foto do aluno",
                SelectionLimit = 1
            });
            var photo = photos.FirstOrDefault();
            if (photo is not null)
                await ApplyPhotoAsync(photo);
        }
        catch (FeatureNotSupportedException)
        {
            await InAppDialogService.ShowAsync("Galeria Indisponível", "A seleção de imagens não é suportada.", "OK");
        }
        catch (PermissionException)
        {
            await InAppDialogService.ShowAsync("Permissão Necessária", "Verifique a permissão de fotos do aplicativo.", "OK");
        }
        catch (OperationCanceledException)
        {
            // O usuário cancelou a seleção.
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Não foi possível selecionar a foto: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        Aluno.Foto = null;
        OnPropertyChanged(nameof(Aluno));
        OnPropertyChanged(nameof(HasFoto));
        OnPropertyChanged(nameof(HasNoFoto));
    }

    [RelayCommand]
    private async Task SaveAlunoAsync()
    {
        if (IsBusy || !await ValidateAlunoAsync())
            return;

        NormalizarDados();

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            if (IsEditMode)
            {
                await _alunoService.AtualizarAsync(Aluno, cts.Token);
                await InAppDialogService.ShowAsync("Sucesso", "Aluno atualizado com sucesso!", "OK");
            }
            else
            {
                await _alunoService.AdicionarAsync(Aluno, cts.Token);
                await InAppDialogService.ShowAsync("Sucesso", "Aluno cadastrado com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A gravação do aluno expirou.", "OK");
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
            await InAppDialogService.ShowAsync("Erro", $"Erro ao salvar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ValidateAlunoAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Aluno.Nome))
            errors.Add("- Nome é obrigatório.");

        if (ApenasDigitos(Aluno.Cpf).Length != 11)
            errors.Add("- CPF deve conter 11 dígitos.");

        var limiteNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-12));
        if (Aluno.DataNascimento == default || Aluno.DataNascimento > limiteNascimento)
            errors.Add("- O aluno deve ter pelo menos 12 anos.");

        if (ApenasDigitos(Aluno.Telefone).Length != 11)
            errors.Add("- Telefone deve conter 11 dígitos, incluindo o DDD.");

        if (string.IsNullOrWhiteSpace(Aluno.Email) ||
            !Aluno.Email.Contains('@') ||
            !Aluno.Email[(Aluno.Email.IndexOf('@') + 1)..].Contains('.'))
        {
            errors.Add("- Informe um e-mail válido.");
        }

        if (Aluno.Endereco?.Id is null or <= 0)
            errors.Add("- Busque e vincule um logradouro pelo CEP.");

        if (string.IsNullOrWhiteSpace(Aluno.Numero))
            errors.Add("- Número do endereço é obrigatório.");

        if (!IsEditMode && !HasFoto)
            errors.Add("- Selecione ou tire uma foto do aluno.");

        var informouSenha = !string.IsNullOrWhiteSpace(Aluno.Senha);
        if (!IsEditMode && !informouSenha)
            errors.Add("- Senha é obrigatória no cadastro.");

        if (informouSenha)
        {
            if (Aluno.Senha!.Trim().Length < 6 || !Aluno.Senha.Any(char.IsUpper))
                errors.Add("- Senha deve ter pelo menos 6 caracteres e uma letra maiúscula.");

            if (!string.Equals(Aluno.Senha, ConfirmarSenha, StringComparison.Ordinal))
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
        Aluno.Nome = Aluno.Nome.Trim();
        Aluno.Cpf = ApenasDigitos(Aluno.Cpf);
        Aluno.Telefone = ApenasDigitos(Aluno.Telefone);
        Aluno.Email = Aluno.Email?.Trim();
        Aluno.Numero = Aluno.Numero.Trim();
        Aluno.Complemento = Aluno.Complemento?.Trim();
        Aluno.Senha = string.IsNullOrWhiteSpace(Aluno.Senha) ? null : Aluno.Senha.Trim();
        Aluno.Foto ??= new ArquivoDto { Conteudo = [] };
    }

    private async Task ApplyPhotoAsync(FileResult photo)
    {
        await using var source = await PhotoCaptureService.OpenReadAsync(photo);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        var totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            totalBytes += bytesRead;
            if (totalBytes > MaxPhotoSizeBytes)
            {
                await InAppDialogService.ShowAsync("Imagem Muito Grande", "A foto deve possuir no máximo 15 MB.", "OK");
                return;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        if (totalBytes == 0)
        {
            await InAppDialogService.ShowAsync("Imagem Inválida", "O arquivo selecionado está vazio.", "OK");
            return;
        }

        Aluno.Foto = new ArquivoDto { Conteudo = destination.ToArray() };
        OnPropertyChanged(nameof(Aluno));
        OnPropertyChanged(nameof(HasFoto));
        OnPropertyChanged(nameof(HasNoFoto));
    }

    private static string ApenasDigitos(string? value) =>
        new([.. (value ?? string.Empty).Where(char.IsDigit)]);

    private static string DetalharErro(Exception ex)
    {
        var erro = ex.GetBaseException();
        return string.IsNullOrWhiteSpace(erro.Message)
            ? $"Código: 0x{erro.HResult:X8}."
            : erro.Message;
    }

    private static AlunoDto CriarNovoAluno() => new()
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
        Foto = new ArquivoDto { Conteudo = [] }
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
