using METS.StockReplenishment.Core.Enums;

namespace METS.StockReplenishment.Core.DTOs;

public class StockValidationResultDto
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}
