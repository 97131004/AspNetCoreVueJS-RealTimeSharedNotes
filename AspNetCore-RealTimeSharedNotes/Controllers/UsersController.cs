using System.Security.Claims;
using AspNetCore_RealTimeSharedNotes.Data.Helpers;
using AspNetCore_RealTimeSharedNotes.Models.Constants;
using AspNetCore_RealTimeSharedNotes.Models.ViewModels;
using AspNetCore_RealTimeSharedNotes.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore_RealTimeSharedNotes.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
public class UsersController : BaseController
{
    private readonly IUsersService _userService;

    public UsersController(IUsersService userService) => _userService = userService;

    public IActionResult Index()
    {
        PopulateViewData();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Json(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
    {
        var requestingRole = User.GetRequestingRole();
        var result = await _userService.CreateUserAsync(model, requestingRole);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Json(new { apiKey = result.ApiKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var requestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var requestingRole = User.FindFirstValue(ClaimTypes.Role)!;
        var deleted = await _userService.DeleteUserAsync(requestingUserId, requestingRole, userId);
        if (!deleted)
            return Json(new { success = false });
        return Json(new { success = true });
    }
}
