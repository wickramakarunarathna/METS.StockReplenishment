using System.Linq;
using METS.StockReplenishment.Core.DTOs;
using METS.StockReplenishment.Core.Entities;
using METS.StockReplenishment.Core.Enums;
using METS.StockReplenishment.Core.Exceptions;
using METS.StockReplenishment.Core.Interfaces;
using METS.StockReplenishment.Infrastructure.Services;
using NSubstitute;
using NUnit.Framework;

namespace METS.StockReplenishment.Tests.Services;

[TestFixture]
public class ReplenishmentRequestServiceTests
{
	private IReplenishmentRequestRepository _repository = null!;
	private IStockValidationQueue _validationQueue = null!;
	private ReplenishmentRequestService _service = null!;

	[SetUp]
	public void SetUp()
	{
		_repository = Substitute.For<IReplenishmentRequestRepository>();
		_validationQueue = Substitute.For<IStockValidationQueue>();
		_service = new ReplenishmentRequestService(_repository, _validationQueue);
	}

	#region Helper Methods

	private static ReplenishmentRequest CreateTestRequest(
		int id = 1,
		string requestNumber = "REQ-2026-0001",
		RequestStatus status = RequestStatus.Draft,
		RequestPriority priority = RequestPriority.Normal,
		string stockLocationCode = "LOC-01",
		string stockLocationName = "Main Warehouse",
		string requestedBy = "John Doe",
		List<ReplenishmentLineItem>? lineItems = null)
	{
		var request = new ReplenishmentRequest
		{
			Id = id,
			RequestNumber = requestNumber,
			Status = status,
			Priority = priority,
			StockLocationCode = stockLocationCode,
			StockLocationName = stockLocationName,
			RequestedBy = requestedBy,
			RequestedAt = DateTime.UtcNow.AddHours(-1),
			StockValidationStatus = StockValidationStatus.NotChecked,
			CreatedAt = DateTime.UtcNow.AddHours(-1),
			UpdatedAt = DateTime.UtcNow.AddHours(-1),
			LineItems = lineItems ?? new List<ReplenishmentLineItem>
			{
				new()
				{
					Id = 10,
					ReplenishmentRequestId = id,
					ArticleNumber = "ART-100",
					Description = "Widget A",
					RequestedQuantity = 50m,
					FulfilledQuantity = null
				}
			}
		};

		foreach (var item in request.LineItems)
		{
			item.ReplenishmentRequest = request;
		}

		return request;
	}

	#endregion

	#region CreateAsync Tests

	[Test]
	public async Task CreateAsync_ShouldCreateDraftRequest()
	{
		// Arrange
		const string generatedNumber = "REQ-2026-0001";
		_repository.GenerateRequestNumberAsync().Returns(Task.FromResult(generatedNumber));
		_repository.AddAsync(Arg.Any<ReplenishmentRequest>())
			.Returns(ci =>
			{
				var req = ci.Arg<ReplenishmentRequest>();
				req.Id = 1;
				return Task.FromResult(req);
			});

		var createDto = new CreateReplenishmentRequestDto
		{
			StockLocationCode = "LOC-01",
			StockLocationName = "Central Hub",
			RequestedBy = "Alice Smith",
			Priority = RequestPriority.Urgent,
			LineItems = new List<CreateLineItemDto>
			{
				new()
				{
					ArticleNumber = "ART-001",
					Description = "Bolts",
					RequestedQuantity = 100m
				}
			}
		};

		// Act
		var result = await _service.CreateAsync(createDto);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.Id, Is.EqualTo(1));
		Assert.That(result.RequestNumber, Is.EqualTo(generatedNumber));
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Draft.ToString()));
		Assert.That(result.Priority, Is.EqualTo(RequestPriority.Urgent.ToString()));
		Assert.That(result.StockLocationCode, Is.EqualTo("LOC-01"));
		Assert.That(result.StockLocationName, Is.EqualTo("Central Hub"));
		Assert.That(result.RequestedBy, Is.EqualTo("Alice Smith"));
		Assert.That(result.StockValidationStatus, Is.EqualTo(StockValidationStatus.NotChecked.ToString()));
		Assert.That(result.LineItems, Has.Count.EqualTo(1));
		Assert.That(result.LineItems.First().ArticleNumber, Is.EqualTo("ART-001"));
		Assert.That(result.LineItems.First().RequestedQuantity, Is.EqualTo(100m));

		await _repository.Received(1).AddAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Status == RequestStatus.Draft &&
			r.StockValidationStatus == StockValidationStatus.NotChecked &&
			r.RequestNumber == generatedNumber &&
			r.LineItems.Count == 1));
	}

	[Test]
	public async Task CreateAsync_ShouldGenerateRequestNumber()
	{
		// Arrange
		const string expectedNumber = "REQ-2026-9999";
		_repository.GenerateRequestNumberAsync().Returns(Task.FromResult(expectedNumber));
		_repository.AddAsync(Arg.Any<ReplenishmentRequest>())
			.Returns(ci =>
			{
				var req = ci.Arg<ReplenishmentRequest>();
				req.Id = 42;
				return Task.FromResult(req);
			});

		var createDto = new CreateReplenishmentRequestDto
		{
			StockLocationCode = "LOC-02",
			StockLocationName = "Regional Branch",
			RequestedBy = "Bob Jones",
			Priority = RequestPriority.Normal,
			LineItems = new List<CreateLineItemDto>
			{
				new()
				{
					ArticleNumber = "ART-002",
					Description = "Nuts",
					RequestedQuantity = 50m
				}
			}
		};

		// Act
		var result = await _service.CreateAsync(createDto);

		// Assert
		await _repository.Received(1).GenerateRequestNumberAsync();
		Assert.That(result.RequestNumber, Is.EqualTo(expectedNumber));
	}

	#endregion

	#region SubmitAsync Tests

	[Test]
	public async Task SubmitAsync_WhenDraft_ShouldChangeStatusToSubmitted()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Draft);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);
		_validationQueue.QueueValidationAsync(requestId).Returns(ValueTask.CompletedTask);

		// Act
		var result = await _service.SubmitAsync(requestId);

		// Assert
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Submitted.ToString()));
		Assert.That(result.SubmittedAt, Is.Not.Null);
		Assert.That(result.StockValidationStatus, Is.EqualTo(StockValidationStatus.Pending.ToString()));
		Assert.That(result.StockValidationMessage, Is.EqualTo("Stock validation queued..."));

		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Id == requestId &&
			r.Status == RequestStatus.Submitted &&
			r.StockValidationStatus == StockValidationStatus.Pending &&
			r.SubmittedAt != null));
	}

	[Test]
	public async Task SubmitAsync_WhenDraft_ShouldQueueStockValidation()
	{
		// Arrange
		const int requestId = 5;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Draft);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);
		_validationQueue.QueueValidationAsync(requestId).Returns(ValueTask.CompletedTask);

		// Act
		await _service.SubmitAsync(requestId);

		// Assert
		await _validationQueue.Received(1).QueueValidationAsync(requestId);
	}

	[Test]
	[TestCase(RequestStatus.Submitted)]
	[TestCase(RequestStatus.Approved)]
	[TestCase(RequestStatus.Rejected)]
	[TestCase(RequestStatus.Fulfilled)]
	public async Task SubmitAsync_WhenNotDraft_ShouldThrowBusinessRuleException(RequestStatus nonDraftStatus)
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: nonDraftStatus);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitAsync(requestId));
		Assert.That(ex!.Message, Does.Contain("Only draft requests can be submitted."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
		await _validationQueue.DidNotReceive().QueueValidationAsync(Arg.Any<int>());
	}

	[Test]
	public async Task SubmitAsync_WhenNoLineItems_ShouldThrowBusinessRuleException()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(
			id: requestId,
			status: RequestStatus.Draft,
			lineItems: new List<ReplenishmentLineItem>());

		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitAsync(requestId));
		Assert.That(ex!.Message, Does.Contain("Cannot submit a request with no line items."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
		await _validationQueue.DidNotReceive().QueueValidationAsync(Arg.Any<int>());
	}

	[Test]
	public void SubmitAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.SubmitAsync(nonExistentId));
	}

	#endregion

	#region ApproveAsync Tests

	[Test]
	public async Task ApproveAsync_WhenSubmitted_ShouldChangeStatusToApproved()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Submitted);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);

		var approveDto = new ApproveRequestDto
		{
			ReviewedBy = "Manager Dan"
		};

		// Act
		var result = await _service.ApproveAsync(requestId, approveDto);

		// Assert
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Approved.ToString()));
		Assert.That(result.ReviewedBy, Is.EqualTo("Manager Dan"));
		Assert.That(result.ReviewedAt, Is.Not.Null);

		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Id == requestId &&
			r.Status == RequestStatus.Approved &&
			r.ReviewedBy == "Manager Dan" &&
			r.ReviewedAt != null));
	}

	[Test]
	[TestCase(RequestStatus.Draft)]
	[TestCase(RequestStatus.Approved)]
	[TestCase(RequestStatus.Rejected)]
	[TestCase(RequestStatus.Fulfilled)]
	public async Task ApproveAsync_WhenNotSubmitted_ShouldThrowBusinessRuleException(RequestStatus nonSubmittedStatus)
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: nonSubmittedStatus);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var approveDto = new ApproveRequestDto { ReviewedBy = "Manager Dan" };

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.ApproveAsync(requestId, approveDto));
		Assert.That(ex!.Message, Does.Contain("Only submitted requests can be approved."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
	}

	[Test]
	public void ApproveAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		var approveDto = new ApproveRequestDto { ReviewedBy = "Manager Dan" };

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveAsync(nonExistentId, approveDto));
	}

	#endregion

	#region RejectAsync Tests

	[Test]
	public async Task RejectAsync_WhenSubmitted_ShouldChangeStatusToRejected()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Submitted);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);

		var rejectDto = new RejectRequestDto
		{
			ReviewedBy = "Approver Sarah",
			Reason = "Items currently discontinued"
		};

		// Act
		var result = await _service.RejectAsync(requestId, rejectDto);

		// Assert
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Rejected.ToString()));
		Assert.That(result.ReviewedBy, Is.EqualTo("Approver Sarah"));
		Assert.That(result.ReviewedAt, Is.Not.Null);
		Assert.That(result.RejectionReason, Is.EqualTo("Items currently discontinued"));

		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Id == requestId &&
			r.Status == RequestStatus.Rejected &&
			r.ReviewedBy == "Approver Sarah" &&
			r.RejectionReason == "Items currently discontinued" &&
			r.ReviewedAt != null));
	}

	[Test]
	public async Task RejectAsync_ShouldSetRejectionReason()
	{
		// Arrange
		const int requestId = 2;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Submitted);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);

		const string expectedReason = "Budget quota exceeded for Q4.";
		var rejectDto = new RejectRequestDto
		{
			ReviewedBy = "Finance Team",
			Reason = expectedReason
		};

		// Act
		var result = await _service.RejectAsync(requestId, rejectDto);

		// Assert
		Assert.That(result.RejectionReason, Is.EqualTo(expectedReason));
		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r => r.RejectionReason == expectedReason));
	}

	[Test]
	[TestCase(RequestStatus.Draft)]
	[TestCase(RequestStatus.Approved)]
	[TestCase(RequestStatus.Rejected)]
	[TestCase(RequestStatus.Fulfilled)]
	public async Task RejectAsync_WhenNotSubmitted_ShouldThrowBusinessRuleException(RequestStatus nonSubmittedStatus)
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: nonSubmittedStatus);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var rejectDto = new RejectRequestDto
		{
			ReviewedBy = "Manager Dan",
			Reason = "Duplicate order"
		};

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.RejectAsync(requestId, rejectDto));
		Assert.That(ex!.Message, Does.Contain("Only submitted requests can be rejected."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
	}

	[Test]
	public void RejectAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		var rejectDto = new RejectRequestDto { ReviewedBy = "Manager Dan", Reason = "Duplicate" };

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.RejectAsync(nonExistentId, rejectDto));
	}

	#endregion

	#region FulfillAsync Tests

	[Test]
	public async Task FulfillAsync_WhenApproved_ShouldChangeStatusToFulfilled()
	{
		// Arrange
		const int requestId = 1;
		var lineItem = new ReplenishmentLineItem
		{
			Id = 10,
			ReplenishmentRequestId = requestId,
			ArticleNumber = "ART-100",
			Description = "Bearing Kit",
			RequestedQuantity = 50m,
			FulfilledQuantity = null
		};
		var existingRequest = CreateTestRequest(
			id: requestId,
			status: RequestStatus.Approved,
			lineItems: new List<ReplenishmentLineItem> { lineItem });

		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);

		var fulfillDto = new FulfillRequestDto
		{
			Items = new List<FulfillLineItemDto>
			{
				new() { LineItemId = 10, FulfilledQuantity = 45m }
			}
		};

		// Act
		var result = await _service.FulfillAsync(requestId, fulfillDto);

		// Assert
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Fulfilled.ToString()));
		Assert.That(result.FulfilledAt, Is.Not.Null);
		Assert.That(result.LineItems, Has.Count.EqualTo(1));
		Assert.That(result.LineItems.First().FulfilledQuantity, Is.EqualTo(45m));

		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Id == requestId &&
			r.Status == RequestStatus.Fulfilled &&
			r.FulfilledAt != null &&
			r.LineItems.First(li => li.Id == 10).FulfilledQuantity == 45m));
	}

	[Test]
	[TestCase(RequestStatus.Draft)]
	[TestCase(RequestStatus.Submitted)]
	[TestCase(RequestStatus.Rejected)]
	[TestCase(RequestStatus.Fulfilled)]
	public async Task FulfillAsync_WhenNotApproved_ShouldThrowBusinessRuleException(RequestStatus nonApprovedStatus)
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: nonApprovedStatus);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var fulfillDto = new FulfillRequestDto
		{
			Items = new List<FulfillLineItemDto>
			{
				new() { LineItemId = 10, FulfilledQuantity = 10m }
			}
		};

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.FulfillAsync(requestId, fulfillDto));
		Assert.That(ex!.Message, Does.Contain("Only approved requests can be fulfilled."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
	}

	[Test]
	public void FulfillAsync_WhenQuantityExceedsRequested_ShouldThrow()
	{
		// Arrange
		const int requestId = 1;
		var lineItem = new ReplenishmentLineItem
		{
			Id = 10,
			ReplenishmentRequestId = requestId,
			ArticleNumber = "ART-100",
			Description = "Bearing Kit",
			RequestedQuantity = 50m,
			FulfilledQuantity = null
		};
		var existingRequest = CreateTestRequest(
			id: requestId,
			status: RequestStatus.Approved,
			lineItems: new List<ReplenishmentLineItem> { lineItem });

		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var fulfillDto = new FulfillRequestDto
		{
			Items = new List<FulfillLineItemDto>
			{
				new() { LineItemId = 10, FulfilledQuantity = 55m } // 55 > 50
            }
		};

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.FulfillAsync(requestId, fulfillDto));
		Assert.That(ex!.Message, Does.Contain("Fulfilled quantity (55) cannot exceed requested quantity (50)"));
	}

	[Test]
	public void FulfillAsync_WhenLineItemNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Approved);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var fulfillDto = new FulfillRequestDto
		{
			Items = new List<FulfillLineItemDto>
			{
				new() { LineItemId = 9999, FulfilledQuantity = 10m }
			}
		};

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.FulfillAsync(requestId, fulfillDto));
	}

	#endregion

	#region UpdateAsync Tests

	[Test]
	public async Task UpdateAsync_WhenDraft_ShouldUpdateFieldsAndLineItems()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: RequestStatus.Draft);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));
		_repository.UpdateAsync(Arg.Any<ReplenishmentRequest>()).Returns(Task.CompletedTask);

		var updateDto = new UpdateReplenishmentRequestDto
		{
			StockLocationCode = "LOC-NEW",
			StockLocationName = "Updated Warehouse",
			Priority = RequestPriority.Urgent,
			LineItems = new List<CreateLineItemDto>
			{
				new()
				{
					ArticleNumber = "ART-200",
					Description = "New Valve",
					RequestedQuantity = 25m
				}
			}
		};

		// Act
		var result = await _service.UpdateAsync(requestId, updateDto);

		// Assert
		Assert.That(result.StockLocationCode, Is.EqualTo("LOC-NEW"));
		Assert.That(result.StockLocationName, Is.EqualTo("Updated Warehouse"));
		Assert.That(result.Priority, Is.EqualTo(RequestPriority.Urgent.ToString()));
		Assert.That(result.LineItems, Has.Count.EqualTo(1));
		Assert.That(result.LineItems.First().ArticleNumber, Is.EqualTo("ART-200"));
		Assert.That(result.LineItems.First().RequestedQuantity, Is.EqualTo(25m));

		await _repository.Received(1).UpdateAsync(Arg.Is<ReplenishmentRequest>(r =>
			r.Id == requestId &&
			r.StockLocationCode == "LOC-NEW" &&
			r.StockLocationName == "Updated Warehouse" &&
			r.Priority == RequestPriority.Urgent &&
			r.LineItems.Count == 1 &&
			r.LineItems.First().ArticleNumber == "ART-200"));
	}

	[Test]
	[TestCase(RequestStatus.Submitted)]
	[TestCase(RequestStatus.Approved)]
	[TestCase(RequestStatus.Rejected)]
	[TestCase(RequestStatus.Fulfilled)]
	public async Task UpdateAsync_WhenNotDraft_ShouldThrowBusinessRuleException(RequestStatus nonDraftStatus)
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId, status: nonDraftStatus);
		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		var updateDto = new UpdateReplenishmentRequestDto
		{
			StockLocationCode = "LOC-NEW",
			StockLocationName = "Updated Warehouse",
			Priority = RequestPriority.Urgent,
			LineItems = new List<CreateLineItemDto>()
		};

		// Act & Assert
		var ex = Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(requestId, updateDto));
		Assert.That(ex!.Message, Does.Contain("Only draft requests can be updated."));

		await _repository.DidNotReceive().UpdateAsync(Arg.Any<ReplenishmentRequest>());
	}

	[Test]
	public void UpdateAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		var updateDto = new UpdateReplenishmentRequestDto
		{
			StockLocationCode = "LOC-NEW",
			StockLocationName = "Updated Warehouse",
			Priority = RequestPriority.Normal
		};

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(nonExistentId, updateDto));
	}

	#endregion

	#region GetByIdAsync Tests

	[Test]
	public async Task GetByIdAsync_WhenFound_ShouldReturnMappedDto()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(
			id: requestId,
			requestNumber: "REQ-100",
			status: RequestStatus.Draft,
			priority: RequestPriority.Normal);

		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		// Act
		var result = await _service.GetByIdAsync(requestId);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.Id, Is.EqualTo(requestId));
		Assert.That(result.RequestNumber, Is.EqualTo("REQ-100"));
		Assert.That(result.Status, Is.EqualTo(RequestStatus.Draft.ToString()));
		Assert.That(result.Priority, Is.EqualTo(RequestPriority.Normal.ToString()));
		Assert.That(result.StockLocationCode, Is.EqualTo(existingRequest.StockLocationCode));
		Assert.That(result.StockLocationName, Is.EqualTo(existingRequest.StockLocationName));
		Assert.That(result.RequestedBy, Is.EqualTo(existingRequest.RequestedBy));
		Assert.That(result.LineItems, Has.Count.EqualTo(1));
	}

	[Test]
	public void GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(nonExistentId));
	}

	#endregion

	#region GetStockValidationStatusAsync & GetDistinctLocationsAsync & GetFilteredAsync Tests

	[Test]
	public async Task GetStockValidationStatusAsync_WhenFound_ShouldReturnStatus()
	{
		// Arrange
		const int requestId = 1;
		var existingRequest = CreateTestRequest(id: requestId);
		existingRequest.StockValidationStatus = StockValidationStatus.Available;
		existingRequest.StockValidationMessage = "All stock available.";

		_repository.GetByIdAsync(requestId).Returns(Task.FromResult<ReplenishmentRequest?>(existingRequest));

		// Act
		var result = await _service.GetStockValidationStatusAsync(requestId);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.RequestId, Is.EqualTo(requestId));
		Assert.That(result.Status, Is.EqualTo(StockValidationStatus.Available.ToString()));
		Assert.That(result.Message, Is.EqualTo("All stock available."));
	}

	[Test]
	public void GetStockValidationStatusAsync_WhenNotFound_ShouldThrowNotFoundException()
	{
		// Arrange
		const int nonExistentId = 999;
		_repository.GetByIdAsync(nonExistentId).Returns(Task.FromResult<ReplenishmentRequest?>(null));

		// Act & Assert
		Assert.ThrowsAsync<NotFoundException>(() => _service.GetStockValidationStatusAsync(nonExistentId));
	}

	[Test]
	public async Task GetDistinctLocationsAsync_ShouldReturnLocationsFromRepository()
	{
		// Arrange
		var locations = new List<string> { "LOC-01", "LOC-02", "LOC-03" };
		_repository.GetDistinctLocationsAsync().Returns(Task.FromResult(locations));

		// Act
		var result = await _service.GetDistinctLocationsAsync();

		// Assert
		Assert.That(result, Is.EquivalentTo(locations));
		await _repository.Received(1).GetDistinctLocationsAsync();
	}

	[Test]
	public async Task GetFilteredAsync_ShouldClampPaginationAndReturnMappedDto()
	{
		// Arrange
		var filter = new RequestFilterDto
		{
			Page = 0,        // Less than 1 -> should become 1
			PageSize = 100   // Greater than 50 -> should become 50
		};

		var request = CreateTestRequest(id: 1);
		var pagedResult = new PagedResultDto<ReplenishmentRequest>
		{
			Items = new List<ReplenishmentRequest> { request },
			TotalCount = 1,
			Page = 1,
			PageSize = 50
		};

		_repository.GetFilteredAsync(Arg.Any<RequestFilterDto>())
			.Returns(Task.FromResult(pagedResult));

		// Act
		var result = await _service.GetFilteredAsync(filter);

		// Assert
		Assert.That(filter.Page, Is.EqualTo(1));
		Assert.That(filter.PageSize, Is.EqualTo(50));
		Assert.That(result.TotalCount, Is.EqualTo(1));
		Assert.That(result.Items, Has.Count.EqualTo(1));
		Assert.That(result.Items.First().Id, Is.EqualTo(1));

		await _repository.Received(1).GetFilteredAsync(Arg.Is<RequestFilterDto>(f =>
			f.Page == 1 && f.PageSize == 50));
	}

	#endregion
}