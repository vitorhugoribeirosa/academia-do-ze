using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class ColaboradorListViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;

    public ObservableCollection<string> FilterTypes { get; } =
        ["Nome", "CPF", "Id", "Email", "Tipo", "Vínculo"];

    private ObservableCollection<ColaboradorDto> _colaboradores = [];
    public ObservableCollection<ColaboradorDto> Colaboradores
    {
        get => _colaboradores;
        set => SetProperty(ref _colaboradores, value);
    }

    private ColaboradorDto? _selectedColaborador;
    public ColaboradorDto? SelectedColaborador
    {
        get => _selectedColaborador;
        set => SetProperty(ref _selectedColaborador, value);
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

    public ColaboradorListViewModel(IColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
        Title = "Colaboradores";
    }

    [RelayCommand]
    private async Task AddColaboradorAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("colaborador");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao navegar para o cadastro: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task EditColaboradorAsync(ColaboradorDto? colaborador)
    {
        if (colaborador is null)
            return;

        try
        {
            await Shell.Current.GoToAsync($"colaborador?Id={colaborador.Id}");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao navegar para a edição: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadColaboradoresAsync();
    }

    [RelayCommand]
    private async Task SearchColaboradoresAsync()
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

            var colaboradores = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
            var resultados = Filtrar(colaboradores, SearchText, SelectedFilterType);
            await SubstituirListaAsync(resultados);
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "A busca de colaboradores expirou. Verifique a conexão com o banco.",
                "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao buscar colaboradores: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadColaboradoresAsync()
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

            var colaboradores = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
            await SubstituirListaAsync(colaboradores);
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "O carregamento dos colaboradores expirou. Verifique a conexão.",
                "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao carregar colaboradores: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteColaboradorAsync(ColaboradorDto? colaborador)
    {
        if (colaborador is null || IsBusy)
            return;

        var confirmou = await InAppDialogService.ShowAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir o colaborador {colaborador.Nome}?",
            "Sim",
            "Não");

        if (!confirmou)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var removeu = await _colaboradorService.RemoverAsync(colaborador.Id, cts.Token);
            if (!removeu)
            {
                await InAppDialogService.ShowAsync(
                    "Erro",
                    "Não foi possível excluir o colaborador.",
                    "OK");
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => Colaboradores.Remove(colaborador));
            await InAppDialogService.ShowAsync(
                "Sucesso",
                "Colaborador excluído com sucesso!",
                "OK");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync(
                "Tempo Esgotado",
                "A exclusão do colaborador expirou. Verifique a conexão.",
                "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao excluir colaborador: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static IEnumerable<ColaboradorDto> Filtrar(
        IEnumerable<ColaboradorDto> colaboradores,
        string texto,
        string tipoFiltro)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return colaboradores;

        var termo = Normalizar(texto.Trim());
        var cpfPesquisado = ApenasDigitos(texto);

        return tipoFiltro switch
        {
            "CPF" => colaboradores.Where(item =>
                ApenasDigitos(item.Cpf).Contains(cpfPesquisado, StringComparison.Ordinal)),
            "Id" => colaboradores.Where(item =>
                item.Id.ToString(CultureInfo.InvariantCulture) == termo),
            "Email" => colaboradores.Where(item =>
                Normalizar(item.Email).Contains(termo, StringComparison.Ordinal)),
            "Tipo" => colaboradores.Where(item =>
                Normalizar(item.Tipo.GetDisplayName()).Contains(termo, StringComparison.Ordinal)),
            "Vínculo" => colaboradores.Where(item =>
                Normalizar(item.Vinculo.GetDisplayName()).Contains(termo, StringComparison.Ordinal)),
            _ => colaboradores.Where(item =>
                Normalizar(item.Nome).Contains(termo, StringComparison.Ordinal))
        };
    }

    private async Task SubstituirListaAsync(IEnumerable<ColaboradorDto> colaboradores)
    {
        var ordenados = colaboradores.OrderBy(item => item.Nome).ToList();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Colaboradores.Clear();
            foreach (var colaborador in ordenados)
                Colaboradores.Add(colaborador);

            OnPropertyChanged(nameof(Colaboradores));
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
