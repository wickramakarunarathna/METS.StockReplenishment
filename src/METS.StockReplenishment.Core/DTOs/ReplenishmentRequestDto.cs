using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class ReplenishmentRequestDto
{
	public int Id { get; set; }

	[Required]
	[StringLength(50)]
	public string RequestNumber { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string Status { get; set; } = string.Empty;

	[Required]
	[StringLength(20)]
	public string Priority { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string StockLocationCode { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string StockLocationName { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string RequestedBy { get; set; } = string.Empty;

	[Required]
	public DateTime RequestedAt { get; set; }

	public DateTime? SubmittedAt { get; set; }

	[StringLength(100)]
	public string? ReviewedBy { get; set; }

	public DateTime? ReviewedAt { get; set; }

	[StringLength(500)]
	public string? RejectionReason { get; set; }

	public DateTime? FulfilledAt { get; set; }

	[Required]
	[StringLength(50)]
	public string StockValidationStatus { get; set; } = string.Empty;

	[StringLength(500)]
	public string? StockValidationMessage { get; set; }

	[Required]
	public DateTime CreatedAt { get; set; }

	[Required]
	public DateTime UpdatedAt { get; set; }

	[Required]
	[MinLength(1)]
	public List<LineItemDto> LineItems { get; set; } = new();
}