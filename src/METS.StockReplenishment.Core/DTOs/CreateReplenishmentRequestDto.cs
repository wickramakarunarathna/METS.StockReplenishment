using METS.StockReplenishment.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class CreateReplenishmentRequestDto
{
	[Required]
	[StringLength(50, ErrorMessage = "Stock location code cannot exceed 50 characters.")]
	public string StockLocationCode { get; set; } = string.Empty;

	[Required]
	[StringLength(200, ErrorMessage = "Stock location name cannot exceed 200 characters.")]
	public string StockLocationName { get; set; } = string.Empty;

	[Required]
	[StringLength(50, ErrorMessage = "Requested by cannot exceed 50 characters.")]
	public string RequestedBy { get; set; } = string.Empty;

	public RequestPriority Priority { get; set; } = RequestPriority.Normal;

	[Required]
	[MinLength(1, ErrorMessage = "At least one line item is required.")]
	public List<CreateLineItemDto> LineItems { get; set; } = new();
}
