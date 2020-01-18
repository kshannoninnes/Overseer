using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Overseer.Models
{
    public class EnforcedUserContext : DbContext
    {
        public EnforcedUserContext()
        {
            Database.EnsureCreated();
        }

        public DbSet<EnforcedUser> EnforcedUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=overseer.db");
    }

    public class EnforcedUser
    {
        [Key]
        public string Id { get; set; }
        [MaxLength(60)]
        public string Nickname { get; set; }
        [MaxLength(60)]
        public string EnforcedNickname { get; set; }
    }
}
