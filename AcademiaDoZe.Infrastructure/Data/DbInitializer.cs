using AcademiaDoZe.Infrastructure.Exceptions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DbInitializer
{
    private static readonly ConcurrentDictionary<string, bool> BancosInicializados = new();

    public static async Task InicializarAsync(
        string connectionString,
        DatabaseType databaseType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var key = $"{databaseType}:{connectionString}";
        if (BancosInicializados.ContainsKey(key))
            return;

        var scriptSql = ObterScript(databaseType);

        try
        {
            await using var connection = DbProvider.CreateConnection(connectionString, databaseType);
            await connection.OpenAsync(cancellationToken);
            await using var command = DbProvider.CreateCommand(scriptSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            BancosInicializados.TryAdd(key, true);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_INICIALIZAR_BANCO", "Erro ao inicializar banco de dados.", ex);
        }
    }

    public static string ObterScript(DatabaseType databaseType)
    {
        var nomeScript = DbProvider.GetScriptName(databaseType);
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(resource => resource.EndsWith(nomeScript, StringComparison.OrdinalIgnoreCase))
            ?? throw new InfrastructureException("SCRIPT_EMBARCADO_NAO_ENCONTRADO", $"Script SQL embarcado '{nomeScript}' nao encontrado.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InfrastructureException("ERRO_LEITURA_SCRIPT", $"Erro ao carregar o script embarcado '{nomeScript}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
