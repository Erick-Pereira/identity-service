using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using MediatR;
using Simcag.IdentityService.Api.Workers;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Application.Services;
using Simcag.IdentityService.Application.UseCases.Register;
using Simcag.IdentityService.Domain.ValueObjects;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;
using Simcag.IdentityService.Infrastructure.Repositories;
using Simcag.IdentityService.Infrastructure.Security;
using System.Text;
using Simcag.Shared.ErrorHandling;
using Simcag.Shared.Hosting;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;
using Simcag.Shared.Messaging.Rpc;
using Simcag.Shared.Messaging.Rpc.Contracts;
using Simcag.Shared.Telemetry;

DotNetEnv.Env.NoClobber().Load();
ContainerListenConfiguration.NormalizeAspNetCoreListenUrlsInContainer();
var builder = WebApplication.CreateBuilder(args);
ContainerListenConfiguration.ApplyDockerListenUrls(builder);
builder.AddSimcagDistributedTelemetry("Simcag.IdentityService");
var isTesting = builder.Environment.IsEnvironment("Testing");

static string? GetEnv(params string[] keys)
{
    foreach (var key in keys)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }
    return null;
}

// Configuração via .env / ambiente
string? connectionString = null;
if (!isTesting)
{
    connectionString = GetEnv("ConnectionStrings__DefaultConnection", "CONNECTIONSTRINGS__DEFAULTCONNECTION")
        ?? throw new InvalidOperationException("Defina ConnectionStrings__DefaultConnection no .env (PostgreSQL).");
}

var jwtSecret = GetEnv("JWT__SECRET", "JWT_SECRET", "JWT_SECRETKEY");
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!builder.Environment.IsDevelopment() && !isTesting)
        throw new InvalidOperationException("Defina JWT__SECRET no .env.");
    jwtSecret = DevJwtSecretFallback.Value;
    if (builder.Environment.IsDevelopment())
        Console.WriteLine(
            "[Simcag.Identity] JWT__SECRET ausente: a usar segredo fixo só para Development. Defina JWT__SECRET em produção.");
}

var jwtIssuer = GetEnv("JWT__ISSUER", "Jwt__Issuer") ?? "Simcag.IdentityService";
var jwtAudience = GetEnv("JWT__AUDIENCE", "Jwt__Audience") ?? "Simcag.Clients";
var accessTokenMinutes = GetEnv("JWT__ACCESSTOKENEXPIRATIONMINUTES") ?? "15";
var refreshTokenDays = GetEnv("JWT__REFRESHTOKENEXPIRATIONDAYS") ?? "7";

builder.Configuration.AddInMemoryCollection(
    new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = jwtSecret,
        ["Jwt:Issuer"] = jwtIssuer,
        ["Jwt:Audience"] = jwtAudience,
        ["Jwt:AccessTokenExpirationMinutes"] = accessTokenMinutes,
        ["Jwt:RefreshTokenExpirationDays"] = refreshTokenDays
    });

// ===== DATABASE =====
if (isTesting)
{
    builder.Services.AddDbContext<IdentityServiceDbContext>(options =>
        options.UseInMemoryDatabase("identity_testing"));
}
else
{
    builder.Services.AddDbContext<IdentityServiceDbContext>(options =>
        options.UseNpgsql(connectionString!,
            npgsqlOptions => npgsqlOptions.CommandTimeout(30)));
}

// ===== MEDIATR =====
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

// ===== APPLICATION SERVICES =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICondominiumRepository, CondominiumRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

// ===== CONTROLLERS =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Econdomiza - Identity", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        In          = Microsoft.OpenApi.ParameterLocation.Header,
        Type        = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Cole apenas o JWT (sem 'Bearer ')."
    });
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ===== AUTHENTICATION - JWT =====
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        // GET /api/auth/validate e GET /api/condominios/lookup: não validar Bearer aqui (token expirado no browser
        // quebra pedidos anónimos). validate lê o JWT no controller; lookup é público.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Não validar JWT nestes pedidos: [AllowAnonymous] + Bearer expirado/ inválido no browser
                // ainda dispara o handler e pode impedir a action (ex.: GET /api/condominios/lookup no registo).
                if (context.Request.Path.StartsWithSegments("/api/auth/validate", StringComparison.OrdinalIgnoreCase)
                    || context.Request.Path.StartsWithSegments("/api/condominios/lookup", StringComparison.OrdinalIgnoreCase))
                    context.Token = null;
                return Task.CompletedTask;
            }
        };
    });

// ===== AUTHORIZATION =====
builder.Services.AddAuthorization();

// ===== BACKGROUND WORKERS =====
if (!isTesting)
{
    builder.Services.AddHostedService<OverdueConformityWorker>();

    var rabbitMqOptions = new RabbitMqOptions
    {
        Host = GetEnv("RABBITMQ__HOST", "RABBITMQ_HOST") ?? "localhost",
        Port = int.Parse(GetEnv("RABBITMQ__PORT", "RABBITMQ_PORT") ?? "5672"),
        UserName = GetEnv("RABBITMQ__USERNAME", "RABBITMQ_USERNAME") ?? "guest",
        Password = GetEnv("RABBITMQ__PASSWORD", "RABBITMQ_PASSWORD") ?? "guest",
        VirtualHost = GetEnv("RABBITMQ__VIRTUALHOST", "RABBITMQ_VIRTUALHOST") ?? "/"
    };
    rabbitMqOptions.ApplyMessageSigningFromEnvironment();
    builder.Services.AddRabbitMqMessaging(rabbitMqOptions);

    builder.Services.AddRabbitMqRpcHandler<GetNotificationRecipientsRpcRequest, GetNotificationRecipientsRpcResponse>(
        RpcQueues.IdentityGetNotificationRecipients,
        async (sp, request, ct) =>
        {
            var users = sp.GetRequiredService<IUserRepository>();
            var roles = new[] { Role.AdminValue, Role.SindicoValue, Role.ConselhoValue };
            var rows = await users.GetActiveByTenantAndRolesAsync(request.TenantId, roles, ct);
            return new GetNotificationRecipientsRpcResponse
            {
                UserIds = rows.Select(u => u.Id).Where(id => id != Guid.Empty).Distinct().ToList()
            };
        });
}

// ===== HEALTH CHECKS =====
var healthChecksBuilder = builder.Services.AddHealthChecks().AddSimcagLiveSelfCheck();
if (isTesting)
    healthChecksBuilder.AddCheck("database", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: [SimcagHealthCheckExtensions.ReadyTag]);
else
    healthChecksBuilder.AddNpgSql(connectionString!, name: "PostgreSQL", tags: [SimcagHealthCheckExtensions.ReadyTag]);

builder.Services.AddSimcagProblemDetails();

// ===== LOGGING =====
builder.Services.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
    configure.AddDebug();
});

var app = builder.Build();

app.UseSimcagExceptionHandler();
app.UseSimcagHttpCorrelationActivityTags();

// ===== DATABASE MIGRATIONS (PostgreSQL; não em ambiente de testes com InMemory) =====
if (!isTesting)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
    await db.Database.MigrateAsync();
}

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSimcagHealthChecks();

app.UseSimcagTelemetryEndpoints();

await app.RunAsync();

file static class DevJwtSecretFallback
{
    // Manter o literal idêntico em gateway-service/Simcag.Gateway.Api/Program.cs (só Development).
    public const string Value = "Simcag.Dev.Jwt.NotForProduction.AlignWithIdentityService.01!";
}

public partial class Program
{
}
