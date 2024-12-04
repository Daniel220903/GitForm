namespace FormClick.Models
{
    public class AdminAction
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        public string ActionType { get; set; }
        public int? TargetUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public User AdminUser { get; set; }
        public User? TargetUser { get; set; }
    }

}
