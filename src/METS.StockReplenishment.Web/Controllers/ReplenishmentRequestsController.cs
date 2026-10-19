using METS.StockReplenishment.Core.DTOs;
using METS.StockReplenishment.Core.Enums;
using METS.StockReplenishment.Core.Exceptions;
using METS.StockReplenishment.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace METS.StockReplenishment.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Worker,Reviewer")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReplenishmentRequestsController : ControllerBase
{
	private readonly IReplenishmentRequestService _service;

	public ReplenishmentRequestsController(IReplenishmentRequestService service)
	{
		_service = service;
	}

	/// <summary>
	/// Gets a paged list of replenishment requests filtered by optional status, priority, stock location code, article
	/// number, and free-text search.
	/// </summary>
	/// <remarks>Defaults: page = 1, pageSize = 10. Server-side paging and filtering are applied.</remarks>
	/// <param name="status">Request status to filter by; null to include all statuses.</param>
	/// <param name="priority">Request priority to filter by; null to include all priorities.</param>
	/// <param name="stockLocationCode">Stock location code to filter by; null or empty to include all locations.</param>
	/// <param name="articleNumber">Article number to filter by; null or empty to include all articles.</param>
	/// <param name="searchTerm">Free-text search term to match request fields; null or empty to disable text filtering.</param>
	/// <param name="page">Page number starting at 1.</param>
	/// <param name="pageSize">Number of items returned per page.</param>
	/// <returns>200 OK with a paged result of ReplenishmentRequestDto objects that match the provided filters.</returns>
	[HttpGet]
	[ProducesResponseType(typeof(PagedResultDto<ReplenishmentRequestDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<PagedResultDto<ReplenishmentRequestDto>>> GetAll(
		[FromQuery] RequestStatus? status,
		[FromQuery] RequestPriority? priority,
		[FromQuery] string? stockLocationCode,
		[FromQuery] string? articleNumber,
		[FromQuery] string? searchTerm,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10)
	{
		var filter = new RequestFilterDto
		{
			Status = status,
			Priority = priority,
			StockLocationCode = stockLocationCode,
			ArticleNumber = articleNumber,
			SearchTerm = searchTerm,
			Page = page,
			PageSize = pageSize
		};

		var result = await _service.GetFilteredAsync(filter);
		return Ok(result);
	}

	/// <summary>
	/// Gets the replenishment request with the specified identifier.
	/// </summary>
	/// <param name="id">The identifier of the replenishment request.</param>
	/// <returns>An ActionResult containing the ReplenishmentRequestDto when found; NotFound (404) if no request exists for the
	/// specified identifier.</returns>
	[HttpGet("{id:int}")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> GetById(int id)
	{
		try
		{
			var result = await _service.GetByIdAsync(id);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Creates a replenishment request and returns 201 Created with the created ReplenishmentRequestDto.
	/// </summary>
	/// <remarks>Requires an authenticated user in the Worker role.</remarks>
	/// <param name="dto">Replenishment request data. RequestedBy is set to the current user's name before validation.</param>
	/// <returns>201 Created with the created ReplenishmentRequestDto, or 400 Bad Request if model validation fails.</returns>
	[HttpPost]
	[Authorize(Roles = "Worker")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Create(
		[FromBody] CreateReplenishmentRequestDto dto)
	{
		dto.RequestedBy = User.Identity?.Name ?? string.Empty;
		ModelState.Remove(nameof(CreateReplenishmentRequestDto.RequestedBy));

		if (!TryValidateModel(dto))
			return BadRequest(ModelState);

		var result = await _service.CreateAsync(dto);
		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
	}

	/// <summary>
	/// Updates the replenishment request with the specified identifier and returns the updated ReplenishmentRequestDto.
	/// </summary>
	/// <remarks>Requires authorization with the Worker role. Returns 400 when model validation
	/// fails.</remarks>
	/// <param name="id">Identifier of the replenishment request to update.</param>
	/// <param name="dto">DTO containing updated values for the replenishment request.</param>
	/// <returns>An ActionResult containing the updated ReplenishmentRequestDto on success; returns 400 for invalid input or
	/// business rule violations, and 404 if the request is not found.</returns>
	[HttpPut("{id:int}")]
	[Authorize(Roles = "Worker")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Update(
		int id, [FromBody] UpdateReplenishmentRequestDto dto)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var result = await _service.UpdateAsync(id, dto);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (BusinessRuleException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Submits the replenishment request identified by the specified id.
	/// </summary>
	/// <remarks>Requires the Worker role. Maps NotFoundException to 404 and BusinessRuleException to 400.
	/// Accessible via HTTP POST at the {id}/submit route.</remarks>
	/// <param name="id">The identifier of the replenishment request to submit.</param>
	/// <returns>An ActionResult containing the submitted ReplenishmentRequestDto if successful; 404 Not Found if the request
	/// does not exist; 400 Bad Request if submission violates business rules.</returns>
	[HttpPost("{id:int}/submit")]
	[Authorize(Roles = "Worker")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Submit(int id)
	{
		try
		{
			var result = await _service.SubmitAsync(id);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (BusinessRuleException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Approve a replenishment request identified by the specified id and return the updated request.
	/// </summary>
	/// <remarks>Requires the Reviewer role. Produces 200 (ReplenishmentRequestDto), 400 (BadRequest) or 404
	/// (NotFound).</remarks>
	/// <param name="id">Identifier of the replenishment request to approve.</param>
	/// <param name="dto">Approval details; ReviewedBy is overwritten with the current user's name.</param>
	/// <returns>An ActionResult containing the updated ReplenishmentRequestDto when approved; returns 400 for invalid input or
	/// business rule violations, and 404 if the request is not found.</returns>
	[HttpPost("{id:int}/approve")]
	[Authorize(Roles = "Reviewer")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Approve(
		int id, [FromBody] ApproveRequestDto dto)
	{
		dto.ReviewedBy = User.Identity?.Name ?? string.Empty;

		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var result = await _service.ApproveAsync(id, dto);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (BusinessRuleException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Reject the replenishment request with the specified identifier and return the updated request.
	/// </summary>
	/// <remarks>Requires the Reviewer role. Validates model state and maps NotFoundException to 404 and
	/// BusinessRuleException to 400.</remarks>
	/// <param name="id">Identifier of the replenishment request to reject.</param>
	/// <param name="dto">Rejection details, including reason; ReviewedBy is set to the current user's name.</param>
	/// <returns>200 OK with the updated ReplenishmentRequestDto when rejection succeeds; 400 for invalid input or business rule
	/// violations; 404 if the request is not found.</returns>
	[HttpPost("{id:int}/reject")]
	[Authorize(Roles = "Reviewer")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Reject(
		int id, [FromBody] RejectRequestDto dto)
	{
		dto.ReviewedBy = User.Identity?.Name ?? string.Empty;

		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var result = await _service.RejectAsync(id, dto);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (BusinessRuleException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Fulfill the specified replenishment request and return the updated request.
	/// </summary>
	/// <remarks>Requires Reviewer role. Produces 200 with the updated ReplenishmentRequestDto, 400 for model
	/// validation or BusinessRuleException, and 404 for NotFoundException.</remarks>
	/// <param name="id">Identifier of the replenishment request to fulfill.</param>
	/// <param name="dto">Fulfillment details for the request.</param>
	/// <returns>An ActionResult containing the updated ReplenishmentRequestDto on success; returns 400 for validation or
	/// business-rule failures, or 404 if the request is not found.</returns>
	[HttpPost("{id:int}/fulfill")]
	[Authorize(Roles = "Reviewer")]
	[ProducesResponseType(typeof(ReplenishmentRequestDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ReplenishmentRequestDto>> Fulfill(
		int id, [FromBody] FulfillRequestDto dto)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var result = await _service.FulfillAsync(id, dto);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
		catch (BusinessRuleException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Gets the stock validation status for the item with the specified identifier.
	/// </summary>
	/// <remarks>Maps NotFoundException to a 404 response and returns an error message in the response body; returns
	/// 200 with the StockValidationResultDto on success.</remarks>
	/// <param name="id">The identifier of the item for which to retrieve stock validation.</param>
	/// <returns>An ActionResult containing the stock validation result as a StockValidationResultDto when found; NotFound (404) when
	/// the item does not exist.</returns>
	[HttpGet("{id:int}/stock-validation")]
	[ProducesResponseType(typeof(StockValidationResultDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<StockValidationResultDto>> GetStockValidation(int id)
	{
		try
		{
			var result = await _service.GetStockValidationStatusAsync(id);
			return Ok(result);
		}
		catch (NotFoundException ex)
		{
			return NotFound(new { error = ex.Message });
		}
	}

	/// <summary>
	/// Retrieves distinct location names.
	/// </summary>
	/// <remarks>Performs an asynchronous call to the underlying service and returns 200 OK with the
	/// results.</remarks>
	/// <returns>An ActionResult that contains a list of distinct location names.</returns>
	[HttpGet("locations")]
	[ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
	public async Task<ActionResult<List<string>>> GetLocations()
	{
		var result = await _service.GetDistinctLocationsAsync();
		return Ok(result);
	}

	/// <summary>
	/// Gets distinct stock location options.
	/// </summary>
	/// <remarks>Retrieves the distinct location options asynchronously from the underlying service.</remarks>
	/// <returns>An ActionResult containing a 200 OK response with a list of StockLocationOptionDto representing distinct stock
	/// location options.</returns>
	[HttpGet("location-options")]
	[ProducesResponseType(typeof(List<StockLocationOptionDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<List<StockLocationOptionDto>>> GetLocationOptions()
	{
		var result = await _service.GetDistinctLocationOptionsAsync();
		return Ok(result);
	}

	/// <summary>
	/// Retrieves distinct article options as a list of ArticleOptionDto.
	/// </summary>
	/// <returns>An ActionResult containing a list of ArticleOptionDto with the distinct article options. Returns 200 (OK) on
	/// success.</returns>
	[HttpGet("article-options")]
	[ProducesResponseType(typeof(List<ArticleOptionDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<List<ArticleOptionDto>>> GetArticleOptions()
	{
		var result = await _service.GetDistinctArticleOptionsAsync();
		return Ok(result);
	}
}