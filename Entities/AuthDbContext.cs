using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AuthDbContext : IdentityDbContext<ApiUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options) { }
    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder builder) => base.OnModelCreating(builder);
}