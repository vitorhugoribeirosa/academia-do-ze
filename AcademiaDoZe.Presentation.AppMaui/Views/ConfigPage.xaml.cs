using AcademiaDoZe.Presentation.AppMaui.Messages;
using AcademiaDoZe.Infrastructure.Data;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ConfigPage : ContentPage
{
    private const string SenhaPadraoBanco = "abcBolinhas12345";
    private bool _carregandoBanco;

    public ConfigPage()
    {
        InitializeComponent();
        CarregarTema();
        CarregarBanco();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarTema();
        CarregarBanco();
    }

    private void CarregarTema()
    {
        TemaPicker.SelectedIndex = Preferences.Default.Get("Tema", "system") switch
        {
            "light" => 0,
            "dark" => 1,
            _ => 2
        };
    }

    private async void OnSalvarTemaClicked(object? sender, EventArgs e)
    {
        var tema = TemaPicker.SelectedIndex switch
        {
            0 => "light",
            1 => "dark",
            _ => "system"
        };

        Preferences.Default.Set("Tema", tema);
        WeakReferenceMessenger.Default.Send(new TemaPreferencesUpdatedMessage(tema));

        await DisplayAlertAsync("Tema atualizado", "A aparência do aplicativo foi atualizada.", "OK");
        await Shell.Current.GoToAsync("//dashboard");
    }

    private void CarregarBanco()
    {
        _carregandoBanco = true;

        var tipoSalvo = Preferences.Default.Get("DatabaseType", DatabaseType.Sqlite.ToString());
        DatabaseTypePicker.SelectedIndex = tipoSalvo switch
        {
            nameof(DatabaseType.MySql) => 1,
            nameof(DatabaseType.SqlServer) => 2,
            _ => 0
        };

        _carregandoBanco = false;
        AtualizarInterfacePorTipoBanco();
    }

    private void OnDatabaseTypeChanged(object? sender, EventArgs e)
    {
        if (!_carregandoBanco)
            AtualizarInterfacePorTipoBanco();
    }

    private void AtualizarInterfacePorTipoBanco()
    {
        var tipo = ObterTipoBancoSelecionado();
        if (tipo is null)
            return;

        var sqliteSelecionado = tipo == DatabaseType.Sqlite;
        SqliteContainer.IsVisible = sqliteSelecionado;
        ServidorBancoGrid.IsVisible = !sqliteSelecionado;
        CredenciaisGrid.IsVisible = !sqliteSelecionado;

        if (sqliteSelecionado)
        {
            SqliteCaminhoEntry.Text = Preferences.Default.Get("Sqlite_Caminho", ObterCaminhoPadraoSqlite());
            return;
        }

        var prefixo = tipo == DatabaseType.SqlServer ? "SqlServer" : "MySql";
        const string servidorPadrao = "localhost";
        var usuarioPadrao = tipo == DatabaseType.SqlServer ? "sa" : "root";
        ServidorEntry.Text = Preferences.Default.Get($"{prefixo}_Servidor", servidorPadrao);
        BancoEntry.Text = Preferences.Default.Get($"{prefixo}_Banco", "db_academia_do_ze");
        UsuarioEntry.Text = Preferences.Default.Get($"{prefixo}_Usuario", usuarioPadrao);
        var senhaSalva = Preferences.Default.Get($"{prefixo}_Senha", SenhaPadraoBanco);
        SenhaEntry.Text = string.IsNullOrWhiteSpace(senhaSalva)
            ? SenhaPadraoBanco
            : senhaSalva;
    }

    private async void OnSalvarBancoClicked(object? sender, EventArgs e)
    {
        var tipo = ObterTipoBancoSelecionado();
        if (tipo is null)
        {
            await DisplayAlertAsync("Validação", "Selecione um tipo de banco de dados.", "OK");
            return;
        }

        if (tipo == DatabaseType.Sqlite)
        {
            if (string.IsNullOrWhiteSpace(SqliteCaminhoEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o caminho do arquivo SQLite.", "OK");
                return;
            }

            Preferences.Default.Set("Sqlite_Caminho", SqliteCaminhoEntry.Text.Trim());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ServidorEntry.Text)
                || string.IsNullOrWhiteSpace(BancoEntry.Text)
                || string.IsNullOrWhiteSpace(UsuarioEntry.Text))
            {
                await DisplayAlertAsync(
                    "Validação",
                    "Informe o servidor, o banco de dados e o usuário.",
                    "OK");
                return;
            }

            var prefixo = tipo == DatabaseType.SqlServer ? "SqlServer" : "MySql";
            Preferences.Default.Set($"{prefixo}_Servidor", ServidorEntry.Text.Trim());
            Preferences.Default.Set($"{prefixo}_Banco", BancoEntry.Text.Trim());
            Preferences.Default.Set($"{prefixo}_Usuario", UsuarioEntry.Text.Trim());
            Preferences.Default.Set($"{prefixo}_Senha", SenhaEntry.Text ?? string.Empty);
        }

        Preferences.Default.Set("DatabaseType", tipo.Value.ToString());
        WeakReferenceMessenger.Default.Send(new BancoPreferencesUpdatedMessage(tipo.Value.ToString()));

        await DisplayAlertAsync(
            "Configuração salva",
            $"As configurações do banco {ObterNomeExibicao(tipo.Value)} foram salvas.",
            "OK");
        await Shell.Current.GoToAsync("//dashboard");
    }

    private DatabaseType? ObterTipoBancoSelecionado()
    {
        return DatabaseTypePicker.SelectedIndex switch
        {
            0 => DatabaseType.Sqlite,
            1 => DatabaseType.MySql,
            2 => DatabaseType.SqlServer,
            _ => null
        };
    }

    private static string ObterNomeExibicao(DatabaseType tipo)
    {
        return tipo switch
        {
            DatabaseType.Sqlite => "SQLite",
            DatabaseType.MySql => "MySQL",
            DatabaseType.SqlServer => "SQL Server",
            _ => tipo.ToString()
        };
    }

    private static string ObterCaminhoPadraoSqlite()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "db_academia_do_ze.db");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return Path.Combine(FileSystem.AppDataDirectory, "db_academia_do_ze.db");
    }

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//dashboard");
    }
}
