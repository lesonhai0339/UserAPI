using System.ComponentModel.DataAnnotations;

namespace UserAPI.Models.Authentication.ResetPassword
{
    public class ResetPassword
    {
        [Required]
        public string password { get; set; } = null!;
        [Compare("password", ErrorMessage = "The password and confirm password do not match!")]
        public string confirmPassword { get; set; } = null!;

        public string email { get; set; } = null!;
        public string token { get; set; } = null!;
    }
}
