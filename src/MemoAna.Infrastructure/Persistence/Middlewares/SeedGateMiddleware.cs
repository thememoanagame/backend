using System.Text.Json;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Seed.Abstractions;
using MemoAna.Application.Seed.Dtos;
using Microsoft.AspNetCore.Components;

namespace MemoAna.Infrastructure.Persistence.Middlewares;

/// <summary>Prevents application access until the required system seed is complete.</summary>
public sealed class SeedGateMiddleware(RequestDelegate next, ILogger<SeedGateMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(
        HttpContext context,
        ISqlSeedService seedService)
    {
        if (ShouldBypass(context))
        {
            await next(context);
            return;
        }

        SeedStatusDto status;
        try
        {
            status = await seedService.GetStatusAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to determine application seed status.");
            await WriteFailureAsync(
                context,
                ["The application initialization status is unavailable."]);
            return;
        }

        if (!status.SeedRequired)
        {
            await next(context);
            return;
        }

        if (IsRazorPageRequest(context))
        {
            context.Response.Redirect("/seed");
            return;
        }

        await WriteFailureAsync(context, status.MissingRequirements);
    }

    private static bool ShouldBypass(HttpContext context)
    {
        PathString path = context.Request.Path;

        if (path.StartsWithSegments("/api/v1/seed")
            || path.StartsWithSegments("/seed")
            || path.StartsWithSegments("/api/openapi")
            || path.StartsWithSegments("/api/scalar"))
        {
            return true;
        }

        if (path.StartsWithSegments("/_blazor")
            || path.StartsWithSegments("/_framework")
            || path.StartsWithSegments("/favicon")
            || path.StartsWithSegments("/css")
            || path.StartsWithSegments("/js")
            || path.StartsWithSegments("/images"))
        {
            return true;
        }

        return false;
    }

    private static bool IsRazorPageRequest(HttpContext context)
    {
        if (context.Request.Method is not ("GET" or "HEAD"))
        {
            return false;
        }

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        string? accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(accept);
    }

    private static async Task WriteFailureAsync(
        HttpContext context,
        IReadOnlyCollection<string> errors)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json; charset=utf-8";

        Response response = ResponseMaker.Failure([.. errors]);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            JsonOptions,
            context.RequestAborted);
    }
}
