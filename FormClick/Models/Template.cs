﻿using Azure;

namespace FormClick.Models
{
    public class Template
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Topic { get; set; }
        public string picture { get; set; }
        public bool Public { get; set; }
        public int Version { get; set; }
        public bool IsCurrent { get; set; } = false;
        public int OriginalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public ICollection<TemplateHistorial> TemplateHistorials { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Likes { get; set; }
        public ICollection<Tag> Tags { get; set; }
        public ICollection<Response> Responses { get; set; }
        public ICollection<TemplateAccess> TemplateAccesses { get; set; }
    }
}
