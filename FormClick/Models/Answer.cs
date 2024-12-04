namespace FormClick.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string? ResponseText { get; set; }
        public int? OptionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Question Question { get; set; }
        public QuestionOption? Option { get; set; }
    }

}
