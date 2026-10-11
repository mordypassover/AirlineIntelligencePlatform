using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin")]
public class AdminController(AirlineDbContext db) : ControllerBase
{
    [HttpGet("airlines")]
    public async Task<IActionResult> Airlines()
    {
        var rows = await db.Airlines
            .Include(a => a.Statistics)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return Ok(rows.Select(ApiView.Airline));
    }

    [HttpPost("airlines")]
    public async Task<IActionResult> Add(AirlineDto x)
    {
        var code = x.IataCode.Trim().ToUpperInvariant();

        if (await db.Airlines.AnyAsync(a => a.IataCode == code))
            return Conflict(new { message = "IATA code already exists" });

        var a = new Airline
        {
            Name = x.Name.Trim(),
            IataCode = code,
            Country = x.Country.Trim(),
            IsActive = x.IsActive
        };

        db.Add(a);
        db.Add(new AirlineStatistic { AirlineId = a.Id });

        await db.SaveChangesAsync();

        return Created($"/api/airlines/{a.Id}", ApiView.Airline(a));
    }

    [HttpPut("airlines/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AirlineDto x)
    {
        var a = await db.Airlines
            .Include(a => a.Statistics)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (a is null)
            return NotFound();

        a.Name = x.Name.Trim();
        a.IataCode = x.IataCode.Trim().ToUpperInvariant();
        a.Country = x.Country.Trim();
        a.IsActive = x.IsActive;

        await db.SaveChangesAsync();

        return Ok(ApiView.Airline(a));
    }

    [HttpDelete("airlines/{id:guid}")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var a = await db.Airlines.FindAsync(id);

        if (a is null)
            return NotFound();

        a.IsActive = false;

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("airlines/{id:guid}/notes")]
    public Task<List<object>> Notes(Guid id) =>
        db.AdminNotes
            .Where(n => n.AirlineId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => (object)new
            {
                n.Id,
                n.Content,
                n.CreatedAt,
                n.UpdatedAt,
                createdBy = n.User!.DisplayName
            })
            .ToListAsync();

    [HttpPost("airlines/{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, NoteDto x)
    {
        if (!await db.Airlines.AnyAsync(a => a.Id == id))
            return NotFound();

        if (string.IsNullOrWhiteSpace(x.Content))
            return BadRequest(new { message = "Note content is required" });

        var n = new AdminNote
        {
            AirlineId = id,
            UserId = ApiView.UserId(User),
            Content = x.Content.Trim()
        };

        db.Add(n);

        await db.SaveChangesAsync();

        return Created($"/api/admin/notes/{n.Id}", n);
    }

    [HttpPut("notes/{id:guid}")]
    public async Task<IActionResult> UpdateNote(Guid id, NoteDto x)
    {
        var n = await db.AdminNotes.FindAsync(id);

        if (n is null)
            return NotFound();

        n.Content = x.Content.Trim();
        n.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(n);
    }

    [HttpDelete("notes/{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id)
    {
        var n = await db.AdminNotes.FindAsync(id);

        if (n is null)
            return NotFound();

        db.Remove(n);

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Ok(new
        {
            totalAirlines = await db.Airlines.CountAsync(),
            totalReviews = await db.Reviews.CountAsync(),
            totalUsers = await db.Users.CountAsync(),
            totalFlights = await db.Flights.CountAsync(),
            lastDataCollection = await db.RawData.MaxAsync(
                x => (DateTime?)x.ReceivedAt),
            pipelineErrors = await db.RawData.CountAsync(
                x => x.Error != null)
        });
}