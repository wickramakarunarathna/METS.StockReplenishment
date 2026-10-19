using METS.StockReplenishment.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace METS.StockReplenishment.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
	public DbSet<ReplenishmentLineItem> ReplenishmentLineItems => Set<ReplenishmentLineItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<ReplenishmentRequest>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.RequestNumber).IsRequired().HasMaxLength(20);
			entity.HasIndex(e => e.RequestNumber).IsUnique();
			entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
			entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
			entity.Property(e => e.StockValidationStatus).HasConversion<string>().HasMaxLength(30);
			entity.Property(e => e.StockLocationCode).IsRequired().HasMaxLength(50);
			entity.Property(e => e.StockLocationName).IsRequired().HasMaxLength(200);
			entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(100);
			entity.Property(e => e.RejectionReason).HasMaxLength(500);
			entity.Property(e => e.ReviewedBy).HasMaxLength(100);
			entity.Property(e => e.StockValidationMessage).HasMaxLength(500);

			entity.HasMany(e => e.LineItems)
				  .WithOne(e => e.ReplenishmentRequest)
				  .HasForeignKey(e => e.ReplenishmentRequestId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ReplenishmentLineItem>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.ArticleNumber).IsRequired().HasMaxLength(50);
			entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
			entity.Property(e => e.RequestedQuantity).HasPrecision(18, 2);
			entity.Property(e => e.FulfilledQuantity).HasPrecision(18, 2);
		});

		base.OnModelCreating(modelBuilder);
	}
}