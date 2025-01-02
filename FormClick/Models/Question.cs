namespace FormClick.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string QuestionType { get; set; }
        public string Text { get; set; }
        public string? openAnswer {  get; set; }
        public bool IsVisibleInResults { get; set; }
        public int templateVersion { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Template Template { get; set; }
        public ICollection<QuestionOption> Options { get; set; }
        public ICollection<Answer> Answers { get; set; }
    }

}
