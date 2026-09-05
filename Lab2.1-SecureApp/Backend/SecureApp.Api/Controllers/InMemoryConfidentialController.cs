using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApp.Api.Dtos;
using SecureApp.Api.Services;

namespace SecureApp.Api.Controllers;

// Lab 3.1: same shape and access-control rules as ConfidentialController, but
// backed by InMemoryRecordStore instead of the SQLite-backed DbContext --
// nothing here is ever written to disk.
[ApiController]
[Route("api/memory/confidential")]
[Authorize]
public class InMemoryConfidentialController(InMemoryRecordStore store) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private RecordResponse ToResponse(InMemoryRecordStore.ConfidentialEntry e) =>
        new(e.Id, store.DecryptTitle(e), store.DecryptContent(e), e.CreatedAtUtc, e.UpdatedAtUtc);

    [HttpGet]
    public ActionResult<List<RecordResponse>> List([FromQuery] string? query)
    {
        var decrypted = store.ListConfidential(CurrentUserId).Select(ToResponse);
        if (!string.IsNullOrWhiteSpace(query))
        {
            decrypted = decrypted.Where(r =>
                r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return Ok(decrypted.ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<RecordResponse> Get(int id)
    {
        var entry = store.GetConfidential(CurrentUserId, id);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpPost]
    public ActionResult<RecordResponse> Create(RecordRequest request)
    {
        var entry = store.CreateConfidential(CurrentUserId, request.Title, request.Content);
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:int}")]
    public ActionResult<RecordResponse> Update(int id, RecordRequest request)
    {
        var entry = store.UpdateConfidential(CurrentUserId, id, request.Title, request.Content);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        store.DeleteConfidential(CurrentUserId, id) ? NoContent() : NotFound();
}
