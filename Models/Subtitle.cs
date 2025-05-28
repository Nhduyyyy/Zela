using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Subtitle kèm transcript.
/// </summary>
public class Subtitle
{
    [Key]
    public int SubtitleId { get; set; }            // PK: INT IDENTITY(1,1)

    public Guid TranscriptId { get; set; }         // FK -> CallTranscript
    public decimal StartTime { get; set; }         // DECIMAL(10,3)
    public decimal EndTime { get; set; }           // DECIMAL(10,3)
    public string Text { get; set; }               // NVARCHAR(MAX)

    [ForeignKey(nameof(TranscriptId))]
    public CallTranscript CallTranscript { get; set; }
}
