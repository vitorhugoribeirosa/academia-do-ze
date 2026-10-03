using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Presentation.AppMaui.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    private const string SenhaPadraoBanco = "abcBolinhas12345";

    public static void ConfigureServices(IServiceCollection services)
    {
        var (connectionString, databaseType) = ObterConfiguracaoAtual();

        var repositoryConfig = new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType
        };

        services.AddSingleton(repositoryConfig);

        WeakReferenceMessenger.Default.Register<RepositoryConfig, BancoPreferencesUpdatedMessage>(
            repositoryConfig,
            static (config, _) =>
            {
                var (novaConnectionString, novoDatabaseType) = ObterConfiguracaoAtual();
                config.ConnectionString = novaConnectionString;
                config.DatabaseType = novoDatabaseType;
            });

        services.AddApplicationServices();
    }

    public static (string ConnectionString, DatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        var tipoSalvo = Preferences.Default.Get("DatabaseType", DatabaseType.Sqlite.ToString());
        if (!Enum.TryParse<DatabaseType>(tipoSalvo, true, out var databaseType))
            databaseType = DatabaseType.Sqlite;

        return databaseType switch
        {
            DatabaseType.MySql => (MontarConnectionStringServidor(DatabaseType.MySql), DatabaseType.MySql),
            DatabaseType.SqlServer => (MontarConnectionStringServidor(DatabaseType.SqlServer), DatabaseType.SqlServer),
            _ => (MontarConnectionStringSqlite(), DatabaseType.Sqlite)
        };
    }

    private static string MontarConnectionStringSqlite()
    {
        var caminho = Preferences.Default.Get("Sqlite_Caminho", FindDatabasePath());
        if (string.IsNullOrWhiteSpace(caminho))
            caminho = FindDatabasePath();

        var complemento = Preferences.Default.Get("Sqlite_Complemento", "Default Timeout=5;");
        if (string.IsNullOrWhiteSpace(complemento))
            complemento = "Default Timeout=5;";
        return AdicionarComplemento($"Data Source={caminho.Trim()};", complemento);
    }

    private static string MontarConnectionStringServidor(DatabaseType databaseType)
    {
        var prefixo = databaseType == DatabaseType.SqlServer ? "SqlServer" : "MySql";
        var usuarioPadrao = databaseType == DatabaseType.SqlServer ? "sa" : "root";
        var complementoPadrao = databaseType == DatabaseType.SqlServer
            ? "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;"
            : "Connection Timeout=5;Default Command Timeout=30;";

        var servidor = Preferences.Default.Get($"{prefixo}_Servidor", "localhost");
        var banco = Preferences.Default.Get($"{prefixo}_Banco", "db_academia_do_ze");
        var usuario = Preferences.Default.Get($"{prefixo}_Usuario", usuarioPadrao);
        var senha = Preferences.Default.Get($"{prefixo}_Senha", SenhaPadraoBanco);
        if (string.IsNullOrWhiteSpace(senha))
            senha = SenhaPadraoBanco;
        var complemento = Preferences.Default.Get($"{prefixo}_Complemento", complementoPadrao);
        if (string.IsNullOrWhiteSpace(complemento))
            complemento = complementoPadrao;

        var connectionString =
            $"Server={servidor.Trim()};Database={banco.Trim()};User Id={usuario.Trim()};Password={senha};";
        return AdicionarComplemento(connectionString, complemento);
    }

    private static string AdicionarComplemento(string connectionString, string? complemento)
    {
        if (string.IsNullOrWhiteSpace(complemento))
            return connectionString;

        var complementoNormalizado = complemento.Trim();
        return complementoNormalizado.EndsWith(';')
            ? connectionString + complementoNormalizado
            : connectionString + complementoNormalizado + ";";
    }

    private static string FindDatabasePath()
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
}
