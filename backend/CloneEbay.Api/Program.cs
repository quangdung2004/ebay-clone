using CloneEbay.Api.Models;
using CloneEbay.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using CloneEbay.Api.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using CloneEbay.Api.Dtos;


var builder = WebApplication.CreateBuilder(args);

// ======================
// Add services

// JWT options + services
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:Connection"]!));

builder.Services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();

// Controllers
builder.Services.AddControllers();

// EF Core
builder.Services.AddDbContext<CloneEbayDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyCnn")
    ));

// OpenAPI (.NET 9)
builder.Services.AddOpenApi();

// ======================
// JWT Authentication + Blacklist check (THÊM Ở ĐÂY)

var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var blacklist = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();

                var auth = ctx.HttpContext.Request.Headers.Authorization.ToString();
                if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return;

                var token = auth["Bearer ".Length..].Trim();

                if (await blacklist.IsBlacklistedAsync(token))
                {
                    ctx.Fail("Token revoked");
                }
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // limit chung cho auth (theo IP) - demo
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            //partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            partitionKey: "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 25,               // 20 req
                Window = TimeSpan.FromMinutes(1),// / 1 phút
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        var cid = context.HttpContext.Items["X-Correlation-Id"]?.ToString();
        context.HttpContext.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Fail(
            message: "Too many requests",
            code: "RATE_LIMITED",
            correlationId: cid
        );

        await context.HttpContext.Response.WriteAsJsonAsync(payload, cancellationToken: token);
    };
});

builder.Services.AddAuthorization();

// ======================

var app = builder.Build();

// ======================
// Configure pipeline

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async ctx =>
{
    var res = ctx.HttpContext.Response;
    if (res.HasStarted) return;

    var cid = ctx.HttpContext.Items["X-Correlation-Id"]?.ToString();

    if (res.StatusCode is 404 or 405)
    {
        res.ContentType = "application/json";

        var (code, message) = res.StatusCode switch
        {
            404 => ("NOT_FOUND", "Route not found"),
            405 => ("METHOD_NOT_ALLOWED", "Method not allowed"),
            _ => ("ERROR", "Error")
        };

        var payload = ApiResponse<object>.Fail(message, code, cid);
        await res.WriteAsJsonAsync(payload);
    }
});

app.MapControllers();

app.Run();
