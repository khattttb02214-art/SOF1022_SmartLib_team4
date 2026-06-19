using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email không được trống")]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu không được trống")]
    public string Password { get; set; } = null!;
}
