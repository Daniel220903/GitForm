using FormClick.Models;

namespace FormClick.ViewModels{
    public class AdminVM{
        public List<User> Users { get; set; }

        public List<TemplateViewModel> Templates { get; set; }
    }
}
