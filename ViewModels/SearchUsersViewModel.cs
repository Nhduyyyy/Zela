using System.Collections.Generic;

namespace Zela.ViewModels
{
    public class SearchUsersViewModel
    {
        public string SearchTerm { get; set; }
        public int? GroupId { get; set; }
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
    }
} 