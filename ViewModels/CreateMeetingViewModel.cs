using System.ComponentModel.DataAnnotations;

namespace Zela.Models.ViewModels
{
    public class CreateMeetingViewModel
    {
        [Required]
        [Display(Name = "ID Người tạo")]
        public int CreatorId { get; set; }
    }
}