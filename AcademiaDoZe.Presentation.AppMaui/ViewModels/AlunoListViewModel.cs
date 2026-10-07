using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AlunoListViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;

    public ObservableCollection<string> FilterTypes { get; } =
        ["Nome", "CPF", "Id", "Email"];

    private ObservableCollection<AlunoDto> _alunos = [];
    public ObservableCollection<AlunoDto> Alunos
    {
        get => _alunos;
        set => SetProperty(ref _alunos, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public AlunoListViewModel(IAlunoService alunoService)
    {
        _alunoService = alunoService;
        Title = "Alunos";
    }

    [RelayCommand]
    private async Task AddAlunoAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("aluno");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao navegar para o cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditAlunoAsync(AlunoDto? aluno)
    {
        if (aluno is null)
            return;

        try
        {
            await Shell.Current.GoToAsync($"aluno?Id={aluno.Id}");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao navegar para a edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadAlunosAsync();
    }

    [RelayCommand]
    private async Task SearchAlunosAsync()
    {
        if (IsBusy)
            return;

        if (SelectedFilterType == "Id" &&
            !string.IsNullOrWhiteSpace(SearchText) &&
            (!int.TryParse(SearchText.Trim(), out var id) || id <= 0))
        {
            await InAppDialogService.ShowAsync(
                "Validação",
                "Para buscar por ID, informe um número inteiro positivo válido.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunos = await _alunoService.ObterTodosAsync(cts.Token) ?? [];
            await SubstituirListaAsync(Filtrar(alunos, SearchText, SelectedFilterType));
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A busca de alunos expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao buscar alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadAlunosAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunos = await _alunoService.ObterTodosAsync(cts.Token) ?? [];
            await SubstituirListaAsync(alunos);
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "O carregamento dos alunos expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao carregar alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAlunoAsync(AlunoDto? aluno)
    {
        if (aluno is null || IsBusy)
            return;

        var confirmou = await InAppDialogService.ShowAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir o aluno {aluno.Nome}?",
            "Sim",
            "Não");

        if (!confirmou)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var removeu = await _alunoService.RemoverAsync(aluno.Id, cts.Token);

            if (!removeu)
            {
                await InAppDialogService.ShowAsync("Erro", "Não foi possível excluir o aluno.", "OK");
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => Alunos.Remove(aluno));
            await InAppDialogService.ShowAsync("Sucesso", "Aluno excluído com sucesso!", "OK");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A exclusão do aluno expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao excluir aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static IEnumerable<AlunoDto> Filtrar(
        IEnumerable<AlunoDto> alunos,
        string texto,
        string tipoFiltro)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return alunos;

        var termo = Normalizar(texto.Trim());
        var cpfPesquisado = ApenasDigitos(texto);

        return tipoFiltro switch
        {
            "CPF" => alunos.Where(item =>
                ApenasDigitos(item.Cpf).Contains(cpfPesquisado, StringComparison.Ordinal)),
            "Id" => alunos.Where(item =>
                item.Id.ToString(CultureInfo.InvariantCulture) == termo),
            "Email" => alunos.Where(item =>
                Normalizar(item.Email).Contains(termo, StringComparison.Ordinal)),
            _ => alunos.Where(item =>
                Normalizar(item.Nome).Contains(termo, StringComparison.Ordinal))
        };
    }

    private async Task SubstituirListaAsync(IEnumerable<AlunoDto> alunos)
    {
        var ordenados = alunos.OrderBy(item => item.Nome).ToList();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Alunos.Clear();
            foreach (var aluno in ordenados)
                Alunos.Add(aluno);

            OnPropertyChanged(nameof(Alunos));
        });
    }

    private static string ApenasDigitos(string? value) =>
        new([.. (value ?? string.Empty).Where(char.IsDigit)]);

    private static string Normalizar(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
