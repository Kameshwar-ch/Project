using Skillforge.Domain;


namespace Skillforge.Repository;


public interface IUserRepository
{
    Task<bool> DeleteUser(int userId);
    Task<User?> Authenticate(string email, string password);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> UpdatePasswordAsync(string email, string hashedPassword);
    public Task UserRegisterAsync(User user);
    Task<User> GetUserByIdAsync(int id);
    Task<bool> UpdateUser(User user);
}

