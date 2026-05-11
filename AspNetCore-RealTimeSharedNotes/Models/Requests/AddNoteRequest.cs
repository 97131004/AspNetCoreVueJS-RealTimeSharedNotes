namespace AspNetCore_RealTimeSharedNotes.Models.Requests;

public record AddNoteRequest
{
    public string Content { get; set; } = string.Empty;
}
