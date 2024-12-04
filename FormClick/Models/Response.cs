namespace FormClick.Models
{
    public class Response
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int UserId { get; set; }
        public float Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Template Template { get; set; }
        public User User { get; set; }
    }

}
