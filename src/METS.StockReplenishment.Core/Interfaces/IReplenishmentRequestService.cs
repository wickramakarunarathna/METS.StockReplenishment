using METS.StockReplenishment.Core.DTOs;

namespace METS.StockReplenishment.Core.Interfaces;

public interface IReplenishmentRequestService
{
	/// <summary>
	/// Retrieves a replenishment request by identifier asynchronously.
	/// </summary>
	/// <param name="id">The identifier of the replenishment request.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the ReplenishmentRequestDto for the
	/// specified identifier, or null if not found.</returns>
	Task<ReplenishmentRequestDto> GetByIdAsync(int id);

	/// <summary>
	/// Retrieves a paged list of replenishment requests that match the specified filter.
	/// </summary>
	/// <remarks>Filtering, sorting, and paging are applied server-side; the returned page may be empty if no items
	/// match.</remarks>
	/// <param name="filter">Filter criteria for querying replenishment requests, including paging, sorting, and search conditions.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is a paged result containing
	/// ReplenishmentRequestDto items that match the filter.</returns>
	Task<PagedResultDto<ReplenishmentRequestDto>> GetFilteredAsync(RequestFilterDto filter);

	/// <summary>
	/// Creates a replenishment request asynchronously from the specified DTO.
	/// </summary>
	/// <param name="dto">The DTO that contains the data required to create the replenishment request.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> CreateAsync(CreateReplenishmentRequestDto dto);

	/// <summary>
	/// Asynchronously updates the replenishment request with the specified identifier.
	/// </summary>
	/// <remarks>Validates input and persists changes; may throw if the request does not exist or validation
	/// fails.</remarks>
	/// <param name="id">Identifier of the replenishment request to update.</param>
	/// <param name="dto">DTO containing the updated properties for the replenishment request.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> UpdateAsync(int id, UpdateReplenishmentRequestDto dto);

	/// <summary>
	/// Submits the replenishment request with the specified identifier asynchronously.
	/// </summary>
	/// <param name="id">The identifier of the replenishment request to submit.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the submitted ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> SubmitAsync(int id);

	/// <summary>
	/// Asynchronously approves the replenishment request identified by the specified id.
	/// </summary>
	/// <param name="id">The identifier of the replenishment request to approve.</param>
	/// <param name="dto">Approval details and options.</param>
	/// <returns>A task whose result is the approved ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> ApproveAsync(int id, ApproveRequestDto dto);

	/// <summary>
	/// Rejects a replenishment request and returns the updated request.
	/// </summary>
	/// <param name="id">The identifier of the replenishment request to reject.</param>
	/// <param name="dto">Rejection details, such as reason and optional comments.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> RejectAsync(int id, RejectRequestDto dto);

	/// <summary>
	/// Fulfills a replenishment request asynchronously.
	/// </summary>
	/// <param name="id">Identifier of the replenishment request to fulfill.</param>
	/// <param name="dto">Fulfillment details to apply to the request.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated ReplenishmentRequestDto.</returns>
	Task<ReplenishmentRequestDto> FulfillAsync(int id, FulfillRequestDto dto);

	/// <summary>
	/// Gets the validation status for the stock identified by the specified id.
	/// </summary>
	/// <remarks>Performs an asynchronous, potentially I/O-bound operation to obtain the validation
	/// status.</remarks>
	/// <param name="id">Identifier of the stock to retrieve validation status for.</param>
	/// <returns>A Task whose result is a StockValidationResultDto containing validation details.</returns>
	Task<StockValidationResultDto> GetStockValidationStatusAsync(int id);

	/// <summary>
	/// Asynchronously retrieves distinct location names.
	/// </summary>
	/// <remarks>The returned list contains unique location names. Order is not guaranteed; callers should not rely
	/// on any specific ordering.</remarks>
	/// <returns>A task whose result is a list of distinct location names.</returns>
	Task<List<string>> GetDistinctLocationsAsync();

	/// <summary>
	/// Retrieves distinct stock location options.
	/// </summary>
	/// <remarks>Duplicates are removed; the ordering of the returned list is implementation-dependent. The
	/// operation may query a data store and can surface data-access exceptions.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct
	/// StockLocationOptionDto instances.</returns>
	Task<List<StockLocationOptionDto>> GetDistinctLocationOptionsAsync();

	/// <summary>
	/// Asynchronously retrieves a distinct List<ArticleOptionDto> representing available article options.
	/// </summary>
	/// <remarks>Distinctness is determined by ArticleOptionDto equality semantics; the order of returned items is
	/// not guaranteed.</remarks>
	/// <returns>A Task whose result is a List<ArticleOptionDto> containing distinct article options.</returns>
	Task<List<ArticleOptionDto>> GetDistinctArticleOptionsAsync();

	/// <summary>
	/// Asynchronously retrieves stock balances for all items.
	/// </summary>
	/// <remarks>Does not block the calling thread. Returns an empty list if no stock records exist.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of StockBalanceDto representing
	/// stock balances.</returns>
	Task<List<StockBalanceDto>> GetStockBalancesAsync();
}