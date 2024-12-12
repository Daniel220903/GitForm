namespace FormClick.ViewModels
{
    public class TemplateViewModel
    {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool IsOwner { get; set; }
    }
}
