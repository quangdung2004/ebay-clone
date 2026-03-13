using System;

namespace CloneEbay.Domain.Entities;

public class RefreshToken
{
    public int id { get; set; }

    public int userId { get; set; }

    public string tokenHash { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime expiresAt { get; set; }

    public DateTime? revokedAt { get; set; }

    public string? replacedByTokenHash { get; set; }

    public string? createdByIp { get; set; }

    public string? revokedByIp { get; set; }

    public string? userAgent { get; set; }

    public virtual User user { get; set; } = null!;
}