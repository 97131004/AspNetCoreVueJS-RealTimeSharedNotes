using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using AspNetCore_RealTimeSharedNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore_RealTimeSharedNotes.Data;

public class NotesRepository : BaseRepository, INotesRepository
{
    public NotesRepository(ApplicationDbContext db) : base(db) { }

    public async Task<List<Note>> GetAllNotesAsync()
    {
        return await _db.Notes
            .AsNoTracking() //disables change tracking for better performance on read-only query
            .Include(n => n.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteAsync(int noteId)
    {
        return await _db.Notes
            .AsNoTracking()
            .Include(n => n.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(n => n.NoteId == noteId);
    }

    public async Task<Note> AddNoteAsync(Note note)
    {
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return (await GetNoteAsync(note.NoteId))!; //required to retrieve user's data + roles and return dto
    }

    public async Task<bool> DeleteNoteAsync(int noteId)
    {
        var deleted = await _db.Notes
            .Where(n => n.NoteId == noteId)
            .ExecuteDeleteAsync(); //atomic deletion, works with race conditions, thus always succeeds
        return deleted > 0;
    }
}

