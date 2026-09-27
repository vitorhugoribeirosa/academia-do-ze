using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var databaseType = DatabaseType.Sqlite;
        var databasePath = FindDatabasePath();
        var connectionString = $"Data Source={databasePath};Default Timeout=5;";

        services.AddSingleton(new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType
        });

        services.AddApplicationServices();
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
