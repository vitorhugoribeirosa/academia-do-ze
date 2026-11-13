using AcademiaDoZe.Presentation.AppMaui.Services;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ControleAcessoPage : ContentPage
{
    public ControleAcessoPage(ControleAcessoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ControleAcessoViewModel viewModel)
            await viewModel.LoadCommand.ExecuteAsync(null);
        await UiAnimationService.EnterPageAsync(AnimatedContent);
    }

    private void OnPessoaTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TapGestureRecognizer { BindingContext: PessoaAcessoItem pessoa } &&
            BindingContext is ControleAcessoViewModel viewModel)
        {
            viewModel.SelectedPessoa = pessoa;
        }
    }
}
