using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.Services;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class MatriculaListPage : ContentPage
{
    public MatriculaListPage(MatriculaListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MatriculaListViewModel viewModel)
            await viewModel.LoadMatriculasCommand.ExecuteAsync(null);

        await UiAnimationService.EnterPageAsync(AnimatedContent);
    }

    private async void OnEditButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: MatriculaDto matricula } &&
            BindingContext is MatriculaListViewModel viewModel)
        {
            await viewModel.EditMatriculaCommand.ExecuteAsync(matricula);
        }
    }

    private async void OnDeleteButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: MatriculaDto matricula } &&
            BindingContext is MatriculaListViewModel viewModel)
        {
            await viewModel.DeleteMatriculaCommand.ExecuteAsync(matricula);
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
        if (e.Parameter is string filterType && BindingContext is MatriculaListViewModel viewModel)
            viewModel.SelectedFilterType = filterType;

        await UiAnimationService.HidePopupAsync(FilterOptionsPanel);
        FilterArrowLabel.Text = "▼";
    }
}
