using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using AspNetCore_RealTimeSharedNotes.Models;
using AspNetCore_RealTimeSharedNotes.Models.Constants;
using AspNetCore_RealTimeSharedNotes.Models.Dtos;
using AspNetCore_RealTimeSharedNotes.Models.Responses;
using AspNetCore_RealTimeSharedNotes.Models.ViewModels;
using AspNetCore_RealTimeSharedNotes.Services.Interfaces;
using Microsoft.Identity.Client;

namespace AspNetCore_RealTimeSharedNotes.Services;

public class UsersService : IUsersService
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IUsersRepository _repo;

    public UsersService(IApiKeyService apiKeyService, IUsersRepository repo)
    {
        _apiKeyService = apiKeyService;
        _repo = repo;
    }

    public Task<List<UserDto>> GetAllUsersAsync()
    {
        return _repo.GetAllUsersAsync();
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserViewModel model, string creatorRole)
    {
        //superadmin can assign any role (except superadmin); admin can only create regular users
        var allowedRole = (creatorRole == UserRoles.SuperAdmin && model.Role != UserRoles.SuperAdmin) ? model.Role : UserRoles.User;
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

        //if any db writing step fails, all operations will be rolled back and no partial data will be saved
        return await _repo.ExecuteInTransactionAsync<CreateUserResponse>(async () =>
        {
            var result = await _repo.CreateUserAsync(user, model.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            var roleResult = await _repo.AddRoleToUserAsync(user, allowedRole);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));

            var apiKey = await _apiKeyService.CreateApiKeyAsync(user.Id);
            return new CreateUserResponse(true, null, apiKey);
        });
    }

    public async Task<bool> DeleteUserAsync(string requestingUserId, string requestingRole, string targetUserId)
    {
        if (requestingUserId == targetUserId)
            return false;

        var targetUser = await _repo.GetUserAsync(targetUserId);
        if (targetUser == null)
            return false;

        var targetRole = targetUser.UserRoles.FirstOrDefault()?.Role.Name ?? UserRoles.User;

        //admin can only delete plain users; superadmin can delete admin+user
        if (requestingRole == UserRoles.Admin && targetRole != UserRoles.User)
            return false;

        return await _repo.DeleteUserAsync(targetUserId);
    }

    public async Task<ApplicationUser> GetUserAsync(string userId)
    {
        return await _repo.GetUserAsync(userId);
    }
}

