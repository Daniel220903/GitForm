namespace FormClick.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string? Cellphone { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? ProfilePicture { get; set; }
        public bool Admin { get; set; }
        public bool Verified { get; set; }
        public string? VerifiedCode { get; set; }
        public bool Banned { get; set; }
        public DateTime? LastLogin { get; set; }
        public string Language { get; set; }
        public bool DarkMode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<Template> Templates { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<AdminAction> AdminActions { get; set; }
    }

}
