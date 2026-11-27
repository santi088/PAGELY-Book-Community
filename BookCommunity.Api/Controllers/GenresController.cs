using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Genres;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public GenresController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetGenres(
        [FromQuery] string? search,
        [FromQuery] string? sort = "name",
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var query = _context.Genres.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(searchLower) ||
                                     (g.Description != null && g.Description.ToLower().Contains(searchLower)));
        }

        query = sort?.ToLower() switch
        {
            "name" => query.OrderBy(g => g.Name),
            "namedesc" => query.OrderByDescending(g => g.Name),
            _ => query.OrderBy(g => g.Name)
        };

        if (page.HasValue && pageSize.HasValue)
        {
            var p = page.Value < 1 ? 1 : page.Value;
            var ps = pageSize.Value < 1 || pageSize.Value > 100 ? 20 : pageSize.Value;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)ps);
            var genres = await query
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(g => new GenreDto { Id = g.Id, Name = g.Name, Description = g.Description })
                .ToListAsync();

            return Ok(new PagedResult<GenreDto>
            {
                Items = genres,
                Page = p,
                PageSize = ps,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }

        var all = await query
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name, Description = g.Description })
            .ToListAsync();

        return Ok(all);
    }
}
