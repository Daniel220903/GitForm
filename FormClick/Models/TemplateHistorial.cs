using FormClick.Models;

public class TemplateHistorial
{
    public int Id { get; set; }
    public int OriginalId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Topic { get; set; }
    public string Picture { get; set; }
    public bool Public { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Relación directa con Template por medio del TemplateId
    public int TemplateId { get; set; }
    public Template Template { get; set; }

    // Relaciones adicionales
    public User User { get; set; }
    //public ICollection<Question> Questions { get; set; }
    //public ICollection<Tag> Tags { get; set; }
    //public ICollection<Response> Responses { get; set; }
    //public ICollection<TemplateAccess> TemplateAccesses { get; set; }
}
