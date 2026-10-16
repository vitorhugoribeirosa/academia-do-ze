using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.Services;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ColaboradorListPage : ContentPage
{
    public ColaboradorListPage(ColaboradorListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ColaboradorListViewModel viewModel)
            await viewModel.LoadColaboradoresCommand.ExecuteAsync(null);

        await UiAnimationService.EnterPageAsync(AnimatedContent);
    }

    private async void OnEditButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: ColaboradorDto colaborador } &&
                BindingContext is ColaboradorListViewModel viewModel)
            {
                await viewModel.EditColaboradorCommand.ExecuteAsync(colaborador);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao editar colaborador: {ex.Message}",
                "OK");
        }
    }

    private async void OnDeleteButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: ColaboradorDto colaborador } &&
                BindingContext is ColaboradorListViewModel viewModel)
            {
                await viewModel.DeleteColaboradorCommand.ExecuteAsync(colaborador);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync(
                "Erro",
                $"Erro ao excluir colaborador: {ex.Message}",
                "OK");
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
        if (e.Parameter is string filterType &&
            BindingContext is ColaboradorListViewModel viewModel)
        {
            viewModel.SelectedFilterType = filterType;
        }

        await UiAnimationService.HidePopupAsync(FilterOptionsPanel);
        FilterArrowLabel.Text = "▼";
    }
}
