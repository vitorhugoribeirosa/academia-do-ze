using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.Services;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class LogradouroListPage : ContentPage
{
    public LogradouroListPage(LogradouroListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LogradouroListViewModel viewModel)
            await viewModel.LoadLogradourosCommand.ExecuteAsync(null);
    }

    private async void OnEditButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button button &&
                button.BindingContext is LogradouroDto logradouro &&
                BindingContext is LogradouroListViewModel viewModel)
            {
                await viewModel.EditLogradouroCommand.ExecuteAsync(logradouro);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao editar logradouro: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button button &&
                button.BindingContext is LogradouroDto logradouro &&
                BindingContext is LogradouroListViewModel viewModel)
            {
                await viewModel.DeleteLogradouroCommand.ExecuteAsync(logradouro);
            }
        }
        catch (Exception ex)
        {
            await InAppDialogService.ShowAsync("Erro", $"Erro ao excluir logradouro: {ex.Message}", "OK");
        }
    }

    private void OnFilterSelectorTapped(object? sender, TappedEventArgs e)
    {
        FilterOptionsPanel.IsVisible = !FilterOptionsPanel.IsVisible;
        FilterArrowLabel.Text = FilterOptionsPanel.IsVisible ? "▲" : "▼";
    }

    private void OnFilterOptionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string filterType &&
            BindingContext is LogradouroListViewModel viewModel)
        {
            viewModel.SelectedFilterType = filterType;
        }

        FilterOptionsPanel.IsVisible = false;
        FilterArrowLabel.Text = "▼";
    }
}
