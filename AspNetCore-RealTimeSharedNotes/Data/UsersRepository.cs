using AspNetCore_RealTimeSharedNotes.Models;
using AspNetCore_RealTimeSharedNotes.Models.Constants;
using AspNetCore_RealTimeSharedNotes.Models.Dtos;
using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore_RealTimeSharedNotes.Data;

public class UsersRepository : BaseRepository, IUsersRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersRepository(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : base(db)
    {
        _userManager = userManager;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
           .AsNoTracking() //disables change tracking for better performance on read-only query
           .Include(u => u.UserRoles)
           .ThenInclude(ur => ur.Role)
           .Select(u => new UserDto
           {
               UserId = u.Id,
               Email = u.Email ?? string.Empty,
               Role = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? UserRoles.User,
           })
           .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserAsync(string userId)
    {
        return await _db.Users
            .AsNoTracking() //disables change tracking for better performance on read-only query
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> AddRoleToUserAsync(ApplicationUser user, string role)
    {
        return await _userManager.AddToRoleAsync(user, role);
    }

    //auto-deletes user + its roles + its apikeys + its notes
    public async Task<bool> DeleteUserAsync(string userId)
    {
        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(); //atomic deletion, works with race conditions, thus always succeeds
        return true;
    }

}
