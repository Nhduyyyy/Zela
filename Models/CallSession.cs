using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Phiên gọi (CallSession) trong VideoRoom.
/// </summary>
public class CallSession
{
    [Key]
    public Guid SessionId { get; set; }            // PK: UNIQUEIDENTIFIER

    public int RoomId { get; set; }                // FK -> VideoRoom
    public DateTime StartedAt { get; set; }        // DATETIME2
    public DateTime? EndedAt { get; set; }         // DATETIME2 NULL

    [MaxLength(500)]
    public string RecordingUrl { get; set; }       // NVARCHAR(500) NULL

    [ForeignKey(nameof(RoomId))]
    public VideoRoom VideoRoom { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
    public ICollection<CallTranscript> Transcripts { get; set; }
    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
}