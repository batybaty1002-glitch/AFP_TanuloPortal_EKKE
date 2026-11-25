using Microsoft.EntityFrameworkCore;
using TanuloPortal.Models;

namespace TanuloPortal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TimetableItem> TimetableItems { get; set; }
    }

    
}