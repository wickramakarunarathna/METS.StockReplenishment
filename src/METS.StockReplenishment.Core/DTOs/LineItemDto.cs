using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class LineItemDto
{
	public int Id { get; set; }

	[Required]
	[StringLength(50)]
	public string ArticleNumber { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(0.0000001, double.MaxValue)]
	public decimal RequestedQuantity { get; set; }

	[Range(0, double.MaxValue)]
	public decimal? FulfilledQuantity { get; set; }
}
