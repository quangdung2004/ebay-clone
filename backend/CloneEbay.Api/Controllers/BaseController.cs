using System.Security.Claims;
using CloneEbay.Api.Dtos;
using CloneEbay.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string? CorrelationId =>
        HttpContext.Items["X-Correlation-Id"]?.ToString();

    protected ApiResponse<T> Success<T>(
        T data,
        string message = "Success",
        string code = "SUCCESS")
    {
        return ApiResponse<T>.Ok(
            data,
            message,
            code,
            CorrelationId
        );
    }

    protected ApiResponse<object> Success(
        string message = "Success",
        string code = "SUCCESS")
    {
        return ApiResponse<object>.Ok(
            null,
            message,
            code,
            CorrelationId
        );
    }

    protected int CurrentUserId
    {
        get
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
                throw new AuthException("Invalid token", unauthorized: true);

            return userId;
        }
    }
}
