namespace FormClick.ViewModels
{
    public class TemplateViewModel {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string? ProfilePicture {get; set;}
        public string? picture { get; set; }
        public bool IsCurrent { get; set; }
        public string? Topic { get; set; }
        public bool IsOwner { get; set; }
        public bool? HasLiked { get; set; }
        public int? TotalLikes { get; set; }
    }
}
