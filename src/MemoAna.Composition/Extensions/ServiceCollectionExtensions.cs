using FluentValidation;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Authorization;
using MemoAna.Application.Common.Contracts;
using MemoAna.Application.Common.Pipeline.Logging;
using MemoAna.Application.Common.Pipeline.Validation;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Health.Abstractions;
using MemoAna.Application.Identity.Abstractions;
using MemoAna.Application.Identity.Handlers;
using MemoAna.Application.Identity.Validators;
using MemoAna.Application.Seed.Abstractions;
using MemoAna.Composition.Authorization;
using MemoAna.Infrastructure.Common.Repository;
using MemoAna.Infrastructure.Common.Services;
using MemoAna.Infrastructure.Common.UnitOfWork;
using MemoAna.Infrastructure.Game.Options;
using MemoAna.Infrastructure.Game.Mqtt;
using MemoAna.Infrastructure.Game.Services;
using MemoAna.Infrastructure.Identity.Models;
using MemoAna.Infrastructure.Identity.Options;
using MemoAna.Infrastructure.Identity.Services;
using MemoAna.Infrastructure.Persistence.Contexts;
using MemoAna.Infrastructure.Persistence.Middlewares;
using MemoAna.Infrastructure.Persistence.Options;
using MemoAna.Infrastructure.Persistence.Seed.Sql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;

namespace MemoAna.Composition.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {

        public IServiceCollection ConfigureAuth(IConfiguration configuration)
        {
            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
            _ = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "BearerSelector";
                options.DefaultChallengeScheme = "BearerSelector";
            })
                .AddPolicyScheme("BearerSelector", "Local or Google JWT", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        string? authorization = context.Request.Headers.Authorization;
                        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            string token = authorization["Bearer ".Length..].Trim();
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

                            if (handler.CanReadToken(token))
                            {
                                var jwtToken = handler.ReadJwtToken(token);
                                if (jwtToken.Issuer.Contains("accounts.google.com"))
                                {
                                    return "GoogleJwt";
                                }
                            }
                        }

                        return "LocalJwt";
                    };
                })
                .AddJwtBearer("LocalJwt", options =>
                {
                    JwtOptions jwt = configuration
                        .GetSection(JwtOptions.SectionName)
                        .Get<JwtOptions>()
                        ?? throw new InvalidOperationException(
                            "JWT configuration is missing.");

                    if (System.Text.Encoding.UTF8.GetByteCount(jwt.Key) < 32)
                    {
                        throw new InvalidOperationException(
                            "Jwt:Key must contain 256 bits.");
                    }

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    System.Text.Encoding.UTF8.GetBytes(
                                        jwt.Key)),
                            ValidateIssuer = true,
                            ValidIssuer = jwt.Issuer,
                            ValidateAudience = true,
                            ValidAudience = jwt.Audience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(30)
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearerDebug");

                            logger.LogError(context.Exception, "Falha na Autenticação JWT: {Message}", context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var principal = context.Principal;
                            if (principal == null)
                            {
                                context.Fail("Invalid token principal.");
                                return Task.CompletedTask;
                            }

                            string? tokenType = principal.FindFirst("typ")?.Value
                                            ?? principal.FindFirst("http://schemas.openxmlformats.org/claims/type")?.Value;

                            if (string.IsNullOrEmpty(tokenType) && context.SecurityToken != null)
                            {
                                if (context.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt)
                                {
                                    tokenType = jwt.Typ;
                                }
                                else if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtLegacy)
                                {
                                    tokenType = jwtLegacy.Header.Typ;
                                }
                            }

                            if (!string.IsNullOrEmpty(tokenType) &&
                                !string.Equals(tokenType, "access", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(tokenType, "JWT", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Fail($"The token is not an access token. Type found: '{tokenType}'.");
                                return Task.CompletedTask;
                            }

                            string? tokenId = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value
                                            ?? principal.FindFirst("jti")?.Value;

                            if (!string.IsNullOrEmpty(tokenId))
                            {
                                var revokedStore = context.HttpContext.RequestServices.GetRequiredService<IRevokedTokenStore>();
                                if (revokedStore.IsRevoked(tokenId))
                                {
                                    context.Fail("The access token has been revoked.");
                                    return Task.CompletedTask;
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                })
                .AddJwtBearer("GoogleJwt", options =>
                {
                    GooglePlayGamesOptions gpg = configuration
                        .GetSection(GooglePlayGamesOptions.SectionName)
                        .Get<GooglePlayGamesOptions>()
                        ?? throw new InvalidOperationException(
                            "GooglePlayGames configuration is missing.");


                    options.Audience = gpg.Audience;
                    options.Authority = gpg.Authority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = gpg.ValidAudience,
                        ValidateAudience = true,
                        ValidIssuers = ["accounts.google.com", "https://accounts.google.com"],
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });

            _ = services.AddAuthorizationBuilder()
                .AddPolicy(IdentityPolicies.Administrator, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(IdentityClaimTypes.Permission, "system.admin"))
                .AddPolicy(IdentityPolicies.User, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(IdentityClaimTypes.Permission, "system.user"))
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());
            return services;
        }


        public IServiceCollection ConfigureApplicationServices()
        {
            _ = services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
            _ = services.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
                options.Assemblies = [typeof(IdentityHandlers).Assembly];
                options.PipelineBehaviors =
                [
                    typeof(LoggingMiddleware<,>),
                    typeof(ValidationMiddleware<,>),
                    typeof(TransactionMiddleware<,>)
                ];
            });
            return services;
        }

        public IServiceCollection ConfigureDatabase(
            IConfiguration configuration)
        {
            ConnectionStringsOptions connectionString = configuration
                .GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>() ?? 
                    throw new InvalidOperationException(
                        "ConnectionStrings configuration is missing.");
            _ = services.AddDbContext<SQLiteDbContext>(
                options =>
                {
                    _ = options.UseSqlite(connectionString.SQLite, sql => sql.CommandTimeout(90));
                });
            _ = services.AddSingleton<LiteDB.LiteDatabase>(options =>
            {
                return new LiteDB.LiteDatabase(connectionString.LiteDB);
            });
            _ = services.AddSingleton<LiteDbContext>(); 
            _ = services.AddScoped<IUnitOfWork, UnitOfWork>();
            _ = services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            _ = services.AddScoped<ICardsRepository, CardsRepository>();
            return services;
        }
        
        public IServiceCollection ConfigureIdentity()
        {
            _ = services.AddIdentityCore<User>(
                options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);
                }).AddRoles<Role>()
                .AddEntityFrameworkStores<SQLiteDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            _ = services.AddScoped<IIdentityService, IdentityService>();
            _ = services.AddScoped<IAuthenticatedUserClassifier, AuthenticatedUserClassifier>();
            _ = services.AddScoped<IGooglePlayGamesAuthenticationService, GooglePlayGamesAuthenticationService>();
            _ = services.AddSingleton<IRevokedTokenStore, RevokedTokenStore>();
            _ = services.AddScoped<IJwtTokenService, JwtTokenService>();
            _ = services.AddScoped<IIdentityEmailSender, LoggingIdentityEmailSender>();
            return services;
        }

        public IServiceCollection ConfigureInfrastructureServices(IConfiguration configuration)
        {
            _ = services.AddDataProtection()
                .SetApplicationName("MemoAna");

            _ = services.AddScoped<ISqlSeedService, SqlSeedService>();
            _ = services.AddScoped<IThemeService, ThemeService>();
            _ = services.AddScoped<IGameService, GameService>();
            _ = services.AddScoped<IHealthService, HealthService>();

            MqttOptions mqttOptions = configuration
                .GetSection(MqttOptions.SectionName)
                .Get<MqttOptions>()
                ?? new MqttOptions();

            _ = services.AddSingleton(mqttOptions);
            _ = services.AddHostedMqttServer(options => options
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(mqttOptions.Port)
                .WithPersistentSessions());

            _ = services.AddSingleton<GameMqttHub>();
            _ = services.AddSingleton<IGamePublisher>(sp => sp.GetRequiredService<GameMqttHub>());

            return services;
        }

    }    
}
