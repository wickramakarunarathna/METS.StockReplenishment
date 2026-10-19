using METS.StockReplenishment.Core.DTOs;
using METS.StockReplenishment.Core.Entities;
using METS.StockReplenishment.Core.Enums;
using METS.StockReplenishment.Core.Exceptions;
using METS.StockReplenishment.Core.Interfaces;

namespace METS.StockReplenishment.Infrastructure.Services;

public class ReplenishmentRequestService : IReplenishmentRequestService
{
	private readonly IReplenishmentRequestRepository _repository;
	private readonly IStockValidationQueue _validationQueue;

	public ReplenishmentRequestService(
		IReplenishmentRequestRepository repository,
		IStockValidationQueue validationQueue)
	{
		_repository = repository;
		_validationQueue = validationQueue;
	}

	public async Task<ReplenishmentRequestDto> GetByIdAsync(int id)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);
		return MapToDto(request);
	}


	/// <summary>
	/// Retrieves a paged list of replenishment requests that match the specified filter.
	/// </summary>
	/// <remarks>Validates and normalizes pagination values, delegates retrieval to the repository, and maps domain
	/// entities to ReplenishmentRequestDto.</remarks>
	/// <param name="filter">Filter and pagination options. Page is normalized to a minimum of 1. PageSize is normalized to the range 1-50 with
	/// a default of 10.</param>
	/// <returns>A paged result containing mapped ReplenishmentRequestDto items, total count, page, and page size.</returns>
	public async Task<PagedResultDto<ReplenishmentRequestDto>> GetFilteredAsync(RequestFilterDto filter)
	{
		if (filter.Page < 1) filter.Page = 1;
		if (filter.PageSize < 1) filter.PageSize = 10;
		if (filter.PageSize > 50) filter.PageSize = 50;

		var result = await _repository.GetFilteredAsync(filter);
		return new PagedResultDto<ReplenishmentRequestDto>
		{
			Items = result.Items.Select(MapToDto).ToList(),
			TotalCount = result.TotalCount,
			Page = result.Page,
			PageSize = result.PageSize
		};
	}

	/// <summary>
	/// Creates a replenishment request from the provided DTO by generating a request number, initializing default status
	/// and timestamps, mapping line items, persisting the entity, and returning the created DTO.
	/// </summary>
	/// <remarks>Asynchronously generates a request number and persists the request via the repository. Sets Status
	/// to Draft, StockValidationStatus to NotChecked, and CreatedAt/UpdatedAt to UTC now.</remarks>
	/// <param name="dto">Data for the new replenishment request, including priority, stock location code and name, requestedBy, and line
	/// items.</param>
	/// <returns>The persisted ReplenishmentRequestDto containing the generated request number, status, timestamps, and mapped line
	/// items.</returns>
	public async Task<ReplenishmentRequestDto> CreateAsync(CreateReplenishmentRequestDto dto)
	{
		var requestNumber = await _repository.GenerateRequestNumberAsync();
		var now = DateTime.UtcNow;

		var request = new ReplenishmentRequest
		{
			RequestNumber = requestNumber,
			Status = RequestStatus.Draft,
			Priority = dto.Priority,
			StockLocationCode = dto.StockLocationCode,
			StockLocationName = dto.StockLocationName,
			RequestedBy = dto.RequestedBy,
			RequestedAt = now,
			StockValidationStatus = StockValidationStatus.NotChecked,
			CreatedAt = now,
			UpdatedAt = now,
			LineItems = dto.LineItems.Select(li => new ReplenishmentLineItem
			{
				ArticleNumber = li.ArticleNumber,
				Description = li.Description,
				RequestedQuantity = li.RequestedQuantity
			}).ToList()
		};

		var created = await _repository.AddAsync(request);
		return MapToDto(created);
	}

	/// <summary>
	/// Updates a replenishment request identified by id using values from dto.
	/// </summary>
	/// <remarks>Replaces existing line items with those from dto, sets UpdatedAt to UTC now, and persists the
	/// changes.</remarks>
	/// <param name="id">Identifier of the replenishment request to update.</param>
	/// <param name="dto">DTO containing updated request fields and line items.</param>
	/// <returns>The updated ReplenishmentRequestDto.</returns>
	/// <exception cref="NotFoundException">Thrown when no replenishment request exists with the specified id.</exception>
	/// <exception cref="BusinessRuleException">Thrown when the request is not in Draft status and cannot be updated.</exception>
	public async Task<ReplenishmentRequestDto> UpdateAsync(int id, UpdateReplenishmentRequestDto dto)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		if (request.Status != RequestStatus.Draft)
			throw new BusinessRuleException("Only draft requests can be updated.");

		request.StockLocationCode = dto.StockLocationCode;
		request.StockLocationName = dto.StockLocationName;
		request.Priority = dto.Priority;
		request.UpdatedAt = DateTime.UtcNow;

		// Replace line items
		request.LineItems.Clear();
		request.LineItems = dto.LineItems.Select(li => new ReplenishmentLineItem
		{
			ReplenishmentRequestId = id,
			ArticleNumber = li.ArticleNumber,
			Description = li.Description,
			RequestedQuantity = li.RequestedQuantity
		}).ToList();

		await _repository.UpdateAsync(request);
		return MapToDto(request);
	}

	/// <summary>
	/// Submits a draft replenishment request identified by id, updates its status and timestamps, marks stock validation
	/// as pending, and queues asynchronous stock validation.
	/// </summary>
	/// <remarks>Updates the repository, sets SubmittedAt and UpdatedAt to UTC, and queues non-blocking stock
	/// validation.</remarks>
	/// <param name="id">Replenishment request identifier.</param>
	/// <returns>A task that returns the updated ReplenishmentRequestDto after successful submission.</returns>
	/// <exception cref="NotFoundException">Thrown when no replenishment request exists with the specified id.</exception>
	/// <exception cref="BusinessRuleException">Thrown when the request is not in Draft status or when it contains no line items.</exception>
	public async Task<ReplenishmentRequestDto> SubmitAsync(int id)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		if (request.Status != RequestStatus.Draft)
			throw new BusinessRuleException("Only draft requests can be submitted.");

		if (!request.LineItems.Any())
			throw new BusinessRuleException("Cannot submit a request with no line items.");

		request.Status = RequestStatus.Submitted;
		request.SubmittedAt = DateTime.UtcNow;
		request.StockValidationStatus = StockValidationStatus.Pending;
		request.StockValidationMessage = "Stock validation queued...";
		request.UpdatedAt = DateTime.UtcNow;

		await _repository.UpdateAsync(request);

		// Queue async stock validation (non-blocking)
		await _validationQueue.QueueValidationAsync(id);

		return MapToDto(request);
	}
	
	/// <summary>
	/// Approves a submitted replenishment request identified by id, updates its status and timestamps, and records the reviewer information.
	/// </summary>
	/// <param name="id">Replenishment request identifier.</param>
	/// <param name="dto">DTO containing approval details.</param>
	/// <returns>A task that returns the updated ReplenishmentRequestDto after successful approval.</returns>
	/// <exception cref="NotFoundException">Thrown when no replenishment request exists with the specified id.</exception>
	/// <exception cref="BusinessRuleException">Thrown when the request is not in Submitted status and cannot be approved.</exception>
	public async Task<ReplenishmentRequestDto> ApproveAsync(int id, ApproveRequestDto dto)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		if (request.Status != RequestStatus.Submitted)
			throw new BusinessRuleException("Only submitted requests can be approved.");

		request.Status = RequestStatus.Approved;
		request.ReviewedBy = dto.ReviewedBy;
		request.ReviewedAt = DateTime.UtcNow;
		request.RejectionReason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();
		request.UpdatedAt = DateTime.UtcNow;

		await _repository.UpdateAsync(request);
		return MapToDto(request);
	}

	/// <summary>
	/// Rejects a submitted replenishment request, records the reviewer, UTC review time, and rejection reason, then
	/// persists the update.
	/// </summary>
	/// <remarks>Sets Status to Rejected, trims the rejection reason, sets ReviewedAt to UTC now, updates UpdatedAt,
	/// and saves the change to the repository.</remarks>
	/// <param name="id">Replenishment request identifier.</param>
	/// <param name="dto">Rejection details including ReviewedBy and Reason.</param>
	/// <returns>The updated ReplenishmentRequestDto representing the rejected request.</returns>
	/// <exception cref="NotFoundException">If no replenishment request exists with the specified id.</exception>
	/// <exception cref="BusinessRuleException">If the request is not in Submitted status.</exception>
	public async Task<ReplenishmentRequestDto> RejectAsync(int id, RejectRequestDto dto)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		if (request.Status != RequestStatus.Submitted)
			throw new BusinessRuleException("Only submitted requests can be rejected.");

		request.Status = RequestStatus.Rejected;
		request.ReviewedBy = dto.ReviewedBy;
		request.ReviewedAt = DateTime.UtcNow;
		request.RejectionReason = dto.Reason.Trim();
		request.UpdatedAt = DateTime.UtcNow;

		await _repository.UpdateAsync(request);
		return MapToDto(request);
	}

	/// <summary>
	/// Apply fulfillment quantities to a replenishment request, validate business rules, update status to Fulfilled,
	/// persist changes, and return the updated DTO.
	/// </summary>
	/// <remarks>Sets FulfilledAt and UpdatedAt to UTC and persists the updated request.</remarks>
	/// <param name="id">Identifier of the replenishment request to fulfill.</param>
	/// <param name="dto">Fulfillment details including per-line-item fulfilled quantities.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the updated ReplenishmentRequestDto.</returns>
	/// <exception cref="NotFoundException">Thrown when the replenishment request or a referenced line item cannot be found.</exception>
	/// <exception cref="BusinessRuleException">Thrown when the request is not approved or when a fulfilled quantity exceeds the requested quantity.</exception>
	public async Task<ReplenishmentRequestDto> FulfillAsync(int id, FulfillRequestDto dto)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		if (request.Status != RequestStatus.Approved)
			throw new BusinessRuleException("Only approved requests can be fulfilled.");

		foreach (var item in dto.Items)
		{
			var lineItem = request.LineItems.FirstOrDefault(li => li.Id == item.LineItemId)
				?? throw new NotFoundException("LineItem", item.LineItemId);

			if (item.FulfilledQuantity > lineItem.RequestedQuantity)
				throw new BusinessRuleException(
					$"Fulfilled quantity ({item.FulfilledQuantity}) cannot exceed requested quantity ({lineItem.RequestedQuantity}) for item {lineItem.ArticleNumber}.");

			lineItem.FulfilledQuantity = item.FulfilledQuantity;
		}

		request.Status = RequestStatus.Fulfilled;
		request.FulfilledAt = DateTime.UtcNow;
		request.UpdatedAt = DateTime.UtcNow;

		await _repository.UpdateAsync(request);
		return MapToDto(request);
	}

	/// <summary>
	/// Retrieves the stock validation status for the replenishment request with the specified identifier.
	/// </summary>
	/// <remarks>Queries the repository asynchronously and maps the request's StockValidationStatus and
	/// StockValidationMessage to the result.</remarks>
	/// <param name="id">The identifier of the replenishment request.</param>
	/// <returns>A StockValidationResultDto containing the request identifier, stock validation status as a string, and an
	/// accompanying message.</returns>
	/// <exception cref="NotFoundException">Thrown when no replenishment request exists with the specified id.</exception>
	public async Task<StockValidationResultDto> GetStockValidationStatusAsync(int id)
	{
		var request = await _repository.GetByIdAsync(id)
			?? throw new NotFoundException("ReplenishmentRequest", id);

		return new StockValidationResultDto
		{
			RequestId = request.Id,
			Status = request.StockValidationStatus.ToString(),
			Message = request.StockValidationMessage
		};
	}

	/// <summary>
	/// Asynchronously retrieves a list of distinct location names.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct location names.</returns>
	public async Task<List<string>> GetDistinctLocationsAsync()
	{
		return await _repository.GetDistinctLocationsAsync();
	}

	/// <summary>
	/// Retrieves distinct stock location options asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct
	/// StockLocationOptionDto.</returns>
	public async Task<List<StockLocationOptionDto>> GetDistinctLocationOptionsAsync()
	{
		return await _repository.GetDistinctLocationOptionsAsync();
	}

	/// <summary>
	/// Retrieves distinct article options as a list of ArticleOptionDto.
	/// </summary>
	/// <remarks>Execution is asynchronous and delegates to the underlying repository; may perform I/O.</remarks>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct ArticleOptionDto.</returns>
	public async Task<List<ArticleOptionDto>> GetDistinctArticleOptionsAsync()
	{
		return await _repository.GetDistinctArticleOptionsAsync();
	}

	/// <summary>
	/// Asynchronously retrieves a list of stock balances.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of stock balances.</returns>
	public async Task<List<StockBalanceDto>> GetStockBalancesAsync()
	{
		return await _repository.GetStockBalancesAsync();
	}

	private static ReplenishmentRequestDto MapToDto(ReplenishmentRequest request)
	{
		return new ReplenishmentRequestDto
		{
			Id = request.Id,
			RequestNumber = request.RequestNumber,
			Status = request.Status.ToString(),
			Priority = request.Priority.ToString(),
			StockLocationCode = request.StockLocationCode,
			StockLocationName = request.StockLocationName,
			RequestedBy = request.RequestedBy,
			RequestedAt = request.RequestedAt,
			SubmittedAt = request.SubmittedAt,
			ReviewedBy = request.ReviewedBy,
			ReviewedAt = request.ReviewedAt,
			RejectionReason = request.RejectionReason,
			FulfilledAt = request.FulfilledAt,
			StockValidationStatus = request.StockValidationStatus.ToString(),
			StockValidationMessage = request.StockValidationMessage,
			CreatedAt = request.CreatedAt,
			UpdatedAt = request.UpdatedAt,
			LineItems = request.LineItems.Select(li => new LineItemDto
			{
				Id = li.Id,
				ArticleNumber = li.ArticleNumber,
				Description = li.Description,
				RequestedQuantity = li.RequestedQuantity,
				FulfilledQuantity = li.FulfilledQuantity
			}).ToList()
		};
	}
}