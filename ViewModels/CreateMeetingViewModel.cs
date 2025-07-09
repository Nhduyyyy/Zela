// Sử dụng namespace chứa các attribute kiểm tra dữ liệu (validation) như [Required], [Display]
using System.ComponentModel.DataAnnotations;

namespace Zela.Models.ViewModels
{
    // ViewModel dùng để truyền dữ liệu giữa View và Controller khi tạo cuộc họp mới
    public class CreateMeetingViewModel
    {
        // Thuộc tính này bắt buộc phải có giá trị (không được bỏ trống) khi submit form
        [Required]
        // Đặt tên hiển thị cho trường này là "ID Người tạo" (dùng cho label trong view và thông báo lỗi)
        [Display(Name = "ID Người tạo")]
        public int CreatorId { get; set; } // ID của người tạo cuộc họp, sẽ được truyền từ controller xuống view và từ view lên controller khi submit
    }
}