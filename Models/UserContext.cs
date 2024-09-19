using Microsoft.EntityFrameworkCore;

public class UserContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Announcement> Announcements { get; set; }

    public UserContext(DbContextOptions<UserContext> options) : base(options) { }
}
