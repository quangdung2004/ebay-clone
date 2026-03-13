using AutoMapper;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Infrastructure.Products.Mapping;

public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductListItemDto>()
            .ForCtorParam("thumbnailUrl",
                opt => opt.MapFrom(src => ProductImageJson.Read(src).FirstOrDefault()))
            .ForCtorParam("price",
                opt => opt.MapFrom(src => src.price ?? 0))
            .ForCtorParam("categoryName",
                opt => opt.MapFrom(src => src.category != null ? src.category.name : null))
            .ForCtorParam("sellerName",
                opt => opt.MapFrom(src => src.seller != null ? src.seller.username : null))
            .ForCtorParam("status",
                opt => opt.MapFrom(src => src.status))
            .ForCtorParam("condition",
                opt => opt.MapFrom(src => src.condition))
            .ForCtorParam("availableQuantity",
                opt => opt.MapFrom(src => src.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault()))
            .ForCtorParam("inStock",
                opt => opt.MapFrom(src => src.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault() > 0))
            .ForCtorParam("bidCount",
                opt => opt.MapFrom(src => src.Bid.Count))
            .ForCtorParam("currentBid",
                opt => opt.MapFrom(src => src.Bid.Any()
                    ? src.Bid.Max(x => x.amount)
                    : null))
            .ForCtorParam("isEnded",
                opt => opt.MapFrom(src => src.isAuction == true
                    && src.auctionEndTime.HasValue
                    && src.auctionEndTime.Value <= DateTime.UtcNow))
            .ForCtorParam("auctionEndTime",
                opt => opt.MapFrom(src => src.auctionEndTime.HasValue
                    ? DateTime.SpecifyKind(src.auctionEndTime.Value, DateTimeKind.Utc)
                    : (DateTime?)null))
            .ForCtorParam("viewCount",
                opt => opt.MapFrom(src => src.viewCount ?? 0));

        CreateMap<Product, ProductDetailDto>()
            .ForCtorParam("images",
                opt => opt.MapFrom(src => ProductImageJson.Read(src)))
            .ForCtorParam("price",
                opt => opt.MapFrom(src => src.price ?? 0))
            .ForCtorParam("categoryName",
                opt => opt.MapFrom(src => src.category != null ? src.category.name : null))
            .ForCtorParam("sellerName",
                opt => opt.MapFrom(src => src.seller != null ? src.seller.username : null))
            .ForCtorParam("status",
                opt => opt.MapFrom(src => src.status))
            .ForCtorParam("condition",
                opt => opt.MapFrom(src => src.condition))
            .ForCtorParam("availableQuantity",
                opt => opt.MapFrom(src => src.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault()))
            .ForCtorParam("inStock",
                opt => opt.MapFrom(src => src.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault() > 0))
            .ForCtorParam("bidCount",
                opt => opt.MapFrom(src => src.Bid.Count))
            .ForCtorParam("currentBid",
                opt => opt.MapFrom(src => src.Bid.Any()
                    ? src.Bid.Max(x => x.amount)
                    : null))
            .ForCtorParam("isEnded",
                opt => opt.MapFrom(src => src.isAuction == true
                    && src.auctionEndTime.HasValue
                    && src.auctionEndTime.Value <= DateTime.UtcNow))
            .ForCtorParam("timeLeft",
                opt => opt.MapFrom(src =>
                    src.isAuction == true && src.auctionEndTime.HasValue
                        ? FormatTimeLeft(src.auctionEndTime.Value)
                        : null))
            .ForCtorParam("auctionEndTime",
                opt => opt.MapFrom(src => src.auctionEndTime.HasValue
                    ? DateTime.SpecifyKind(src.auctionEndTime.Value, DateTimeKind.Utc)
                    : (DateTime?)null))
            .ForCtorParam("viewCount",
                opt => opt.MapFrom(src => src.viewCount ?? 0));
    }

    private static string? FormatTimeLeft(DateTime endTimeUtc)
    {
        var diff = endTimeUtc - DateTime.UtcNow;
        if (diff <= TimeSpan.Zero) return "Ended";

        if (diff.TotalDays >= 1)
            return $"{(int)diff.TotalDays}d {diff.Hours}h";

        if (diff.TotalHours >= 1)
            return $"{(int)diff.TotalHours}h {diff.Minutes}m";

        return $"{diff.Minutes}m";
    }
}