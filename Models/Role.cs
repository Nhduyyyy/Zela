using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zela.Models;

/// <summary>
/// Role gán cho User (1 User có thể có nhiều Role).
/// </summary>
public class Role
{
    [Key]
    public int RoleId { get; set; }                // PK: RoleId INT IDENTITY(1,1)

    [Required]
    public int UserId { get; set; }                // FK -> User

    [MaxLength(250)]
    public string RoleName { get; set; }           // NVARCHAR(250)

    public DateTime CreateAt { get; set; }         // DATE

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }                 // Navigation về User
}