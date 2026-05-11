using System.Security.Claims;
using AspNetCore_RealTimeSharedNotes.Hubs;
using AspNetCore_RealTimeSharedNotes.Models.Requests;
using AspNetCore_RealTimeSharedNotes.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore_RealTimeSharedNotes.Controllers.Api;

[ApiController]
[Route("api/notes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotesApiController : ControllerBase
{
    private readonly INotesService _noteService;
    private readonly IUsersService _userService;
    private readonly IHubContext<NotesHub> _hub;

    public NotesApiController(INotesService noteService, IUsersService userService, IHubContext<NotesHub> hub)
    {
        _noteService = noteService;
        _userService = userService;
        _hub = hub;
    }

    [HttpGet("getnotes")]
    public async Task<IActionResult> GetNotes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var notes = await _noteService.GetAllNotesAsync(userId);
        return Ok(notes);
    }

    [HttpPost("addnote")]
    public async Task<IActionResult> AddNote([FromBody] AddNoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var note = await _noteService.AddNoteAsync(userId, role, request.Content);
        await _hub.Clients.All.SendAsync("NoteAdded", note);
        return Ok(note);
    }

    [HttpDelete("deletenote/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(int noteId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var deleted = await _noteService.DeleteNoteAsync(noteId, userId, role);
        if (!deleted)
            return NotFound();
        await _hub.Clients.All.SendAsync("NoteRemoved", noteId);
        return Ok();
    }
}

