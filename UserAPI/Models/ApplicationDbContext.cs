using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserAPI.Models.Authentication.Signup;
using UserAPI.Models.DbContext;

namespace UserAPI.Models
{
    public class ApplicationDbContext:IdentityDbContext<UserAccount>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            SeedRole(builder);
        }
        private static void SeedRole(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole() { Name="Admin",ConcurrencyStamp="1",NormalizedName="Admin"},
                new IdentityRole() { Name = "Mod", ConcurrencyStamp = "2", NormalizedName = "Moderator" },
                new IdentityRole() { Name = "User", ConcurrencyStamp = "3", NormalizedName = "User" }
                );
        }
    }
}
