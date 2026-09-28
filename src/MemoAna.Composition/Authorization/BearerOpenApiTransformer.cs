using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MemoAna.Composition.Authorization;

/// <summary>Describes the authentication schemes used by MemoAna in generated OpenAPI documents.</summary>
public sealed class BearerOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT"
        };

        document.Components.SecuritySchemes["SeedBasic"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            In = ParameterLocation.Header
        };

        return Task.CompletedTask;
    }
}

/// <summary>Applies OpenAPI security requirements according to endpoint authorization metadata.</summary>
public sealed class AuthorizationOpenApiTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return Task.CompletedTask;
        }

        var authorization = metadata.OfType<IAuthorizeData>().ToArray();
        if (authorization.Length == 0)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];

        bool seedBasic = authorization
            .SelectMany(x => (x.AuthenticationSchemes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(x => string.Equals(x, "SeedBasic", StringComparison.Ordinal));

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    seedBasic ? "SeedBasic" : "Bearer",
                    document: null)] = []
            });

        return Task.CompletedTask;
    }
}
