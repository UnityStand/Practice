using System.ComponentModel.DataAnnotations;

namespace EventApi.Domain.Entities;

public class User
{

    public Guid Id { get; set; }
    public string Login { get; private set; } = string.Empty;
    public string HashedPassword { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Customer;


    private User() { }


    public static User Create(string login, string passwordHash, UserRole role = UserRole.Customer)
    {
        if (string.IsNullOrEmpty(login))
            throw new ValidationException("login cannot be empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ValidationException("passwordHash cannot be empty");

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            HashedPassword = passwordHash,
            Role = role

        };
    }
}
