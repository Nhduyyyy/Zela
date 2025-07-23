namespace Zela.Enum;

public enum MessageStatus
{
    Sent = 0,               // Đã gửi
    Delivered = 1,          // Đã nhận (người nhận đã nhận được, nhưng chưa đọc)
    Seen = 2                // Đã xem
}