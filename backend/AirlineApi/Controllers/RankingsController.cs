using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;

[ApiController]
[Route("api/rankings")]
public class RankingsController(AirlineDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? sort)
    {
        var q = db.Airlines
            .Where(a => a.IsActive)
            .Include(a => a.Statistics)
            .AsQueryable();

        q = sort switch
        {
            "price" => q.OrderByDescending(a => a.Statistics!.PriceScore),
            "service" => q.OrderByDescending(a => a.Statistics!.ServiceScore),
            "staff" => q.OrderByDescending(a => a.Statistics!.StaffScore),
            "environment" => q.OrderByDescending(a => a.Statistics!.EnvironmentalScore),
            "flights" => q.OrderByDescending(a => a.Statistics!.FlightsPerWeek),
            _ => q.OrderByDescending(a => a.Statistics!.OverallScore)
        };

        var rows = await q.ToListAsync();

        return Ok(rows.Select(ApiView.Airline));
    }
}