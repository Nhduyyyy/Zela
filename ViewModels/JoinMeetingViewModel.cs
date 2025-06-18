using System.ComponentModel.DataAnnotations;

namespace Zela.Models.ViewModels
{
    public class JoinMeetingViewModel
    {
        [Required]
        [StringLength(10, ErrorMessage = "Mã phòng tối đa 10 ký tự")]
        [Display(Name = "Mã phòng")]
        public string Password { get; set; }
    }
}