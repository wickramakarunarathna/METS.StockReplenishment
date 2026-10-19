using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace METS.StockReplenishment.Core.Entities;

/// <summary>
/// Represents a single line item within a replenishment request.
/// </summary>
public class ReplenishmentLineItem
{
	/// <summary>
	/// Primary key.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Foreign key to the parent request.
	/// </summary>
	[Required]
	[ForeignKey(nameof(ReplenishmentRequest))]
	public int ReplenishmentRequestId { get; set; }

	/// <summary>
	/// Article number or SKU.
	/// </summary>
	[Required]
	[StringLength(50)]
	public string ArticleNumber { get; set; } = string.Empty;

	/// <summary>
	/// Human-readable description of the item.
	/// </summary>
	[Required]
	[StringLength(500)]
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Quantity requested.
	/// </summary>
	[Required]
	[Range(0.0000001, double.MaxValue)]
	public decimal RequestedQuantity { get; set; }

	/// <summary>
	/// Quantity fulfilled (may be null if not fulfilled yet).
	/// </summary>
	[Range(0, double.MaxValue)]
	public decimal? FulfilledQuantity { get; set; }

	/// <summary>
	/// Navigation to the parent replenishment request.
	/// Keep non-virtual unless EF proxy lazy-loading is used.
	/// </summary>
	[Required]
	public ReplenishmentRequest ReplenishmentRequest { get; set; } = null!;
}