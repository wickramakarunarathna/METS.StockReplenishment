using System.ComponentModel.DataAnnotations;

namespace METS.StockReplenishment.Core.DTOs;

public class FulfillRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one fulfilled item is required.")]
    public List<FulfillLineItemDto> Items { get; set; } = new();
}

public class FulfillLineItemDto
{
    public int LineItemId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Fulfilled quantity cannot be negative.")]
    public decimal FulfilledQuantity { get; set; }
}
