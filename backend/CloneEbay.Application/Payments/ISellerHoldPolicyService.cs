using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Payments;

public interface ISellerHoldPolicyService
{
    DateTime CalculateAvailableAt(SellerTrustProfile trustProfile, DateTime heldAtUtc);
}