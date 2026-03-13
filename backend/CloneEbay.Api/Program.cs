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
// Services

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

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

    options.AddPolicy("auth", _ =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 25,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        var correlationId =
            context.HttpContext.Items["X-Correlation-Id"]?.ToString()
            ?? context.HttpContext.TraceIdentifier;

        context.HttpContext.Response.ContentType = "application/json";

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
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("fe");

app.UseCookiePolicy();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseStaticFiles();

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