using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Chữ ký bản ghi (Transcript) của CallSession.
/// </summary>
public class CallTranscript
{
    [Key]
    public Guid TranscriptId { get; set; }         // PK: UNIQUEIDENTIFIER

    public Guid SessionId { get; set; }            // FK -> CallSession
    public DateTime GeneratedAt { get; set; }      // DATETIME2
    public string Content { get; set; }            // NVARCHAR(MAX)

    [ForeignKey(nameof(SessionId))]
    public CallSession CallSession { get; set; }
    public ICollection<Subtitle> Subtitles { get; set; }
}