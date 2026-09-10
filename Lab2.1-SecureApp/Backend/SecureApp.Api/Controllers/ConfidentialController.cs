using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApp.Api.Data;
using SecureApp.Api.Dtos;
using SecureApp.Api.Models;
using SecureApp.Api.Services;

namespace SecureApp.Api.Controllers;

[ApiController]
[Route("api/confidential")]
[Authorize]
public class ConfidentialController(AppDbContext db, ConfidentialCrypto crypto) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private RecordResponse ToResponse(ConfidentialRecord r) =>
        new(r.Id, crypto.Decrypt(r.EncryptedTitle), crypto.Decrypt(r.EncryptedContent), r.CreatedAtUtc, r.UpdatedAtUtc);

    [HttpGet]
    public async Task<ActionResult<List<RecordResponse>>> List([FromQuery] string? query)
    {
        var records = await db.ConfidentialRecords
            .Where(r => r.OwnerId == CurrentUserId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync();

        var decrypted = records.Select(ToResponse);
        if (!string.IsNullOrWhiteSpace(query))
        {
            decrypted = decrypted.Where(r =>
                r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(decrypted.ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecordResponse>> Get(int id)
    {
        var record = await db.ConfidentialRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        return record is null ? NotFound() : Ok(ToResponse(record));
    }

    [HttpPost]
    public async Task<ActionResult<RecordResponse>> Create(RecordRequest request)
    {
        var record = new ConfidentialRecord
        {
            OwnerId = CurrentUserId,
            EncryptedTitle = crypto.Encrypt(request.Title),
            EncryptedContent = crypto.Encrypt(request.Content),
        };
        db.ConfidentialRecords.Add(record);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = record.Id }, ToResponse(record));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RecordResponse>> Update(int id, RecordRequest request)
    {
        var record = await db.ConfidentialRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        if (record is null) return NotFound();

        record.EncryptedTitle = crypto.Encrypt(request.Title);
        record.EncryptedContent = crypto.Encrypt(request.Content);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToResponse(record));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await db.ConfidentialRecords.SingleOrDefaultAsync(r => r.Id == id && r.OwnerId == CurrentUserId);
        if (record is null) return NotFound();

        db.ConfidentialRecords.Remove(record);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
