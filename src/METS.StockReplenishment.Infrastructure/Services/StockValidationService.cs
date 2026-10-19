using METS.StockReplenishment.Core.Enums;
using METS.StockReplenishment.Core.Interfaces;
using METS.StockReplenishment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace METS.StockReplenishment.Infrastructure.Services;

public class StockValidationService : IStockValidationService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<StockValidationService> _logger;

	public StockValidationService(
		IServiceScopeFactory scopeFactory,
		ILogger<StockValidationService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	/// <summary>
	/// Validates stock availability for the specified replenishment request asynchronously and updates the request's stock
	/// validation status, message, and timestamps in the database.
	/// </summary>
	/// <remarks>Runs in a new service scope for background execution. Loads the request with its line items, marks
	/// validation as InProgress, persists intermediate and final status updates, and logs progress. May simulate external
	/// service latency and set statuses to Available, PartiallyAvailable, or Unavailable. Returns immediately if the
	/// request is not found.</remarks>
	/// <param name="requestId">Identifier of the replenishment request to validate.</param>
	/// <returns>A Task that represents the asynchronous operation.</returns>
	public async Task ValidateStockAvailabilityAsync(int requestId)
	{
		_logger.LogInformation("Starting stock validation for request {RequestId}", requestId);

		// Use a new scope since this runs in a background task
		using var scope = _scopeFactory.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var request = await context.ReplenishmentRequests
			.Include(r => r.LineItems)
			.FirstOrDefaultAsync(r => r.Id == requestId);

		if (request == null)
		{
			_logger.LogWarning("Request {RequestId} not found for stock validation", requestId);
			return;
		}

		// Mark as InProgress
		request.StockValidationStatus = StockValidationStatus.InProgress;
		request.StockValidationMessage = "Checking stock availability...";
		request.UpdatedAt = DateTime.UtcNow;
		await context.SaveChangesAsync();

		// Simulate slow external service (2-5 seconds)
		var random = new Random();
		var delay = random.Next(2000, 5001);
		await Task.Delay(delay);

		// Simulate validation results randomly
		var roll = random.Next(100);
		if (roll < 60) // 60% chance all available
		{
			request.StockValidationStatus = StockValidationStatus.Available;
			request.StockValidationMessage = "All items are available in sufficient quantities.";
		}
		else if (roll < 90) // 30% chance partially available
		{
			request.StockValidationStatus = StockValidationStatus.PartiallyAvailable;
			var limitedItem = request.LineItems.First();
			request.StockValidationMessage = $"Some items have limited availability. {limitedItem.ArticleNumber} has limited stock.";
		}
		else // 10% chance unavailable
		{
			request.StockValidationStatus = StockValidationStatus.Unavailable;
			request.StockValidationMessage = "Required materials are currently out of stock. Please try again later.";
		}

		request.UpdatedAt = DateTime.UtcNow;
		await context.SaveChangesAsync();

		_logger.LogInformation(
			"Stock validation completed for request {RequestId}: {Status}",
			requestId, request.StockValidationStatus);
	}
}