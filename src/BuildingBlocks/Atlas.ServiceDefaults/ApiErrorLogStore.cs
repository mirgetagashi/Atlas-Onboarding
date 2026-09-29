using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atlas.ServiceDefaults;

internal sealed record ApiErrorLogEntry(
    DateTimeOffset At,
    string Service,
    string Method,
    string Path,
    int StatusCode,
    string? ExceptionType,
    string? ExceptionMessage,
    string? InnerException,
    string RequestBody,
    string ResponseBody);

internal interface IApiErrorLogStore
{
    Task WriteAsync(ApiErrorLogEntry entry, CancellationToken cancellationToken);
}

internal sealed class ApiErrorLogOptions
{
    public string ConnectionString { get; }

    public string ServiceName { get; }

    public ApiErrorLogOptions(string connectionString, string serviceName)
    {
        ConnectionString = connectionString;
        ServiceName = serviceName;
    }
}

internal static class ApiErrorLogRegistration
{
    public static void AddApiErrorLog(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var connectionString = configuration.GetConnectionString("ErrorLog");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        services.AddSingleton(new ApiErrorLogOptions(connectionString, serviceName));
        services.AddSingleton<IApiErrorLogStore, SqlApiErrorLogStore>();
        services.AddHostedService<ApiErrorLogInitializer>();
    }
}

/// <summary>Creates atlas_error_logs.ApiErrorLogs once. A failure here does not stop the API.</summary>
internal sealed class ApiErrorLogInitializer : IHostedService
{
    private readonly ApiErrorLogOptions _options;
    private readonly ILogger<ApiErrorLogInitializer> _logger;

    public ApiErrorLogInitializer(ApiErrorLogOptions options, ILogger<ApiErrorLogInitializer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
            var database = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(database) || database.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new InvalidOperationException("ErrorLog connection string has no usable database name.");
            }

            builder.InitialCatalog = "master";
            await using (var master = new SqlConnection(builder.ConnectionString))
            {
                await master.OpenAsync(cancellationToken);
                await using var createDb = master.CreateCommand();
                createDb.CommandText = $"IF DB_ID(N'{database}') IS NULL CREATE DATABASE [{database}];";
                await createDb.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var createTable = connection.CreateCommand();
            createTable.CommandText = """
                IF OBJECT_ID(N'ApiErrorLogs', N'U') IS NULL
                BEGIN
                    CREATE TABLE ApiErrorLogs (
                        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApiErrorLogs PRIMARY KEY,
                        At datetimeoffset NOT NULL,
                        Service nvarchar(80) NOT NULL,
                        Method nvarchar(16) NOT NULL,
                        Path nvarchar(400) NOT NULL,
                        StatusCode int NOT NULL,
                        ExceptionType nvarchar(300) NULL,
                        ExceptionMessage nvarchar(4000) NULL,
                        InnerException nvarchar(4000) NULL,
                        RequestBody nvarchar(max) NULL,
                        ResponseBody nvarchar(max) NULL
                    );
                    CREATE INDEX IX_ApiErrorLogs_At ON ApiErrorLogs (At);
                END
                """;
            await createTable.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "ApiErrorLogs table was not created; error rows will not be stored");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class SqlApiErrorLogStore : IApiErrorLogStore
{
    private readonly ApiErrorLogOptions _options;
    private readonly ILogger<SqlApiErrorLogStore> _logger;

    public SqlApiErrorLogStore(ApiErrorLogOptions options, ILogger<SqlApiErrorLogStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task WriteAsync(ApiErrorLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ApiErrorLogs
                    (At, Service, Method, Path, StatusCode, ExceptionType, ExceptionMessage, InnerException, RequestBody, ResponseBody)
                VALUES
                    (@At, @Service, @Method, @Path, @StatusCode, @ExceptionType, @ExceptionMessage, @InnerException, @RequestBody, @ResponseBody);
                """;
            command.Parameters.AddWithValue("@At", entry.At);
            command.Parameters.AddWithValue("@Service", Fit(entry.Service, 80));
            command.Parameters.AddWithValue("@Method", Fit(entry.Method, 16));
            command.Parameters.AddWithValue("@Path", Fit(entry.Path, 400));
            command.Parameters.AddWithValue("@StatusCode", entry.StatusCode);
            command.Parameters.AddWithValue("@ExceptionType", (object?)Fit(entry.ExceptionType, 300) ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExceptionMessage", (object?)Fit(entry.ExceptionMessage, 4000) ?? DBNull.Value);
            command.Parameters.AddWithValue("@InnerException", (object?)Fit(entry.InnerException, 4000) ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequestBody", (object?)entry.RequestBody ?? DBNull.Value);
            command.Parameters.AddWithValue("@ResponseBody", (object?)entry.ResponseBody ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to store API error log for {Method} {Path}", entry.Method, entry.Path);
        }
    }

    private static string? Fit(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length <= max ? value : value[..max];
    }
}
