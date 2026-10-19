using METS.StockReplenishment.Core.DTOs;
using METS.StockReplenishment.Core.Entities;
using METS.StockReplenishment.Core.Interfaces;
using METS.StockReplenishment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace METS.StockReplenishment.Infrastructure.Repositories;

public class ReplenishmentRequestRepository : IReplenishmentRequestRepository
{
	private readonly AppDbContext _context;

	public ReplenishmentRequestRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<ReplenishmentRequest?> GetByIdAsync(int id)
	{
		return await _context.ReplenishmentRequests
			.Include(r => r.LineItems)
			.FirstOrDefaultAsync(r => r.Id == id);
	}

	public async Task<PagedResultDto<ReplenishmentRequest>> GetFilteredAsync(RequestFilterDto filter)
	{
		var query = _context.ReplenishmentRequests
			.Include(r => r.LineItems)
			.AsQueryable();

		if (filter.Status.HasValue)
			query = query.Where(r => r.Status == filter.Status.Value);

		if (filter.Priority.HasValue)
			query = query.Where(r => r.Priority == filter.Priority.Value);

		if (!string.IsNullOrWhiteSpace(filter.StockLocationCode))
			query = query.Where(r => r.StockLocationCode == filter.StockLocationCode);

		if (!string.IsNullOrWhiteSpace(filter.ArticleNumber))
			query = query.Where(r => r.LineItems.Any(li => li.ArticleNumber == filter.ArticleNumber));

		if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
		{
			var term = filter.SearchTerm.ToLower();
			query = query.Where(r =>
				r.RequestNumber.ToLower().Contains(term) ||
				r.RequestedBy.ToLower().Contains(term) ||
				r.StockLocationName.ToLower().Contains(term));
		}

		var totalCount = await query.CountAsync();

		var items = await query
			.OrderByDescending(r => r.Priority)
			.ThenByDescending(r => r.CreatedAt)
			.Skip((filter.Page - 1) * filter.PageSize)
			.Take(filter.PageSize)
			.ToListAsync();

		return new PagedResultDto<ReplenishmentRequest>
		{
			Items = items,
			TotalCount = totalCount,
			Page = filter.Page,
			PageSize = filter.PageSize
		};
	}

	public async Task<ReplenishmentRequest> AddAsync(ReplenishmentRequest request)
	{
		_context.ReplenishmentRequests.Add(request);
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task UpdateAsync(ReplenishmentRequest request)
	{
		_context.ReplenishmentRequests.Update(request);
		await _context.SaveChangesAsync();
	}

	public async Task<string> GenerateRequestNumberAsync()
	{
		var year = DateTime.UtcNow.Year;
		var count = await _context.ReplenishmentRequests
			.CountAsync(r => r.RequestNumber.StartsWith($"REQ-{year}"));
		return $"REQ-{year}-{(count + 1):D3}";
	}

	public async Task<List<string>> GetDistinctLocationsAsync()
	{
		return await _context.ReplenishmentRequests
			.Select(r => r.StockLocationCode)
			.Distinct()
			.OrderBy(l => l)
			.ToListAsync();
	}

	public async Task<List<StockLocationOptionDto>> GetDistinctLocationOptionsAsync()
	{
		return await _context.ReplenishmentRequests
			.GroupBy(r => new { r.StockLocationCode, r.StockLocationName })
			.Select(g => new StockLocationOptionDto
			{
				Code = g.Key.StockLocationCode,
				Name = g.Key.StockLocationName
			})
			.OrderBy(l => l.Code)
			.ToListAsync();
	}

	public async Task<List<ArticleOptionDto>> GetDistinctArticleOptionsAsync()
	{
		return await _context.ReplenishmentLineItems
			.GroupBy(li => new { li.ArticleNumber, li.Description })
			.Select(g => new ArticleOptionDto
			{
				ArticleNumber = g.Key.ArticleNumber,
				Description = g.Key.Description
			})
			.OrderBy(a => a.ArticleNumber)
			.ToListAsync();
	}

	public async Task<List<StockBalanceDto>> GetStockBalancesAsync()
	{
		const decimal defaultStockLevel = 500m;
		const decimal lowStockThreshold = 150m;

		var articleDemand = await _context.ReplenishmentLineItems
			.GroupBy(li => new { li.ArticleNumber, li.Description })
			.Select(g => new
			{
				g.Key.ArticleNumber,
				g.Key.Description,
				TotalRequested = g.Sum(x => x.RequestedQuantity),
				TotalFulfilled = g.Sum(x => x.FulfilledQuantity ?? 0m)
			})
			.OrderBy(x => x.ArticleNumber)
			.ToListAsync();

		return articleDemand
			.Select(x => new StockBalanceDto
			{
				ArticleNumber = x.ArticleNumber,
				Description = x.Description,
				CurrentBalance = Math.Max(0m, defaultStockLevel - (x.TotalRequested - x.TotalFulfilled)),
				LowStockThreshold = lowStockThreshold
			})
			.ToList();
	}
}