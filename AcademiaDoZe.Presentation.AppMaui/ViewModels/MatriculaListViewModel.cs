using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;

    public ObservableCollection<string> FilterTypes { get; } =
        ["Ativas", "Todas", "ID da matrícula", "ID do aluno", "Vencendo em dias"];

    public ObservableCollection<MatriculaDto> Matriculas { get; } = [];

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Ativas";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set
        {
            if (!SetProperty(ref _selectedFilterType, value))
                return;

            SearchText = string.Empty;
            OnPropertyChanged(nameof(SearchPlaceholder));
        }
    }

    public string SearchPlaceholder => SelectedFilterType switch
    {
        "ID da matrícula" => "Digite o ID da matrícula",
        "ID do aluno" => "Digite o ID do aluno",
        "Vencendo em dias" => "Digite a quantidade de dias",
        "Todas" => "Nenhum valor necessário",
        _ => "ID do aluno"
    };

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matrículas";
    }

    [RelayCommand]
    private async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao navegar para o cadastro de matrícula: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task EditMatriculaAsync(MatriculaDto? matricula)
    {
        if (matricula is null)
            return;

        try
        {
            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao navegar para a edição da matrícula: {ex.Message}",
                "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await SearchMatriculasAsync();
    }

    [RelayCommand]
    private async Task LoadMatriculasAsync() => await SearchMatriculasAsync();

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        var filtro = await ValidarFiltroAsync();
        if (!filtro.IsValid)
        {
            IsRefreshing = false;
            return;
        }

        var valorNumerico = filtro.Value;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            IEnumerable<MatriculaDto> resultados = SelectedFilterType switch
            {
                "Todas" => await _matriculaService.ObterTodasAsync(cts.Token),
                "ID da matrícula" => await BuscarPorIdAsync(valorNumerico, cts.Token),
                "ID do aluno" => await _matriculaService.ObterPorAlunoIdAsync(valorNumerico, cts.Token),
                "Vencendo em dias" => await _matriculaService.ObterVencendoEmDiasAsync(valorNumerico, cts.Token),
                _ => await _matriculaService.ObterAtivasAsync(valorNumerico, cts.Token)
            };

            await SubstituirListaAsync(resultados ?? []);
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A busca de matrículas expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao buscar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto? matricula)
    {
        if (matricula is null || IsBusy)
            return;

        var confirmou = await InAppDialogService.ShowAsync(
            "Confirmar Exclusão",
            $"Deseja realmente excluir a matrícula de {matricula.AlunoMatricula.Nome}?",
            "Sim",
            "Não");

        if (!confirmou)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            if (!await _matriculaService.RemoverAsync(matricula.Id, cts.Token))
            {
                await InAppDialogService.ShowAsync("Erro", "Não foi possível excluir a matrícula.", "OK");
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => Matriculas.Remove(matricula));
            await InAppDialogService.ShowAsync("Sucesso", "Matrícula excluída com sucesso!", "OK");
        }
        catch (OperationCanceledException)
        {
            await InAppDialogService.ShowAsync("Tempo Esgotado", "A exclusão da matrícula expirou.", "OK");
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao excluir matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<(bool IsValid, int Value)> ValidarFiltroAsync()
    {
        var valorNumerico = 0;
        var exigeNumero = SelectedFilterType is "ID da matrícula" or "ID do aluno" or "Vencendo em dias";

        if (!exigeNumero)
        {
            if (SelectedFilterType == "Ativas" && !string.IsNullOrWhiteSpace(SearchText) &&
                (!int.TryParse(SearchText.Trim(), out valorNumerico) || valorNumerico < 0))
            {
                await InAppDialogService.ShowAsync(
                    "Validação",
                    "Para filtrar matrículas ativas por aluno, informe um ID válido.",
                    "OK");
                return (false, 0);
            }

            return (true, valorNumerico);
        }

        if (!int.TryParse(SearchText.Trim(), out valorNumerico) ||
            (SelectedFilterType == "Vencendo em dias" ? valorNumerico < 0 : valorNumerico <= 0))
        {
            await InAppDialogService.ShowAsync(
                "Validação",
                SelectedFilterType == "Vencendo em dias"
                    ? "Informe uma quantidade de dias igual ou maior que zero."
                    : "Informe um ID inteiro positivo.",
                "OK");
            return (false, 0);
        }

        return (true, valorNumerico);
    }

    private async Task<IEnumerable<MatriculaDto>> BuscarPorIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var matricula = await _matriculaService.ObterPorIdAsync(id, cancellationToken);
        return matricula is null ? [] : [matricula];
    }

    private async Task SubstituirListaAsync(IEnumerable<MatriculaDto> matriculas)
    {
        var ordenadas = matriculas
            .OrderByDescending(item => item.Id)
            .ToList();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Matriculas.Clear();
            foreach (var matricula in ordenadas)
                Matriculas.Add(matricula);
        });
    }
}
