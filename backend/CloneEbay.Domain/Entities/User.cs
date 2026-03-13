using System;
using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class User
{
    public int id { get; set; }

    public string? username { get; set; }

    public string? email { get; set; }

    public string? password { get; set; }

    public string? role { get; set; }

    public string? avatarURL { get; set; }

    public virtual ICollection<Address> Address { get; set; } = new List<Address>();

    public virtual ICollection<Bid> Bid { get; set; } = new List<Bid>();

    public virtual ICollection<Dispute> Dispute { get; set; } = new List<Dispute>();

    public virtual ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();

    public virtual ICollection<Message> Messagereceiver { get; set; } = new List<Message>();

    public virtual ICollection<Message> Messagesender { get; set; } = new List<Message>();

    public virtual ICollection<OrderTable> OrderTable { get; set; } = new List<OrderTable>();

    public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();

    public virtual ICollection<Product> Product { get; set; } = new List<Product>();

    public virtual ICollection<ReturnRequest> ReturnRequest { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual ICollection<Store> Store { get; set; } = new List<Store>();
}
