using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class RejectRequestDto
{
	[Required(ErrorMessage = "A rejection reason is required.")]
	[StringLength(500)]
	public string Reason { get; set; } = string.Empty;

	[StringLength(50)]
	public string ReviewedBy { get; set; } = string.Empty;
}
