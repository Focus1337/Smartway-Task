﻿namespace FileHub.Presentation.Models;

public class RegisterUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}