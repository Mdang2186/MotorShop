// File: Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System.Globalization;
using System.Text;
using MotorShop.Models.Entities; 

namespace MotorShop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ===== DbSets =====
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
        public DbSet<ChatThread> ChatThreads { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ProductTag> ProductTags => Set<ProductTag>();

        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Bank> Banks => Set<Bank>();      // ⚠️ Đảm bảo Models/Bank.cs có namespace MotorShop.Models
        public DbSet<Shipper> Shippers => Set<Shipper>();
        public DbSet<AiConversation> AiConversations { get; set; } = null!;
        public DbSet<AiMessage> AiMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ===== Money precision =====
            b.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            b.Entity<Product>().Property(p => p.OriginalPrice).HasPrecision(18, 2);
            b.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Entity<Order>().Property(o => o.ShippingFee).HasPrecision(18, 2);
            b.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
            b.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);

            // ===== Product =====
            b.Entity<Product>(e =>
            {
                e.HasIndex(x => new { x.Name, x.BrandId, x.CategoryId });
                e.Property(x => x.Slug).HasMaxLength(180);
                e.HasIndex(x => x.Slug).IsUnique(false);

                e.Property(x => x.IsPublished).HasDefaultValue(true);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Concurrency token
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            // ===== Brand / Category =====
            b.Entity<Brand>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Slug).HasMaxLength(120);
                e.HasIndex(x => x.Slug).IsUnique(false);
            });

            b.Entity<Category>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Slug).HasMaxLength(120);
                e.HasIndex(x => x.Slug).IsUnique(false);
            });

            // ===== Tag & ProductTag =====
            b.Entity<Tag>(e =>
            {
                e.HasIndex(t => t.Name).IsUnique();
                e.Property(t => t.Slug).HasMaxLength(120);
                e.HasIndex(t => t.Slug).IsUnique(false);
            });

            b.Entity<ProductTag>(e =>
            {
                e.HasKey(pt => new { pt.ProductId, pt.TagId });
                e.HasOne(pt => pt.Product).WithMany(p => p.ProductTags).HasForeignKey(pt => pt.ProductId);
                e.HasOne(pt => pt.Tag).WithMany(t => t.ProductTags).HasForeignKey(pt => pt.TagId);
            });

            // ===== Order / OrderItem =====
            b.Entity<Order>(e =>
            {
                e.Property(x => x.OrderDate).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(x => new { x.UserId, x.OrderDate });
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.PaymentStatus);
            });

            b.Entity<OrderItem>(e =>
            {
                e.HasOne(i => i.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Product)
                    .WithMany()
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== ProductImage =====
            b.Entity<ProductImage>(e =>
            {
                e.HasOne(pi => pi.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Mỗi Product không trùng SortOrder
                e.HasIndex(pi => new { pi.ProductId, pi.SortOrder }).IsUnique();

                e.Property(pi => pi.ImageUrl).HasMaxLength(500).IsRequired();

                // Đồng bộ với model: cho phép null, dài 255
                e.Property(pi => pi.Caption).HasMaxLength(255).IsRequired(false);
            });

            // ===== ProductSpecification =====
            b.Entity<ProductSpecification>(e =>
            {
                e.HasOne(ps => ps.Product)
                    .WithMany(p => p.Specifications)
                    .HasForeignKey(ps => ps.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(ps => new { ps.ProductId, ps.SortOrder });

                // Tránh trùng spec theo tên trong cùng 1 sản phẩm
                e.HasIndex(ps => new { ps.ProductId, ps.Name }).IsUnique();

                e.Property(ps => ps.Name).HasMaxLength(150).IsRequired();
                e.Property(ps => ps.Value).HasMaxLength(1000);
            });

            // ===== Branch & Shipper =====
            b.Entity<Order>()
                .HasOne(o => o.PickupBranch)
                .WithMany(br => br.PickupOrders)
                .HasForeignKey(o => o.PickupBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Order>()
                .HasOne(o => o.Shipper)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.ShipperId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== Bank (Entity) =====
            b.Entity<Bank>(e =>
            {
                e.HasIndex(x => x.ShortName).IsUnique(false);
                e.HasIndex(x => x.Bin).IsUnique(false);
                e.HasIndex(x => x.Code).IsUnique(false); // muốn duy nhất thì .IsUnique(true)
            });

            // ===== ApplicationUser =====
            b.Entity<ApplicationUser>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ===== ApplicationUser =====
            b.Entity<ApplicationUser>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ===== Chat =====
            b.Entity<ChatThread>()
       .HasOne(t => t.Customer)
       .WithMany()
       .HasForeignKey(t => t.CustomerId)
       .OnDelete(DeleteBehavior.Restrict);

            b.Entity<ChatThread>()
                .HasOne(t => t.Staff)
                .WithMany()
                .HasForeignKey(t => t.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<ChatMessage>()
                .HasOne(m => m.Thread)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

        }

        // ====== Auto audit + slug ======
        public override int SaveChanges()
        {
            ApplyAuditAndSlug();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditAndSlug();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditAndSlug()
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    switch (entry.Entity)
                    {
                        case Product p:
                            if (entry.State == EntityState.Added && p.CreatedAt == default)
                                p.CreatedAt = utcNow;
                            p.UpdatedAt = utcNow;

                            if (string.IsNullOrWhiteSpace(p.Slug) || entry.Property(nameof(Product.Name)).IsModified)
                                p.Slug = ToSlug(p.Name);
                            break;

                        case Brand br:
                            if (string.IsNullOrWhiteSpace(br.Slug) || entry.Property(nameof(Brand.Name)).IsModified)
                                br.Slug = ToSlug(br.Name);
                            break;

                        case Category cat:
                            if (string.IsNullOrWhiteSpace(cat.Slug) || entry.Property(nameof(Category.Name)).IsModified)
                                cat.Slug = ToSlug(cat.Name);
                            break;
                    }
                }
            }
        }

        // Bỏ dấu tiếng Việt -> slug ASCII an toàn URL
        private static string ToSlug(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToLowerInvariant();

            // remove diacritics
            s = s.Normalize(NormalizationForm.FormD);
            var chars = s.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
            s = new string(chars).Normalize(NormalizationForm.FormC);

            // spaces & non-alnum
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"-+", "-").Trim('-');

            return s;
        }
    }
}
