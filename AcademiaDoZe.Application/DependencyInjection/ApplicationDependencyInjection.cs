using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Services;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using AcademiaDoZe.Infrastructure.Data;

namespace AcademiaDoZe.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ILogradouroService, LogradouroService>();
        services.AddTransient<IAlunoService, AlunoService>();
        services.AddTransient<IColaboradorService, ColaboradorService>();
        services.AddTransient<IMatriculaService, MatriculaService>();

        services.AddTransient(provider =>
        {
            var config = provider.GetRequiredService<RepositoryConfig>();
            return CriarRepositoryFactory<ILogradouroRepository>(config,
                static (connectionString, databaseType) =>
                    new LogradouroRepository(connectionString, databaseType));
        });
        services.AddTransient(provider =>
        {
            var config = provider.GetRequiredService<RepositoryConfig>();
            return CriarRepositoryFactory<IAlunoRepository>(config,
                static (connectionString, databaseType) =>
                    new AlunoRepository(connectionString, databaseType));
        });
        services.AddTransient(provider =>
        {
            var config = provider.GetRequiredService<RepositoryConfig>();
            return CriarRepositoryFactory<IColaboradorRepository>(config,
                static (connectionString, databaseType) =>
                    new ColaboradorRepository(connectionString, databaseType));
        });
        services.AddTransient(provider =>
        {
            var config = provider.GetRequiredService<RepositoryConfig>();
            return CriarRepositoryFactory<IMatriculaRepository>(config,
                static (connectionString, databaseType) =>
                    new MatriculaRepository(connectionString, databaseType));
        });

        return services;
    }

    private static Func<TRepository> CriarRepositoryFactory<TRepository>(
        RepositoryConfig config,
        Func<string, DatabaseType, TRepository> criarRepository)
        where TRepository : class
    {
        var sincronizacao = new object();
        TRepository? repositoryAtual = null;
        string? connectionStringAtual = null;
        DatabaseType? databaseTypeAtual = null;

        return () =>
        {
            lock (sincronizacao)
            {
                var connectionString = config.ConnectionString;
                var databaseType = config.DatabaseType;

                if (repositoryAtual is null
                    || connectionStringAtual != connectionString
                    || databaseTypeAtual != databaseType)
                {
                    if (repositoryAtual is IDisposable disposable)
                        disposable.Dispose();

                    repositoryAtual = criarRepository(connectionString, databaseType);
                    connectionStringAtual = connectionString;
                    databaseTypeAtual = databaseType;
                }

                return repositoryAtual;
            }
        };
    }
}
