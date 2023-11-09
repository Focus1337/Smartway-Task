using System.ComponentModel.DataAnnotations;

namespace FileHub.Presentation.Models;

public class RegisterUserDto
{
    [Required] public string Email { get; set; }
    [Required] public string Password { get; set; }

    public RegisterUserDto(string email, string password)
    {
        Email = email;
        Password = password;
    }
}