using System.ComponentModel.DataAnnotations;

namespace UserAPI.Models.Authentication.Login
{
    public class LoginUser
    {
        [Required(ErrorMessage ="User Name Is Required!")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "User Name Is Required!")]
        public string? Password { get; set; }
    }
}
