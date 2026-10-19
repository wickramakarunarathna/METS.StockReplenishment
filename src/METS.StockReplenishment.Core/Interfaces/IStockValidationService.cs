namespace METS.StockReplenishment.Core.Interfaces;

public interface IStockValidationService
{
	/// <summary>
	/// Asynchronously validates stock availability for the specified request.
	/// </summary>
	/// <param name="requestId">The identifier of the request to validate.</param>
	/// <returns>A task that represents the asynchronous validation operation.</returns>
	Task ValidateStockAvailabilityAsync(int requestId);
}