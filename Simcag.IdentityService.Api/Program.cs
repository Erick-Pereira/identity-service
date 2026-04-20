using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Application.Services;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;
using Simcag.IdentityService.Infrastructure.Repositories;
using Simcag.Shared.Contracts;
using System.Text;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB__HOST")};" +
                      $"Port={Environment.GetEnvironmentVariable("DB__PORT")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB__NAME")};" +
                      $"Username={Environment.GetEnvironmentVariable("DB__USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DB__PASSWORD")}";

builder.Services.AddDbContext<IdentityServiceDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT__KEY") ?? "your-super-secure-jwt-key-here-at-least-256-bits-long";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT__ISSUER") ?? "Simcag.IdentityService";
var jwtAudience = Environment.GetEnvironmentVariable("JWT__AUDIENCE") ?? "Simcag.Clients";

builder.Services.AddSingleton(new JwtService(jwtKey, jwtIssuer, jwtAudience, 15, 7 * 24 * 60)); // 15 min access, 7 days refresh

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();