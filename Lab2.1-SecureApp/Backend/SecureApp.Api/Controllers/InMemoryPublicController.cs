using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApp.Api.Dtos;
using SecureApp.Api.Services;

namespace SecureApp.Api.Controllers;

// Lab 3.1: same shape and access-control rules as PublicController, but
// backed by InMemoryRecordStore instead of the SQLite-backed DbContext.
[ApiController]
[Route("api/memory/public")]
[Authorize]
public class InMemoryPublicController(InMemoryRecordStore store) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static RecordResponse ToResponse(InMemoryRecordStore.PublicEntry e) =>
        new(e.Id, e.Title, e.Content, e.CreatedAtUtc, e.UpdatedAtUtc);

    [HttpGet]
    public ActionResult<List<RecordResponse>> List([FromQuery] string? query)
    {
        var entries = store.ListPublic(CurrentUserId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            entries = entries.Where(e =>
                e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return Ok(entries.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<RecordResponse> Get(int id)
    {
        var entry = store.GetPublic(CurrentUserId, id);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpPost]
    public ActionResult<RecordResponse> Create(RecordRequest request)
    {
        var entry = store.CreatePublic(CurrentUserId, request.Title, request.Content);
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:int}")]
    public ActionResult<RecordResponse> Update(int id, RecordRequest request)
    {
        var entry = store.UpdatePublic(CurrentUserId, id, request.Title, request.Content);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        store.DeletePublic(CurrentUserId, id) ? NoContent() : NotFound();
}
