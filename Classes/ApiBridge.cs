using ConnectLIbrary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaR;
// Удобный мост к API SQL через текущий сервер.
internal static class ApiBridge
{
    private static string ApiUrl => Program.Servers?.CurrentServer?.ApiUrl;
    private static SqlData SqlData => Program.Servers?.SqlData;
    // Асинхронный вызов: параметры через SqlParam; apiUrl берется из текущего сервера
    public static async Task<List<T>> QuerySqlAsync<T>(string query, List<SqlParam> parameters = null)
    {
        if (SqlData == null || string.IsNullOrWhiteSpace(ApiUrl))
            throw new System.InvalidOperationException("API URL не сконфигурирован");
        if (SqlData.ApiKey == null)
            throw new System.InvalidOperationException("API Key не сконфигурирован");
        return await SqlData.GetSqlDataAsync<T>(ApiUrl, query, parameters);
    }
    // Scalar-запрос
    public static async Task<object> ScalarSqlAsync(string query, List<SqlParam> parameters = null)
    {
        if (SqlData == null || string.IsNullOrWhiteSpace(ApiUrl))
            throw new System.InvalidOperationException("API URL не сконфигурирован");
        if (SqlData.ApiKey == null)
            throw new System.InvalidOperationException("API Key не сконфигурирован");
        return await SqlData.GetSqlScalarAsync(ApiUrl, query, parameters);
    }
    // Выполнение DML
    public static async Task<bool> ExecuteSqlAsync(string query, List<SqlParam> parameters = null)
    {
        if (SqlData == null || string.IsNullOrWhiteSpace(ApiUrl))
            throw new System.InvalidOperationException("API URL не сконфигурирован");
        if (SqlData.ApiKey == null)
            throw new System.InvalidOperationException("API Key не сконфигурирован");
        return await SqlData.SetSqlDataAsync(ApiUrl, query, parameters);
    }
    // Транзакция
    public static async Task<bool> TransactionSqlAsync(List<SqlQuery> queries)
    {
        if (SqlData == null || string.IsNullOrWhiteSpace(ApiUrl))
            throw new System.InvalidOperationException("API URL не сконфигурирован");
        if (SqlData.ApiKey == null)
            throw new System.InvalidOperationException("API Key не сконфигурирован");
        return await SqlData.SetTSqlDataAsync(ApiUrl, queries);
    }
}