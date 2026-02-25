using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Api.Models;

public partial class CloneEbayDbContext : DbContext
{
    public CloneEbayDbContext()
    {
    }

    public CloneEbayDbContext(DbContextOptions<CloneEbayDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Address { get; set; }

    public virtual DbSet<Bid> Bid { get; set; }

    public virtual DbSet<Category> Category { get; set; }

    public virtual DbSet<Coupon> Coupon { get; set; }

    public virtual DbSet<Dispute> Dispute { get; set; }

    public virtual DbSet<Feedback> Feedback { get; set; }

    public virtual DbSet<Inventory> Inventory { get; set; }

    public virtual DbSet<Message> Message { get; set; }

    public virtual DbSet<OrderItem> OrderItem { get; set; }

    public virtual DbSet<OrderTable> OrderTable { get; set; }

    public virtual DbSet<Payment> Payment { get; set; }

    public virtual DbSet<Product> Product { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequest { get; set; }

    public virtual DbSet<Review> Review { get; set; }

    public virtual DbSet<ShippingInfo> ShippingInfo { get; set; }

    public virtual DbSet<Store> Store { get; set; }

    public virtual DbSet<User> User { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=CloneEbayDB;User Id=sa;Password=123;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Address__3213E83F5B4CAB29");

            entity.Property(e => e.city).HasMaxLength(50);
            entity.Property(e => e.country).HasMaxLength(50);
            entity.Property(e => e.fullName).HasMaxLength(100);
            entity.Property(e => e.phone).HasMaxLength(20);
            entity.Property(e => e.state).HasMaxLength(50);
            entity.Property(e => e.street).HasMaxLength(100);

            entity.HasOne(d => d.user).WithMany(p => p.Address)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__Address__userId__3A81B327");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Bid__3213E83F42F77A36");

            entity.Property(e => e.amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.bidTime).HasColumnType("datetime");

            entity.HasOne(d => d.bidder).WithMany(p => p.Bid)
                .HasForeignKey(d => d.bidderId)
                .HasConstraintName("FK__Bid__bidderId__5629CD9C");

            entity.HasOne(d => d.product).WithMany(p => p.Bid)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Bid__productId__5535A963");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Category__3213E83F2FC87602");

            entity.Property(e => e.name).HasMaxLength(100);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Coupon__3213E83FE752B844");

            entity.Property(e => e.code).HasMaxLength(50);
            entity.Property(e => e.discountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.endDate).HasColumnType("datetime");
            entity.Property(e => e.startDate).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Coupon)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Coupon__productI__60A75C0F");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Dispute__3213E83F5F1E1150");

            entity.Property(e => e.status).HasMaxLength(20);

            entity.HasOne(d => d.order).WithMany(p => p.Dispute)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__Dispute__orderId__693CA210");

            entity.HasOne(d => d.raisedByNavigation).WithMany(p => p.Dispute)
                .HasForeignKey(d => d.raisedBy)
                .HasConstraintName("FK__Dispute__raisedB__6A30C649");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Feedback__3213E83F5FDB847A");

            entity.Property(e => e.averageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.positiveRate).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.seller).WithMany(p => p.Feedback)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Feedback__seller__66603565");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Inventor__3213E83F041D60B2");

            entity.Property(e => e.lastUpdated).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Inventory)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Inventory__produ__6383C8BA");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Message__3213E83FBFB17914");

            entity.Property(e => e.timestamp).HasColumnType("datetime");

            entity.HasOne(d => d.receiver).WithMany(p => p.Messagereceiver)
                .HasForeignKey(d => d.receiverId)
                .HasConstraintName("FK__Message__receive__5DCAEF64");

            entity.HasOne(d => d.sender).WithMany(p => p.Messagesender)
                .HasForeignKey(d => d.senderId)
                .HasConstraintName("FK__Message__senderI__5CD6CB2B");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OrderIte__3213E83FF2999F94");

            entity.Property(e => e.unitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.order).WithMany(p => p.OrderItem)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__OrderItem__order__46E78A0C");

            entity.HasOne(d => d.product).WithMany(p => p.OrderItem)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__OrderItem__produ__47DBAE45");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__OrderTab__3213E83F6A6DBF44");

            entity.Property(e => e.orderDate).HasColumnType("datetime");
            entity.Property(e => e.status).HasMaxLength(20);
            entity.Property(e => e.totalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.address).WithMany(p => p.OrderTable)
                .HasForeignKey(d => d.addressId)
                .HasConstraintName("FK__OrderTabl__addre__440B1D61");

            entity.HasOne(d => d.buyer).WithMany(p => p.OrderTable)
                .HasForeignKey(d => d.buyerId)
                .HasConstraintName("FK__OrderTabl__buyer__4316F928");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Payment__3213E83F5CBFD446");

            entity.Property(e => e.amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.method).HasMaxLength(50);
            entity.Property(e => e.paidAt).HasColumnType("datetime");
            entity.Property(e => e.status).HasMaxLength(20);

            entity.HasOne(d => d.order).WithMany(p => p.Payment)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__Payment__orderId__4AB81AF0");

            entity.HasOne(d => d.user).WithMany(p => p.Payment)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__Payment__userId__4BAC3F29");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Product__3213E83F861F9FD4");

            entity.Property(e => e.auctionEndTime).HasColumnType("datetime");
            entity.Property(e => e.price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.title).HasMaxLength(255);

            entity.HasOne(d => d.category).WithMany(p => p.Product)
                .HasForeignKey(d => d.categoryId)
                .HasConstraintName("FK__Product__categor__3F466844");

            entity.HasOne(d => d.seller).WithMany(p => p.Product)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Product__sellerI__403A8C7D");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ReturnRe__3213E83F316F72F4");

            entity.Property(e => e.createdAt).HasColumnType("datetime");
            entity.Property(e => e.status).HasMaxLength(20);

            entity.HasOne(d => d.order).WithMany(p => p.ReturnRequest)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__ReturnReq__order__5165187F");

            entity.HasOne(d => d.user).WithMany(p => p.ReturnRequest)
                .HasForeignKey(d => d.userId)
                .HasConstraintName("FK__ReturnReq__userI__52593CB8");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Review__3213E83FB37AACD8");

            entity.Property(e => e.createdAt).HasColumnType("datetime");

            entity.HasOne(d => d.product).WithMany(p => p.Review)
                .HasForeignKey(d => d.productId)
                .HasConstraintName("FK__Review__productI__59063A47");

            entity.HasOne(d => d.reviewer).WithMany(p => p.Review)
                .HasForeignKey(d => d.reviewerId)
                .HasConstraintName("FK__Review__reviewer__59FA5E80");
        });

        modelBuilder.Entity<ShippingInfo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Shipping__3213E83F4C7AE977");

            entity.Property(e => e.carrier).HasMaxLength(100);
            entity.Property(e => e.estimatedArrival).HasColumnType("datetime");
            entity.Property(e => e.status).HasMaxLength(50);
            entity.Property(e => e.trackingNumber).HasMaxLength(100);

            entity.HasOne(d => d.order).WithMany(p => p.ShippingInfo)
                .HasForeignKey(d => d.orderId)
                .HasConstraintName("FK__ShippingI__order__4E88ABD4");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Store__3213E83F50818C21");

            entity.Property(e => e.storeName).HasMaxLength(100);

            entity.HasOne(d => d.seller).WithMany(p => p.Store)
                .HasForeignKey(d => d.sellerId)
                .HasConstraintName("FK__Store__sellerId__6D0D32F4");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__User__3213E83F73F12474");

            entity.HasIndex(e => e.email, "UQ__User__AB6E6164E3522631").IsUnique();

            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.password).HasMaxLength(255);
            entity.Property(e => e.role).HasMaxLength(20);
            entity.Property(e => e.username).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
