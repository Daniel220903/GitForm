namespace FormClick.ViewModels
{
    public class AnswerTemplateVM {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public int? Version { get; set; }
        public bool? IsCurrent { get; set; }
        public string UserName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? picture { get; set; }
        public string? Topic { get; set; }
        public int? TotalLikes { get; set; }
        public List<ResponsedBYVM> Responses { get; set; } // Usamos ResponsedBYVM aquí
    }

    public class ResponsedBYVM {
        public int ResponseId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string? ProfilePicture { get; set; }
        public float Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
