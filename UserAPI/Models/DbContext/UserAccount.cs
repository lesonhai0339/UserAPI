using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace UserAPI.Models.DbContext
{
    public class UserAccount:IdentityUser
    {
        public string? Avatar { get; set; }
        public string? Name { get; set; }
    }
}
