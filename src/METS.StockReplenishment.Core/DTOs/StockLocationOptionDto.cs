using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class StockLocationOptionDto
{
	[StringLength(50)]
	public string Code { get; set; } = string.Empty;

	[StringLength(100)]
	public string Name { get; set; } = string.Empty;
}