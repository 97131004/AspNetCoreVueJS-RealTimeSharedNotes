using AspNetCore_RealTimeSharedNotes.Models;
using AspNetCore_RealTimeSharedNotes.Models.Dtos;
using Microsoft.AspNetCore.Identity;

namespace AspNetCore_RealTimeSharedNotes.Data.Interfaces;

public interface IUsersRepository : IBaseRepository
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<ApplicationUser?> GetUserAsync(string userId);
    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
    Task<IdentityResult> AddRoleToUserAsync(ApplicationUser user, string role);
    Task<bool> DeleteUserAsync(string userId);
}
