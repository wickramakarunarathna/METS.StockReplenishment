using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using METS.StockReplenishment.Core.Enums;

namespace METS.StockReplenishment.Core.Entities;

/// <summary>
/// Represents a stock replenishment request.
/// </summary>
public class ReplenishmentRequest
{
	/// <summary>
	/// Primary key.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Human-readable request identifier.
	/// </summary>
	[Required]
	[StringLength(50)]
	public string RequestNumber { get; set; } = string.Empty;

	/// <summary>
	/// Current status of the request.
	/// </summary>
	public RequestStatus Status { get; set; } = RequestStatus.Draft;

	/// <summary>
	/// Priority of the request.
	/// </summary>
	public RequestPriority Priority { get; set; } = RequestPriority.Normal;

	/// <summary>
	/// Stock location code.
	/// </summary>
	[Required]
	[StringLength(50)]
	public string StockLocationCode { get; set; } = string.Empty;

	/// <summary>
	/// Stock location name.
	/// </summary>
	[Required]
	[StringLength(100)]
	public string StockLocationName { get; set; } = string.Empty;

	/// <summary>
	/// User who requested the replenishment.
	/// </summary>
	[Required]
	[StringLength(100)]
	public string RequestedBy { get; set; } = string.Empty;

	/// <summary>
	/// When the request was created by the requester.
	/// </summary>
	[Required]
	public DateTime RequestedAt { get; set; }

	/// <summary>
	/// When the request was submitted for review (if applicable).
	/// </summary>
	public DateTime? SubmittedAt { get; set; }

	/// <summary>
	/// User who reviewed the request.
	/// </summary>
	[StringLength(100)]
	public string? ReviewedBy { get; set; }

	/// <summary>
	/// When the request was reviewed.
	/// </summary>
	public DateTime? ReviewedAt { get; set; }

	/// <summary>
	/// Optional rejection reason.
	/// </summary>
	[StringLength(500)]
	public string? RejectionReason { get; set; }

	/// <summary>
	/// When the request was fulfilled.
	/// </summary>
	public DateTime? FulfilledAt { get; set; }

	/// <summary>
	/// Result of stock validation.
	/// </summary>
	public StockValidationStatus StockValidationStatus { get; set; } = StockValidationStatus.NotChecked;

	/// <summary>
	/// Optional message from stock validation.
	/// </summary>
	[StringLength(500)]
	public string? StockValidationMessage { get; set; }

	/// <summary>
	/// Creation timestamp.
	/// </summary>
	[Required]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Last update timestamp.
	/// </summary>
	[Required]
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// Line items belonging to the request.
	/// Keep as <see cref="ICollection{T}"/> for ORM flexibility.
	/// </summary>
	[Required]
	public ICollection<ReplenishmentLineItem> LineItems { get; set; } = new List<ReplenishmentLineItem>();
}