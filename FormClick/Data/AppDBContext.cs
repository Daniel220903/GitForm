using Microsoft.EntityFrameworkCore;
using FormClick.Models;

namespace FormClick.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext() { }
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<AdminAction> AdminActions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateHistorial> TemplateHistorials { get; set; }
        public DbSet<TemplateAccess> TemplateAccess { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Like> Likes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity => {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(500);
                entity.Property(u => u.Language).HasDefaultValue("esp");
                entity.Property(u => u.DarkMode).HasDefaultValue(false);
                entity.HasMany(u => u.Templates)
                      .WithOne(t => t.User)
                      .HasForeignKey(t => t.UserId);
                entity.HasMany(u => u.Comments)
                      .WithOne(c => c.User)
                      .HasForeignKey(c => c.UserId);
                entity.HasMany(u => u.Likes)
                      .WithOne(c => c.User)
                      .HasForeignKey(c => c.UserId);
            });

            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.User)
                      .WithMany(u => u.Templates)
                      .HasForeignKey(t => t.UserId);
                entity.HasMany(t => t.Questions)
                      .WithOne(q => q.Template)
                      .HasForeignKey(q => q.TemplateId);
                entity.HasMany(t => t.Comments)
                      .WithOne(c => c.Template)
                      .HasForeignKey(c => c.TemplateId);
                entity.HasMany(t => t.Likes)
                      .WithOne(c => c.Template)
                      .HasForeignKey(c => c.TemplateId);
                entity.HasMany(t => t.Tags)
                      .WithOne(tag => tag.Template)
                      .HasForeignKey(tag => tag.TemplateId);
                entity.HasMany(t => t.Responses)
                      .WithOne(r => r.Template)
                      .HasForeignKey(r => r.TemplateId);
                entity.HasMany(t => t.TemplateAccesses)
                      .WithOne(ta => ta.Template)
                      .HasForeignKey(ta => ta.TemplateId);
            });

            modelBuilder.Entity<TemplateHistorial>(entity => {
                entity.HasKey(t => t.Id);
                entity.HasOne(t => t.Template)
                      .WithMany(t => t.TemplateHistorials)
                      .HasForeignKey(t => t.TemplateId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<TemplateAccess>(entity =>
            {
                entity.HasKey(ta => ta.Id);
                entity.HasOne(ta => ta.Template).WithMany(t => t.TemplateAccesses).HasForeignKey(ta => ta.TemplateId);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.HasOne(q => q.Template)
                      .WithMany(t => t.Questions)
                      .HasForeignKey(q => q.TemplateId);
                entity.HasMany(q => q.Options)
                      .WithOne(o => o.Question)
                      .HasForeignKey(o => o.QuestionId);
                entity.HasMany(q => q.Answers)
                      .WithOne(a => a.Question)
                      .HasForeignKey(a => a.QuestionId);
                entity.Property(q => q.openAnswer)
                      .IsRequired(false);
            });


            modelBuilder.Entity<QuestionOption>(entity => {
                entity.HasKey(o => o.Id);
                entity.HasOne(o => o.Question)
                      .WithMany(q => q.Options)
                      .HasForeignKey(o => o.QuestionId);
            });

            modelBuilder.Entity<Answer>(entity => {
                entity.HasKey(a => a.Id);

                entity.HasOne(a => a.Question)
                      .WithMany(q => q.Answers)
                      .HasForeignKey(a => a.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Option)
                      .WithMany()
                      .HasForeignKey(a => a.OptionId)
                      .OnDelete(DeleteBehavior.Restrict); 
                entity.HasOne(a => a.Response)
                      .WithMany(r => r.Answers)
                      .HasForeignKey(a => a.ResponseId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<Response>(entity => {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.Template)
                      .WithMany(t => t.Responses)
                      .HasForeignKey(r => r.TemplateId)
                      .OnDelete(DeleteBehavior.Restrict); // Elimina la cascada
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Elimina la cascada
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Template)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(c => c.TemplateId)
                      .OnDelete(DeleteBehavior.Restrict); // Elimina la cascada

                entity.HasOne(c => c.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict); // Elimina la cascada
            });

            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Template)
                      .WithMany(t => t.Likes)
                      .HasForeignKey(c => c.TemplateId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Likes)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Tag>(entity => {
                entity.HasKey(tag => tag.Id);
                entity.HasOne(tag => tag.Template)
                      .WithMany(t => t.Tags)
                      .HasForeignKey(tag => tag.TemplateId);
            });

            modelBuilder.Entity<AdminAction>(entity => {
                entity.HasKey(a => a.Id);
                entity.HasOne(a => a.AdminUser)
                      .WithMany(u => u.AdminActions)
                      .HasForeignKey(a => a.AdminUserId);
                entity.HasOne(a => a.TargetUser)
                      .WithMany()
                      .HasForeignKey(a => a.TargetUserId);
            });
        }
    }
}
