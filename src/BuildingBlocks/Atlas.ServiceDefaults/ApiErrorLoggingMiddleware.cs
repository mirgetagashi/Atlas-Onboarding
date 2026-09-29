using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.ServiceDefaults;

/// <summary>
/// On every service: when an endpoint fails, write one log with the route, the exception chain
/// (including inner exceptions), and the request and response bodies. Binary bodies are not stored.
/// </summary>
internal static class ApiErrorLoggingMiddleware
{
    private const int MaxCharacters = 8_000;

    public static WebApplication UseApiErrorLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Atlas.ApiErrors");
            var requestBody = await CaptureRequestAsync(context.Request);

            var originalResponse = context.Response.Body;
            await using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            Exception? thrown = null;
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                thrown = exception;
                throw;
            }
            finally
            {
                context.Response.Body = originalResponse;
                responseBuffer.Position = 0;
                var responseBody = await CaptureAsync(responseBuffer, context.Response.ContentType);
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponse);

                var handled = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var error = handled ?? thrown;
                if (error is not null || context.Response.StatusCode >= StatusCodes.Status400BadRequest)
                {
                    Log(logger, context, requestBody, responseBody, error);
                    await StoreAsync(context, requestBody, responseBody, error);
                }
            }
        });

        return app;
    }

    private static async Task StoreAsync(HttpContext context, string requestBody, string responseBody, Exception? error)
    {
        var store = context.RequestServices.GetService<IApiErrorLogStore>();
        var options = context.RequestServices.GetService<ApiErrorLogOptions>();
        if (store is null || options is null)
        {
            return;
        }

        var clock = context.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;
        await store.WriteAsync(
            new ApiErrorLogEntry(
                clock.GetUtcNow(),
                options.ServiceName,
                context.Request.Method,
                $"{context.Request.Path}{context.Request.QueryString}",
                context.Response.StatusCode,
                error?.GetType().FullName,
                error?.Message,
                DescribeInner(error),
                requestBody,
                responseBody),
            context.RequestAborted);
    }

    private static void Log(ILogger logger, HttpContext context, string requestBody, string responseBody, Exception? error)
    {
        var endpoint = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
        logger.LogError(
            error,
            "Endpoint {Endpoint} failed with {StatusCode}. InnerException: {InnerException}. Request: {RequestBody}. Response: {ResponseBody}",
            endpoint,
            context.Response.StatusCode,
            DescribeInner(error),
            requestBody,
            responseBody);
    }

    private static string? DescribeInner(Exception? exception)
    {
        if (exception?.InnerException is null)
        {
            return null;
        }

        var chain = new StringBuilder();
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (chain.Length > 0)
            {
                chain.Append(" | ");
            }

            chain.Append(inner.GetType().Name);
            chain.Append(": ");
            chain.Append(inner.Message);
        }

        return chain.ToString();
    }

    private static async Task<string> CaptureRequestAsync(HttpRequest request)
    {
        if (!IsText(request.ContentType))
        {
            return "[binary request omitted]";
        }

        request.EnableBuffering();
        var body = await CaptureAsync(request.Body, request.ContentType);
        request.Body.Position = 0;
        return body;
    }

    private static async Task<string> CaptureAsync(Stream body, string? contentType)
    {
        if (!IsText(contentType) && body.CanSeek && body.Length > 0)
        {
            return "[binary body omitted]";
        }

        body.Position = 0;
        using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        return text.Length <= MaxCharacters ? text : text[..MaxCharacters] + "…[truncated]";
    }

    private static bool IsText(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return true;
        }

        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("problem", StringComparison.OrdinalIgnoreCase);
    }
}
