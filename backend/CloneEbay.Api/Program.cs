using System.Text;
using System.Threading.RateLimiting;
using CloneEbay.Api.Middleware;
using CloneEbay.Application;
using CloneEbay.Application.Auth;
using CloneEbay.Application.Hubs;
using CloneEbay.Application.Notifications;
using CloneEbay.Contracts;
using CloneEbay.Infrastructure;
using CloneEbay.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ======================
// Logging

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ======================
// Services

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + custom validation response
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var correlationId =
                context.HttpContext.Items["X-Correlation-Id"]?.ToString()
                ?? context.HttpContext.TraceIdentifier;

            var transactionId =
                context.HttpContext.Items["X-Transaction-Id"]?.ToString()
                ?? correlationId;

            var firstError = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "Invalid value."
                    : e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";

            var payload = ApiResponse<object>.Fail(
                message: firstError,
                code: "VALIDATION_ERROR",
                correlationId: correlationId
            );

            context.HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
            context.HttpContext.Response.Headers["X-Transaction-Id"] = transactionId;

            return new BadRequestObjectResult(payload);
        };
    });

// ======================
// CORS

var feOrigin = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("fe", policy =>
    {
        policy.WithOrigins(feOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ======================
// Cookie policy

var cookieSecure =
    bool.Parse(builder.Configuration["Auth:RefreshCookieSecure"] ?? "false");

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = cookieSecure ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
});

// ======================
// OpenAPI

builder.Services.AddOpenApi();

// ======================
// JWT Authentication

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing in configuration.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();

                var authHeader = context.HttpContext.Request.Headers.Authorization.ToString();

                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return;

                var token = authHeader["Bearer ".Length..].Trim();

                if (await blacklist.IsBlacklistedAsync(token))
                {
                    context.Fail("Token revoked");
                }
            }
        };
    });

// ======================
// Rate Limiter

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global limiter cho toàn project
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId =
            httpContext.User?.Identity?.IsAuthenticated == true
                ? httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst("id")?.Value
                    ?? httpContext.User.Identity?.Name
                : null;

        var ip =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown-ip";

        var key = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{ip}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    // Policy riêng cho auth endpoint
    options.AddPolicy("auth", httpContext =>
    {
        var ip =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown-ip";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (context, token) =>
    {
        var correlationId =
            context.HttpContext.Items["X-Correlation-Id"]?.ToString()
            ?? context.HttpContext.TraceIdentifier;

        var transactionId =
            context.HttpContext.Items["X-Transaction-Id"]?.ToString()
            ?? correlationId;

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter");

        logger.LogWarning(
            "Rate limit rejected | cid={cid} | tx={tx} | method={method} | path={path} | ip={ip}",
            correlationId,
            transactionId,
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress?.ToString());

        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        context.HttpContext.Response.Headers["X-Transaction-Id"] = transactionId;

        var payload = ApiResponse<object>.Fail(
            message: "Too many requests.",
            code: "RATE_LIMITED",
            correlationId: correlationId
        );

        await context.HttpContext.Response.WriteAsJsonAsync(
            payload,
            cancellationToken: token);
    };
});

builder.Services.AddAuthorization();

// ======================
// Build app

var app = builder.Build();

// ======================
// Pipeline

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTransactionLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("fe");
app.UseCookiePolicy();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseStaticFiles();

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AuctionHub>("/hubs/auction");
app.MapControllers();

// ======================
// Status code response

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.HasStarted)
        return;

    var correlationId =
        context.HttpContext.Items["X-Correlation-Id"]?.ToString()
        ?? context.HttpContext.TraceIdentifier;

    var transactionId =
        context.HttpContext.Items["X-Transaction-Id"]?.ToString()
        ?? correlationId;

    context.HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
    context.HttpContext.Response.Headers["X-Transaction-Id"] = transactionId;

    if (response.StatusCode is 404 or 405)
    {
        response.ContentType = "application/json";

        var (code, message) = response.StatusCode switch
        {
            404 => ("NOT_FOUND", "Route not found."),
            405 => ("METHOD_NOT_ALLOWED", "Method not allowed."),
            _ => ("ERROR", "Error.")
        };

        var payload = ApiResponse<object>.Fail(
            message: message,
            code: code,
            correlationId: correlationId
        );

        await response.WriteAsJsonAsync(payload);
    }
});

app.Run();