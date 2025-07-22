using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zela.Enum;

namespace Zela.Models;

/// <summary>
/// Thông báo (notification) gửi đến User.
/// </summary>
public class Notification
{
    [Key]
    public int NotificationId { get; set; }        // PK: INT IDENTITY(1,1)

    public int ReceiverUserId { get; set; }       // FK -> User (receiver)
    public int SenderUserId { get; set; }         // FK -> User (sender)
    public string Content { get; set; }           // Notification content/preview
    public MessageType MessageType { get; set; }  // Enum: Text, Image, File
    public DateTime Timestamp { get; set; }       // When notification was created
    public bool IsRead { get; set; }              // Read status

    [ForeignKey(nameof(ReceiverUserId))]
    public User Receiver { get; set; }
    [ForeignKey(nameof(SenderUserId))]
    public User Sender { get; set; }
}