using Backend.Models.Entities;
using Backend.Models.DTOs;

namespace Backend.Services;

//TODO: This interface should be seperated into its own file
public interface IUserService
{
    LoginResponseDto Login(string email, string passwordHash);
    bool Logout();
    UserDto CreateUser(CreateUserDto userData);
    User DeactivateUser(Guid userId);
    List<User> GetActiveUsers();
}

public class UserService : IUserService
{
    public LoginResponseDto Login(string email, string passwordHash)
    {
        Console.WriteLine("UserService: Login request received");
        throw new NotImplementedException(); //to do implement
    }

    public bool Logout()
    {
        Console.WriteLine("UserService: Logout request received");
        throw new NotImplementedException(); //to do implement
    }
    

    public UserDto CreateUser(CreateUserDto userData)
    {
        Console.WriteLine("UserService: CreateUser request received");
        throw new NotImplementedException(); //to do implement
    }

    public User DeactivateUser(Guid userId)
    {
        Console.WriteLine("UserService: DeactivateUser request received");
        throw new NotImplementedException(); //to do implement
    }

    public List<User> GetActiveUsers()
    {
        Console.WriteLine("UserService: GetActiveUsers request received");
        return new List<User>(); //to do implement
    }
}
