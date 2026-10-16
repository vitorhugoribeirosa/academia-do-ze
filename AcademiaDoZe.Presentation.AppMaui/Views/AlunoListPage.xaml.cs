using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.Services;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AlunoListPage : ContentPage
{
    public AlunoListPage(AlunoListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AlunoListViewModel viewModel)
            await viewModel.LoadAlunosCommand.ExecuteAsync(null);

        await UiAnimationService.EnterPageAsync(AnimatedContent);
    }

    private async void OnEditButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: AlunoDto aluno } &&
                BindingContext is AlunoListViewModel viewModel)
            {
                await viewModel.EditAlunoCommand.ExecuteAsync(aluno);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao editar aluno: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: AlunoDto aluno } &&
                BindingContext is AlunoListViewModel viewModel)
            {
                await viewModel.DeleteAlunoCommand.ExecuteAsync(aluno);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao excluir aluno: {ex.Message}", "OK");
        }
    }

    private async void OnFilterSelectorTapped(object? sender, TappedEventArgs e)
    {
        if (FilterOptionsPanel.IsVisible)
        {
            await UiAnimationService.HidePopupAsync(FilterOptionsPanel);
            FilterArrowLabel.Text = "▼";
        }
        else
        {
            FilterArrowLabel.Text = "▲";
            await UiAnimationService.ShowPopupAsync(FilterOptionsPanel);
        }
    }

    private async void OnFilterOptionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string filterType && BindingContext is AlunoListViewModel viewModel)
            viewModel.SelectedFilterType = filterType;

        await UiAnimationService.HidePopupAsync(FilterOptionsPanel);
        FilterArrowLabel.Text = "▼";
    }
}
