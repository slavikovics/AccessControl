using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApp.Api.Data;
using SecureApp.Api.Dtos;
using SecureApp.Api.Models;

namespace SecureApp.Api.Controllers;

// Non-confidential data: stored in plain text, but still access-controlled to
// the owner and reached only via authenticated, parameterized EF Core queries
// (no string-built SQL, so this endpoint is not SQL-injectable).
[ApiController]
[Route("api/public")]
[Authorize]
public class PublicController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static RecordResponse ToResponse(PublicRecord r) =>
        new(r.Id, r.Title, r.Content, r.CreatedAtUtc, r.UpdatedAtUtc);

    [HttpGet]
    public async Task<ActionResult<List<RecordResponse>>> List([FromQuery] string? query)
    {
        var q = db.PublicRecords.Where(r => r.OwnerId == CurrentUserId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(r => r.Title.Contains(query) || r.Content.Contains(query));
        }

        var records = await q.OrderByDescending(r => r.UpdatedAtUtc).ToListAsync();
        return Ok(records.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecordResponse>> Get(int id)
    {
        var record = await db.PublicRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        return record is null ? NotFound() : Ok(ToResponse(record));
    }

    [HttpPost]
    public async Task<ActionResult<RecordResponse>> Create(RecordRequest request)
    {
        var record = new PublicRecord { OwnerId = CurrentUserId, Title = request.Title, Content = request.Content };
        db.PublicRecords.Add(record);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = record.Id }, ToResponse(record));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RecordResponse>> Update(int id, RecordRequest request)
    {
        var record = await db.PublicRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        if (record is null) return NotFound();

        record.Title = request.Title;
        record.Content = request.Content;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToResponse(record));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await db.PublicRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        if (record is null) return NotFound();

        db.PublicRecords.Remove(record);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
