using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MemoAna.Composition.Authorization;

/// <summary>Describes the MemoAna authentication schemes and applies them to protected OpenAPI operations.</summary>
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

        var apiDescriptions = context.ApplicationServices
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups
            .Items
            .SelectMany(group => group.Items);

        foreach (ApiDescription description in apiDescriptions)
        {
            if (string.IsNullOrWhiteSpace(description.HttpMethod)
                || string.IsNullOrWhiteSpace(description.RelativePath))
            {
                continue;
            }

            string path = "/" + description.RelativePath.TrimStart('/');
            if (!document.Paths.TryGetValue(path, out IOpenApiPathItem? pathItem)
                || pathItem.Operations is null)
            {
                continue;
            }

            HttpMethod httpMethod = new(description.HttpMethod);
            if (!pathItem.Operations.TryGetValue(httpMethod, out OpenApiOperation? operation))
            {
                continue;
            }

            var metadata = description.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<AllowAnonymousAttribute>().Any())
            {
                continue;
            }

            var authorization = metadata.OfType<IAuthorizeData>().ToArray();
            if (authorization.Length == 0)
            {
                continue;
            }

            bool seedBasic = authorization
                .SelectMany(x => (x.AuthenticationSchemes ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Any(x => string.Equals(x, "SeedBasic", StringComparison.Ordinal));

            operation.Security ??= [];
            operation.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        seedBasic ? "SeedBasic" : "Bearer",
                        document)] = []
                });
        }

        return Task.CompletedTask;
    }
}
