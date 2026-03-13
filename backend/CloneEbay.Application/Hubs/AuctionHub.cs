using Microsoft.AspNetCore.SignalR;

namespace CloneEbay.Application.Hubs;

public sealed class AuctionHub : Hub
{
    public async Task JoinProductRoom(int productId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-product-{productId}");
    }

    public async Task LeaveProductRoom(int productId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-product-{productId}");
    }
}