using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;

[ApiController]
[Authorize]
[Route("api/users/me")]
public class UsersController(AirlineDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Me() =>
        await db.Users.FindAsync(ApiView.UserId(User)) is { } u
            ? Ok(ApiView.User(u))
            : NotFound();

    [HttpGet("saved-airlines")]
    public async Task<IActionResult> Saved()
    {
        var uid = ApiView.UserId(User);

        return Ok(await db.SavedAirlines
            .Where(s => s.UserId == uid)
            .Select(s => new
            {
                s.AirlineId,
                s.Airline!.Name,
                s.Airline.IataCode,
                s.Airline.Country,
                s.Airline.Statistics!.OverallScore
            })
            .ToListAsync());
    }

    [HttpPost("saved-airlines")]
    public async Task<IActionResult> Save(SaveAirlineDto x)
    {
        var uid = ApiView.UserId(User);

        if (!await db.Airlines.AnyAsync(a => a.Id == x.AirlineId))
            return NotFound(new { message = "Airline not found" });

        if (!await db.SavedAirlines.AnyAsync(s =>
            s.UserId == uid && s.AirlineId == x.AirlineId))
        {
            db.SavedAirlines.Add(new()
            {
                UserId = uid,
                AirlineId = x.AirlineId
            });

            await db.SaveChangesAsync();
        }

        return Created($"/api/airlines/{x.AirlineId}", new
        {
            x.AirlineId
        });
    }

    [HttpDelete("saved-airlines/{airlineId:guid}")]
    public async Task<IActionResult> Remove(Guid airlineId)
    {
        var s = await db.SavedAirlines.FindAsync(
            ApiView.UserId(User),
            airlineId);

        if (s is null)
            return NotFound();

        db.Remove(s);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

//Changed