using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;
namespace ConsoleApp1
{
    public class SurveyContext : DbContext
    {
        public SurveyContext(DbContextOptions<SurveyContext> options) : base(options) { }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<ResponseDetail> ResponseDetails { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=DESKTOP-CPJ3TCE\\SQLEXPRESS;Initial Catalog=OnlineSurvey5;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Section>()
                .HasOne(s => s.ParentSection)
                .WithMany(p => p.SubSections)
                .HasForeignKey(s => s.ParentSectionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.ParentQuestion)
                .WithMany(p => p.SubQuestions)
                .HasForeignKey(q => q.ParentQuestionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Admin)
                .WithMany(a => a.Surveys)
                .HasForeignKey(s => s.AdminID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Section>()
                .HasOne(sec => sec.Survey)
                .WithMany(s => s.Sections)
                .HasForeignKey(sec => sec.SurveyID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Section)
                .WithMany(sec => sec.Questions)
                .HasForeignKey(q => q.SectionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Response>()
                .HasOne(r => r.User)
                .WithMany(u => u.Responses)
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Response>()
                .HasOne(r => r.Survey)
                .WithMany(s => s.Responses)
                .HasForeignKey(r => r.SurveyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResponseDetail>()
                .HasOne(rd => rd.Response)
                .WithMany(r => r.ResponseDetails)
                .HasForeignKey(rd => rd.ResponseID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResponseDetail>()
                .HasOne(rd => rd.Question)
                .WithMany(q => q.ResponseDetails)
                .HasForeignKey(rd => rd.QuestionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResponseDetail>()
                .HasOne(rd => rd.Option)
                .WithMany(o => o.ResponseDetails)
                .HasForeignKey(rd => rd.OptionID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
