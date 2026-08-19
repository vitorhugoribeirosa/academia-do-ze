using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public abstract class BaseRepository : IDisposable, IAsyncDisposable
{
    protected readonly string ConnectionString;
    protected readonly DatabaseType DatabaseType;

    private DbConnection? _connection;
    private bool _disposed;

    protected BaseRepository(string connectionString, DatabaseType databaseType)
    {
        ConnectionString = connectionString
            ?? throw new InfrastructureException("STRING_CONEXAO_NULA", "String de conexao nao pode ser nula.");
        DatabaseType = databaseType;
    }

    protected virtual async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await DbInitializer.InicializarAsync(ConnectionString, DatabaseType, cancellationToken);

            if (_connection is null)
            {
                _connection = DbProvider.CreateConnection(ConnectionString, DatabaseType);
                await _connection.OpenAsync(cancellationToken);
            }
            else if (_connection.State == ConnectionState.Broken)
            {
                await _connection.CloseAsync();
                await _connection.OpenAsync(cancellationToken);
            }
            else if (_connection.State == ConnectionState.Closed)
            {
                await _connection.OpenAsync(cancellationToken);
            }

            return _connection;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("FALHA_ABRIR_CONEXAO", "Falha ao abrir conexao com o banco de dados.", ex);
        }
    }

    protected virtual async Task<DbCommand> CreateCommandAsync(
        string commandText,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);
        return DbProvider.CreateCommand(commandText, connection);
    }

    protected string FormatInsertQuery(string insertSql) => DbProvider.FormatInsertQuery(insertSql, DatabaseType);
    protected string GetCurrentDateFunction() => DbProvider.GetCurrentDateFunction(DatabaseType);
    protected string GetDateAddDaysExpression(string dateExpr, string daysParam) =>
        DbProvider.GetDateAddDaysExpression(dateExpr, daysParam, DatabaseType);
    protected string GetDateHourExpression(string dateColumn) => DbProvider.GetDateHourExpression(dateColumn, DatabaseType);
    protected string GetDateMonthExpression(string dateColumn) => DbProvider.GetDateMonthExpression(dateColumn, DatabaseType);
    protected string GetDateDayExpression(string dateColumn) => DbProvider.GetDateDayExpression(dateColumn, DatabaseType);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
