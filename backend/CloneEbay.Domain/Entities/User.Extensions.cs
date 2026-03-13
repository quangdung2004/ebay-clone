using System;
using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class User
{
    public bool emailVerified { get; set; }

    public DateTime? emailVerifiedAt { get; set; }

    public DateTime? passwordUpdatedAt { get; set; }

    public virtual ICollection<RefreshToken> RefreshToken { get; set; } = new List<RefreshToken>();

    public virtual ICollection<UserToken> UserToken { get; set; } = new List<UserToken>();
}