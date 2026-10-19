namespace METS.StockReplenishment.Core.Enums;

public enum StockValidationStatus
{
	NotChecked = 0,
	Pending = 1,
	InProgress = 2,
	Available = 3,
	PartiallyAvailable = 4,
	Unavailable = 5
}