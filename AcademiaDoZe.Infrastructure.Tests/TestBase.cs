using AcademiaDoZe.Infrastructure.Data;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]

namespace AcademiaDoZe.Infrastructure.Tests;

public abstract class TestBase
{
    private const DatabaseType SelectedDatabaseType = DatabaseType.SqlServer;

    protected string ConnectionString { get; }
    protected DatabaseType DatabaseType { get; }

    protected TestBase()
    {
        DatabaseType = SelectedDatabaseType;
        var sqlitePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "db_academia_do_ze.db"));

        ConnectionString = DatabaseType switch
        {
            DatabaseType.SqlServer => "Server=localhost;Database=db_academia_do_ze;User Id=sa;Password=abcBolinhas12345;TrustServerCertificate=True;Encrypt=True;",
            DatabaseType.MySql => "Server=localhost;Database=db_academia_do_ze;User Id=root;Password=abcBolinhas12345;Character Set=utf8mb4;",
            DatabaseType.Sqlite => $"Data Source={sqlitePath};Cache=Shared;",
            _ => throw new ArgumentOutOfRangeException(nameof(DatabaseType), DatabaseType, "SGBD nao suportado para testes.")
        };
    }

    private static int _counter = 10000;

    protected static string GerarCep() =>
        (80000000 + (int)(DateTime.UtcNow.Ticks % 8000000) + Interlocked.Increment(ref _counter)).ToString("D8")[..8];

    protected static string GerarEmail() => $"user_{Guid.NewGuid().ToString("N")[..8]}@test.com";
    protected static string GerarTelefone() =>
        (49990000000L + (DateTime.UtcNow.Ticks % 8000000000L) + Interlocked.Increment(ref _counter)).ToString("D11")[..11];
}
