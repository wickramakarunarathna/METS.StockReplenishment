using METS.StockReplenishment.Core.Entities;
using METS.StockReplenishment.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace METS.StockReplenishment.Infrastructure.Data;

public static class DataSeeder
{
	public static void Seed(AppDbContext context)
	{
		SeedRoles(context);
		SeedUsers(context);
		SeedReplenishmentRequests(context);
	}

	private static void SeedRoles(AppDbContext context)
	{
		if (context.Roles.Any()) return;

		var roles = new List<IdentityRole>
		{
			new() { Id = "1", Name = "Worker", NormalizedName = "WORKER" },
			new() { Id = "2", Name = "Reviewer", NormalizedName = "REVIEWER" }
		};

		context.Roles.AddRange(roles);
		context.SaveChanges();
	}

	private static void SeedUsers(AppDbContext context)
	{
		if (context.Users.Any()) return;

		var passwordHasher = new PasswordHasher<IdentityUser>();

		// Worker user
		var worker = new IdentityUser
		{
			Id = "worker-id",
			UserName = "worker",
			NormalizedUserName = "WORKER",
			Email = "worker@mets.local",
			NormalizedEmail = "WORKER@METS.LOCAL",
			EmailConfirmed = true,
			SecurityStamp = Guid.NewGuid().ToString()
		};
		worker.PasswordHash = passwordHasher.HashPassword(worker, "worker123");

		// Reviewer user
		var reviewer = new IdentityUser
		{
			Id = "reviewer-id",
			UserName = "reviewer",
			NormalizedUserName = "REVIEWER",
			Email = "reviewer@mets.local",
			NormalizedEmail = "REVIEWER@METS.LOCAL",
			EmailConfirmed = true,
			SecurityStamp = Guid.NewGuid().ToString()
		};
		reviewer.PasswordHash = passwordHasher.HashPassword(reviewer, "reviewer123");

		context.Users.AddRange(worker, reviewer);
		context.SaveChanges();

		// Assign roles
		var workerRole = new IdentityUserRole<string>
		{
			UserId = worker.Id,
			RoleId = "1"
		};

		var reviewerRole = new IdentityUserRole<string>
		{
			UserId = reviewer.Id,
			RoleId = "2"
		};

		context.UserRoles.AddRange(workerRole, reviewerRole);
		context.SaveChanges();
	}

	private static void SeedReplenishmentRequests(AppDbContext context)
	{
		if (context.ReplenishmentRequests.Any()) return;

		var now = DateTime.UtcNow;

		var requests = new List<ReplenishmentRequest>
		{
            // Draft requests
            new()
			{
				RequestNumber = "REQ-2024-001",
				Status = RequestStatus.Draft,
				Priority = RequestPriority.Normal,
				StockLocationCode = "WH-A1",
				StockLocationName = "Warehouse A - Section 1",
				RequestedBy = "John Smith",
				RequestedAt = now.AddDays(-2),
				StockValidationStatus = StockValidationStatus.NotChecked,
				CreatedAt = now.AddDays(-2),
				UpdatedAt = now.AddDays(-2),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-1001", Description = "Steel Bolts M10x50", RequestedQuantity = 500 },
					new() { ArticleNumber = "MAT-1002", Description = "Steel Nuts M10", RequestedQuantity = 500 }
				}
			},
			new()
			{
				RequestNumber = "REQ-2024-002",
				Status = RequestStatus.Draft,
				Priority = RequestPriority.Urgent,
				StockLocationCode = "WH-B3",
				StockLocationName = "Warehouse B - Section 3",
				RequestedBy = "Maria Garcia",
				RequestedAt = now.AddDays(-1),
				StockValidationStatus = StockValidationStatus.NotChecked,
				CreatedAt = now.AddDays(-1),
				UpdatedAt = now.AddDays(-1),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-2001", Description = "Copper Wire 2.5mm", RequestedQuantity = 200 },
					new() { ArticleNumber = "MAT-2002", Description = "Insulation Tape", RequestedQuantity = 50 },
					new() { ArticleNumber = "MAT-2003", Description = "Terminal Connectors", RequestedQuantity = 100 }
				}
			},
            // Submitted requests (waiting for approval)
            new()
			{
				RequestNumber = "REQ-2024-003",
				Status = RequestStatus.Submitted,
				Priority = RequestPriority.Normal,
				StockLocationCode = "WH-A2",
				StockLocationName = "Warehouse A - Section 2",
				RequestedBy = "James Wilson",
				RequestedAt = now.AddDays(-5),
				SubmittedAt = now.AddDays(-4),
				StockValidationStatus = StockValidationStatus.Available,
				StockValidationMessage = "All items are available in sufficient quantities.",
				CreatedAt = now.AddDays(-5),
				UpdatedAt = now.AddDays(-4),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-3001", Description = "Aluminum Sheets 2mm", RequestedQuantity = 100 },
					new() { ArticleNumber = "MAT-3002", Description = "Rivets 4mm", RequestedQuantity = 1000 }
				}
			},
			new()
			{
				RequestNumber = "REQ-2024-004",
				Status = RequestStatus.Submitted,
				Priority = RequestPriority.Urgent,
				StockLocationCode = "WH-C1",
				StockLocationName = "Warehouse C - Section 1",
				RequestedBy = "Sarah Johnson",
				RequestedAt = now.AddDays(-3),
				SubmittedAt = now.AddDays(-2),
				StockValidationStatus = StockValidationStatus.PartiallyAvailable,
				StockValidationMessage = "Some items have limited availability. MAT-4002 only 30 units available out of 100 requested.",
				CreatedAt = now.AddDays(-3),
				UpdatedAt = now.AddDays(-2),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-4001", Description = "Hydraulic Hose 12mm", RequestedQuantity = 50 },
					new() { ArticleNumber = "MAT-4002", Description = "O-Ring Seals", RequestedQuantity = 100 }
				}
			},
            // Approved request
            new()
			{
				RequestNumber = "REQ-2024-005",
				Status = RequestStatus.Approved,
				Priority = RequestPriority.Low,
				StockLocationCode = "WH-A1",
				StockLocationName = "Warehouse A - Section 1",
				RequestedBy = "John Smith",
				RequestedAt = now.AddDays(-10),
				SubmittedAt = now.AddDays(-9),
				ReviewedBy = "Admin",
				ReviewedAt = now.AddDays(-8),
				StockValidationStatus = StockValidationStatus.Available,
				StockValidationMessage = "All items are available.",
				CreatedAt = now.AddDays(-10),
				UpdatedAt = now.AddDays(-8),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-5001", Description = "Lubricant Oil 5L", RequestedQuantity = 20 },
					new() { ArticleNumber = "MAT-5002", Description = "Filter Cartridges", RequestedQuantity = 10 }
				}
			},
            // Rejected request
            new()
			{
				RequestNumber = "REQ-2024-006",
				Status = RequestStatus.Rejected,
				Priority = RequestPriority.Normal,
				StockLocationCode = "WH-B1",
				StockLocationName = "Warehouse B - Section 1",
				RequestedBy = "Tom Brown",
				RequestedAt = now.AddDays(-7),
				SubmittedAt = now.AddDays(-6),
				ReviewedBy = "Admin",
				ReviewedAt = now.AddDays(-5),
				RejectionReason = "Duplicate request. Already covered by REQ-2024-005.",
				StockValidationStatus = StockValidationStatus.Available,
				StockValidationMessage = "All items are available.",
				CreatedAt = now.AddDays(-7),
				UpdatedAt = now.AddDays(-5),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-5001", Description = "Lubricant Oil 5L", RequestedQuantity = 15 }
				}
			},
            // Fulfilled request
            new()
			{
				RequestNumber = "REQ-2024-007",
				Status = RequestStatus.Fulfilled,
				Priority = RequestPriority.Urgent,
				StockLocationCode = "WH-C1",
				StockLocationName = "Warehouse C - Section 1",
				RequestedBy = "Emily Davis",
				RequestedAt = now.AddDays(-14),
				SubmittedAt = now.AddDays(-13),
				ReviewedBy = "Admin",
				ReviewedAt = now.AddDays(-12),
				FulfilledAt = now.AddDays(-10),
				StockValidationStatus = StockValidationStatus.Available,
				StockValidationMessage = "All items are available.",
				CreatedAt = now.AddDays(-14),
				UpdatedAt = now.AddDays(-10),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-6001", Description = "Welding Electrodes", RequestedQuantity = 200, FulfilledQuantity = 200 },
					new() { ArticleNumber = "MAT-6002", Description = "Safety Gloves (pairs)", RequestedQuantity = 50, FulfilledQuantity = 50 },
					new() { ArticleNumber = "MAT-6003", Description = "Welding Wire 1.2mm", RequestedQuantity = 30, FulfilledQuantity = 25 }
				}
			},
            // Another submitted request with low priority
            new()
			{
				RequestNumber = "REQ-2024-008",
				Status = RequestStatus.Submitted,
				Priority = RequestPriority.Low,
				StockLocationCode = "WH-B2",
				StockLocationName = "Warehouse B - Section 2",
				RequestedBy = "Robert Lee",
				RequestedAt = now.AddDays(-2),
				SubmittedAt = now.AddDays(-1),
				StockValidationStatus = StockValidationStatus.Available,
				StockValidationMessage = "All items are available in sufficient quantities.",
				CreatedAt = now.AddDays(-2),
				UpdatedAt = now.AddDays(-1),
				LineItems = new List<ReplenishmentLineItem>
				{
					new() { ArticleNumber = "MAT-7001", Description = "Cleaning Solvent 1L", RequestedQuantity = 30 },
					new() { ArticleNumber = "MAT-7002", Description = "Microfiber Cloths", RequestedQuantity = 100 }
				}
			}
		};

		context.ReplenishmentRequests.AddRange(requests);
		context.SaveChanges();
	}
}