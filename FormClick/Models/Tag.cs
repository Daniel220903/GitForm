namespace FormClick.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string TagName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Template Template { get; set; }
    }

}
