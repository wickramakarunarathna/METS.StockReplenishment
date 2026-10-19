using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class StockBalanceDto
{
	[StringLength(50, ErrorMessage = "Article number cannot exceed 50 characters.")]
	public string ArticleNumber { get; set; } = string.Empty;

	[StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
	public string Description { get; set; } = string.Empty;

	[Range(0, double.MaxValue)]
	public decimal CurrentBalance { get; set; }

	[Range(0, double.MaxValue)]
	public decimal LowStockThreshold { get; set; }

	[ScaffoldColumn(false)]
	public bool IsLowStock => CurrentBalance <= LowStockThreshold;
}