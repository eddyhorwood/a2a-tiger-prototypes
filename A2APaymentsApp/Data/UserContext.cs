using Microsoft.EntityFrameworkCore;
using A2APaymentsApp.Models;

// Database model to be used when entity framework will create the SignUpWithXeroUsers.db
public class UserContext : DbContext
{
    public UserContext(
        DbContextOptions<UserContext> options
        ) : base(options) { }


    // The .db will have table headings of values declared in SignUpWithXeroUser.cs file
    public DbSet<SignUpWithXeroUser> SignUpWithXeroUsers { get; set; }
    
    public DbSet<Organisation> Organisations { get; set; }


    // The .db will have the table called "SignUpWithXeroUsers"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SignUpWithXeroUser>().ToTable("SignUpWithXeroUsers");
    }
}