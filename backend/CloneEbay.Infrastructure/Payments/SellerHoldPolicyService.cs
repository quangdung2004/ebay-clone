using CloneEbay.Application.Payments;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Infrastructure.Payments;

public sealed class SellerHoldPolicyService : ISellerHoldPolicyService
{
    public DateTime CalculateAvailableAt(SellerTrustProfile trustProfile, DateTime heldAtUtc)
    {
        var level = trustProfile.level;

        return level switch
        {
            >= 3 => heldAtUtc.AddDays(1),
            2 => heldAtUtc.AddDays(3),
            _ => heldAtUtc.AddMinutes(5)
        };
    }
}