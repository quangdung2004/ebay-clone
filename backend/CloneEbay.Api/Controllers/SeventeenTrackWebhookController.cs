using System.Text;
using System.Text.Json;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[ApiController]
[Route("api/webhooks/17track")]
public sealed class SeventeenTrackWebhookController : ControllerBase
{
    private readonly IShippingWebhookService _service;

    public SeventeenTrackWebhookController(IShippingWebhookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        Request.EnableBuffering();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signature = Request.Headers["signature"].FirstOrDefault()
                     ?? Request.Headers["Signature"].FirstOrDefault();

        var request = JsonSerializer.Deserialize<SeventeenTrackWebhookRequest>(
            rawBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new SeventeenTrackWebhookRequest();

        await _service.Handle17TrackWebhookAsync(rawBody, signature, request, ct);
        return Ok(new { success = true });
    }
}