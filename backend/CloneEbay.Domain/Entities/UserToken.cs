using System;

namespace CloneEbay.Domain.Entities;

public class UserToken
{
    public int id { get; set; }

    public int userId { get; set; }

    // "VERIFY_EMAIL" | "RESET_PASSWORD"
    public string type { get; set; } = null!;

    public string tokenHash { get; set; } = null!;

    public DateTime createdAt { get; set; }

    public DateTime expiresAt { get; set; }

    public DateTime? usedAt { get; set; }

    public virtual User user { get; set; } = null!;
}