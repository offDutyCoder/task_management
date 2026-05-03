using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByLoginIdAsync(string loginId);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> ExistsLoginIdAsync(string loginId, int? excludeUserId = null);
}
