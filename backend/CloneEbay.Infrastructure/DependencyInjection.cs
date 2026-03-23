using CloneEbay.Application.Auth;
using CloneEbay.Application.Auctions;
using CloneEbay.Application.Bids;
using CloneEbay.Application.Categories;
using CloneEbay.Application.Notifications;
using CloneEbay.Application.Orders;
using CloneEbay.Application.Payments;
using CloneEbay.Application.Products;
using CloneEbay.Application.Shipping;
using CloneEbay.Application.Stores;
using CloneEbay.Infrastructure.Auth;
using CloneEbay.Infrastructure.Auctions;
using CloneEbay.Infrastructure.Bids;
using CloneEbay.Infrastructure.Categories;
using CloneEbay.Infrastructure.Messaging;
using CloneEbay.Infrastructure.Orders;
using CloneEbay.Infrastructure.Payments;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
using CloneEbay.Infrastructure.Products.Mapping;
using CloneEbay.Infrastructure.Shipping;
using CloneEbay.Infrastructure.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CloneEbay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<CloneEbayDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MyCnn")));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration["Redis:Connection"]!));

        // AutoMapper
        services.AddAutoMapper(typeof(ProductProfile).Assembly);

        // SignalR
        services.AddSignalR();

        // JWT options + service
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<JwtService>();

        // Auth options
        services.Configure<AuthOptions>(options =>
        {
            options.AccessTokenMinutes =
                int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "30");

            options.RefreshTokenDays =
                int.Parse(configuration["Jwt:RefreshTokenDays"] ?? "7");

            options.RefreshTokenDaysRememberMe =
                int.Parse(configuration["Jwt:RefreshTokenDaysRememberMe"] ?? "30");

            options.VerifyEmailMinutes =
                int.Parse(configuration["TokenLifetimes:VerifyEmailMinutes"] ?? "1440");

            options.ResetPasswordMinutes =
                int.Parse(configuration["TokenLifetimes:ResetPasswordMinutes"] ?? "60");

            options.FrontendBaseUrl =
                configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";

            options.RefreshCookieSecure =
                bool.Parse(configuration["Auth:RefreshCookieSecure"] ?? "false");

            options.RefreshCookiePath =
                configuration["Auth:RefreshCookiePath"] ?? "/api/auth";
        });

        // Auth helpers
        services.AddScoped<IAuthTokenFactory, AuthTokenFactory>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();
        services.AddScoped<IAuthEmailService, AuthEmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();

        // Domain/application services
        services.AddScoped<IAuctionService, AuctionService>();
        services.AddScoped<IBidService, BidService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IOrderService, OrderService>();

        // PayPal
        services.Configure<PayPalOptions>(configuration.GetSection("PayPal"));
        services.AddHttpClient<IPaymentService, PaymentService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Seller wallet / settlement
        services.AddScoped<ISellerHoldPolicyService, SellerHoldPolicyService>();
        services.AddScoped<ISellerWalletService, SellerWalletService>();
        services.AddHostedService<SettlementReleaseBackgroundService>();

        // 17TRACK
        services.Configure<SeventeenTrackOptions>(configuration.GetSection("SeventeenTrack"));
        services.AddHttpClient<ISeventeenTrackClient, SeventeenTrackClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Shipping & Orders
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IShippingWebhookService, ShippingWebhookService>();
        services.AddScoped<CloneEbay.Application.Orders.IOrderEmailService, CloneEbay.Infrastructure.Orders.OrderEmailService>();

        
        // Geocoding
        services.AddHttpClient<CloneEbay.Application.Common.Interfaces.IGeocodingService, NominatimGeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "CloneEbayTracking/1.0");
        });

        // Realtime + messaging
        services.AddSingleton<AuctionRealtimeNotifier>();
        services.AddSingleton<IAuctionEventPublisher, RabbitMqAuctionEventPublisher>();

        // Background services
        services.AddHostedService<AuctionClosingBackgroundService>();
        services.AddHostedService<AuctionWinnerEmailConsumer>();
        services.AddHostedService<CloneEbay.Infrastructure.Payments.SettlementReleaseBackgroundService>();
        services.AddHostedService<CloneEbay.Infrastructure.Orders.OrderAutoCancelBackgroundService>();


        return services;
    }
}