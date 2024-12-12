namespace FormClick.Models
{
    public class TemplateAccess
    {
        public int Id { get; set; }
        public int TemplateId {get; set; }
        public int UserId {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        //RELACIONES
        public Template Template { get; set; }
    }
}
