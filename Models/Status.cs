using System.ComponentModel.DataAnnotations;

namespace Zela.Models;

/// <summary>
/// Trạng thái friendship.
/// </summary>
public class Status
{
    [Key]
    public int StatusId { get; set; }              // PK: StatusId INT IDENTITY(1,1)

    [MaxLength(250)]
    public string StatuName { get; set; }          // NVARCHAR(250)

    public DateTime CreatedAt { get; set; }        // DATETIME

    [MaxLength(500)]
    public string Describe { get; set; }           // NVARCHAR(500)

    public ICollection<Friendship> Friendships { get; set; }
}