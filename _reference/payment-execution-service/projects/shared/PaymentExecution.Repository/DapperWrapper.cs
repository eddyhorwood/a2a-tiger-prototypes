using System.Data;
using Dapper;
namespace PaymentExecution.Repository;

public interface IDapperWrapper
{
    Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection dbConnection, string sql, object? param = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection dbConnection, string sql, object? param = null);
    Task<IEnumerable<T>> QueryAsync<T>(IDbConnection dbConnection, string sql, object? param = null);
    Task<int> ExecuteAsync(IDbConnection dbConnection, string sql, object? param = null, IDbTransaction? transaction = null);
    Task<T?> ExecuteScalarAsync<T>(IDbConnection dbConnection, string sql, object? param = null, IDbTransaction? transaction = null);
}

public class DapperWrapper : IDapperWrapper
{
    public async Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection dbConnection, string sql, object? param = null)
    {
        return await dbConnection.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection dbConnection, string sql, object? param = null)
    {
        return await dbConnection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection dbConnection, string sql, object? param = null)
    {
        return await dbConnection.QueryAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(IDbConnection dbConnection, string sql, object? param = null, IDbTransaction? transaction = null)
    {
        return await dbConnection.ExecuteAsync(sql, param, transaction);
    }

    public async Task<T?> ExecuteScalarAsync<T>(IDbConnection dbConnection, string sql, object? param = null, IDbTransaction? transaction = null)
    {
        return await dbConnection.ExecuteScalarAsync<T>(sql, param, transaction);
    }

}
