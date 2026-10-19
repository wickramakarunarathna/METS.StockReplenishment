using METS.StockReplenishment.Core.DTOs;
using METS.StockReplenishment.Core.Entities;

namespace METS.StockReplenishment.Core.Interfaces;

public interface IReplenishmentRequestRepository
{
	/// <summary>
	/// Gets the ReplenishmentRequest with the specified identifier asynchronously.
	/// </summary>
	/// <param name="id">Identifier of the ReplenishmentRequest to retrieve.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the ReplenishmentRequest if found;
	/// otherwise, null.</returns>
	Task<ReplenishmentRequest?> GetByIdAsync(int id);

	/// <summary>
	/// Gets a paged list of replenishment requests that match the specified filter.
	/// </summary>
	/// <param name="filter">Criteria used to filter, sort, and page replenishment requests.</param>
	/// <returns>A task that resolves to a PagedResultDto<ReplenishmentRequest> containing the matching items.</returns>
	Task<PagedResultDto<ReplenishmentRequest>> GetFilteredAsync(RequestFilterDto filter);

	/// <summary>
	/// Adds the specified replenishment request asynchronously and returns the created request.
	/// </summary>
	/// <remarks>Performs validation and persists the request. Server-assigned fields such as identifiers or
	/// timestamps may be populated on the returned object. May throw ArgumentNullException if request is null or
	/// ArgumentException for invalid data.</remarks>
	/// <param name="request">The replenishment request to add.</param>
	/// <returns>A task that completes with the created ReplenishmentRequest.</returns>
	Task<ReplenishmentRequest> AddAsync(ReplenishmentRequest request);

	/// <summary>
	/// Updates an existing replenishment request asynchronously.
	/// </summary>
	/// <param name="request">The replenishment request to update.</param>
	/// <returns>A task that represents the asynchronous update operation.</returns>
	Task UpdateAsync(ReplenishmentRequest request);

	/// <summary>
	/// Generates a unique request number asynchronously.
	/// </summary>
	/// <remarks>The format and sequencing of the request number are implementation-specific.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains the generated request number.</returns>
	Task<string> GenerateRequestNumberAsync();

	/// <summary>
	/// Asynchronously retrieves distinct location names.
	/// </summary>
	/// <remarks>Results are unique. Order is not guaranteed and the list may be empty.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct location names.</returns>
	Task<List<string>> GetDistinctLocationsAsync();

	/// <summary>
	/// Asynchronously gets the distinct stock location options.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct
	/// StockLocationOptionDto.</returns>
	Task<List<StockLocationOptionDto>> GetDistinctLocationOptionsAsync();

	/// <summary>
	/// Retrieves distinct article option DTOs asynchronously.
	/// </summary>
	/// <remarks>Distinctness is enforced; the returned sequence has no guaranteed order.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct ArticleOptionDto
	/// objects.</returns>
	Task<List<ArticleOptionDto>> GetDistinctArticleOptionsAsync();

	/// <summary>
	/// Asynchronously retrieves the current stock balances.
	/// </summary>
	/// <remarks>Performs I/O and may throw exceptions for connectivity or data errors.</remarks>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of StockBalanceDto with the current
	/// stock balances.</returns>
	Task<List<StockBalanceDto>> GetStockBalancesAsync();
}