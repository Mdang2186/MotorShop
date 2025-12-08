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

        public DbSet<AiConversation> AiConversations { get; set; } = null!;
        public DbSet<AiMessage> AiMessages { get; set; } = null!;

        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();

        public DbSet<ChatThread> ChatThreads { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        public DbSet<BranchInventory> BranchInventories { get; set; } = null!;

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ProductTag> ProductTags => Set<ProductTag>();
        public DbSet<ShopBankAccount> ShopBankAccounts => Set<ShopBankAccount>();

        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Bank> Banks => Set<Bank>();
        public DbSet<Shipper> Shippers => Set<Shipper>();

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

                // Mô tả full HTML
                e.Property(x => x.Description)
                 .HasColumnType("nvarchar(max)")
                 .IsRequired(false);

                // Concurrency token
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            // ===== BranchInventory =====
            b.Entity<BranchInventory>(e =>
            {
                // Một sản phẩm tại một chi nhánh chỉ có 1 dòng tồn
                e.HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();

                e.HasOne(x => x.Branch)
                    .WithMany(br => br.Inventories)    // nhớ thêm ICollection<BranchInventory> Inventories vào Branch
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.BranchInventories) // nhớ thêm ICollection<BranchInventory> BranchInventories vào Product
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            // ===== AI Conversation / Message =====
            b.Entity<AiConversation>(e =>
            {
                e.Property(c => c.Title).HasMaxLength(200);
                e.Property(c => c.LastUserMessage).HasMaxLength(1000);

                e.HasMany(c => c.Messages)
                    .WithOne(m => m.Conversation)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<AiMessage>(e =>
            {
                e.Property(m => m.Content).HasMaxLength(4000);
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
                e.HasOne(pt => pt.Product)
                    .WithMany(p => p.ProductTags)
                    .HasForeignKey(pt => pt.ProductId);

                e.HasOne(pt => pt.Tag)
                    .WithMany(t => t.ProductTags)
                    .HasForeignKey(pt => pt.TagId);
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

                e.HasIndex(pi => new { pi.ProductId, pi.SortOrder }).IsUnique();

                e.Property(pi => pi.ImageUrl).HasMaxLength(500).IsRequired();
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
                e.HasIndex(ps => new { ps.ProductId, ps.Name }).IsUnique();

                e.Property(ps => ps.Name).HasMaxLength(150).IsRequired();
                e.Property(ps => ps.Value).HasMaxLength(1000);
            });

            // ===== Branch & Shipper trên Order =====
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

            // ===== Bank =====
            b.Entity<Bank>(e =>
            {
                e.HasIndex(x => x.ShortName).IsUnique(false);
                e.HasIndex(x => x.Bin).IsUnique(false);
                e.HasIndex(x => x.Code).IsUnique(false);
            });
            // ===== ShopBankAccount =====
            b.Entity<ShopBankAccount>(e =>
            {
                e.HasOne(x => x.Bank)
                    .WithMany()
                    .HasForeignKey(x => x.BankId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(x => x.AccountNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                e.Property(x => x.AccountName)
                    .HasMaxLength(120)
                    .IsRequired();

                e.Property(x => x.Branch)
                    .HasMaxLength(120);

                e.Property(x => x.Note)
                    .HasMaxLength(300);

                // Một ngân hàng chỉ có tối đa 1 tài khoản mặc định
                e.HasIndex(x => new { x.BankId, x.IsDefault })
                    .HasFilter("[IsDefault] = 1");
            });

            // ===== ApplicationUser =====
            b.Entity<ApplicationUser>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ===== Chat =====
            b.Entity<ChatThread>(e =>
            {
                e.HasOne(t => t.Customer)
                    .WithMany()
                    .HasForeignKey(t => t.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Staff)
                    .WithMany()
                    .HasForeignKey(t => t.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<ChatMessage>(e =>
            {
                e.HasOne(m => m.Thread)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(m => m.ThreadId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .IsRequired(false) // Allow Null Sender
                    .OnDelete(DeleteBehavior.Restrict);
            });
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

                            if (string.IsNullOrWhiteSpace(p.Slug) ||
                                entry.Property(nameof(Product.Name)).IsModified)
                                p.Slug = ToSlug(p.Name);
                            break;

                        case Brand br:
                            if (string.IsNullOrWhiteSpace(br.Slug) ||
                                entry.Property(nameof(Brand.Name)).IsModified)
                                br.Slug = ToSlug(br.Name);
                            break;

                        case Category cat:
                            if (string.IsNullOrWhiteSpace(cat.Slug) ||
                                entry.Property(nameof(Category.Name)).IsModified)
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
