using METS.StockReplenishment.Core.Enums;

namespace METS.StockReplenishment.Core.DTOs;

public class RequestFilterDto
{
    public RequestStatus? Status { get; set; }
    public RequestPriority? Priority { get; set; }
    public string? StockLocationCode { get; set; }
    public string? ArticleNumber { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
