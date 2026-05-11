using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using AspNetCore_RealTimeSharedNotes.Models;
using AspNetCore_RealTimeSharedNotes.Models.Constants;
using AspNetCore_RealTimeSharedNotes.Models.Dtos;
using AspNetCore_RealTimeSharedNotes.Models.Responses;
using AspNetCore_RealTimeSharedNotes.Models.ViewModels;
using AspNetCore_RealTimeSharedNotes.Services;
using AspNetCore_RealTimeSharedNotes.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace AspNetCore_RealTimeSharedNotes.UnitTests;

[TestFixture]
public class UsersServiceTests : TestBase
{
    private Mock<IUsersRepository> _repoMock;
    private Mock<IApiKeyService> _apiKeyServiceMock;
    private Mock<ILogger<UsersService>> _loggerMock;
    private UsersService _service;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IUsersRepository>();
        _apiKeyServiceMock = new Mock<IApiKeyService>();
        _loggerMock = new Mock<ILogger<UsersService>>();
        _service = new UsersService(_apiKeyServiceMock.Object, _repoMock.Object, _loggerMock.Object);

        _repoMock
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task<CreateUserResponse>>>()))
            .Returns<Func<Task<CreateUserResponse>>>(action => action());
    }

    //CreateUserAsync

    [Test]
    public async Task CreateUserAsync_RepositoryFailure_ReturnsFalseWithError()
    {
        const string expectedError = "Failed to create user.";
        var model = new CreateUserViewModel { Email = "test@test.com", Password = "Pass123!", Role = UserRoles.User };

        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<ApplicationUser>(), model.Password))
                 .ReturnsAsync(IdentityResult.Failed([new IdentityError { Description = expectedError }]));

        var (success, error, apiKey) = await _service.CreateUserAsync(model, UserRoles.Admin);

        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain(expectedError));
        Assert.That(apiKey, Is.Null);
    }

    public record CreateUserCase(
        string CreatorRole,
        string RequestedRole,
        string ExpectedAssignedRole);

    private static TestCaseData[] CreateUserAsync_Cases() =>
    [
        new TestCaseData(new CreateUserCase(UserRoles.Admin, UserRoles.Admin, UserRoles.User))
            .SetName("AdminCreator_AlwaysAssignsUserRole"),
        new TestCaseData(new CreateUserCase(UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.Admin))
            .SetName("SuperAdminCreator_CanAssignAdminRole"),
        new TestCaseData(new CreateUserCase(UserRoles.SuperAdmin, UserRoles.SuperAdmin, UserRoles.User))
            .SetName("SuperAdminCreator_CannotAssignSuperAdminRole"),
    ];

    [TestCaseSource(nameof(CreateUserAsync_Cases))]
    public async Task CreateUserAsync(CreateUserCase cuc)
    {
        var model = new CreateUserViewModel { Email = "test@test.com", Password = "Pass123!", Role = cuc.RequestedRole };

        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<ApplicationUser>(), model.Password))
                 .ReturnsAsync(IdentityResult.Success);
        _repoMock.Setup(r => r.AddRoleToUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                 .ReturnsAsync(IdentityResult.Success);
        _apiKeyServiceMock.Setup(a => a.CreateApiKeyAsync(It.IsAny<string>()))
                 .ReturnsAsync(new ApiKeyViewModel { ClientId = "cid", ClientSecret = "sec" });

        var (success, error, apiKey) = await _service.CreateUserAsync(model, cuc.CreatorRole);

        Assert.That(success, Is.True);
        _repoMock.Verify(r => r.AddRoleToUserAsync(It.IsAny<ApplicationUser>(), cuc.ExpectedAssignedRole), Times.Once);
    }

    //DeleteUserAsync

    public record DeleteUserCase(
        string RequesterUserId,
        string RequesterRole,
        string TargetUserId,
        string? TargetRole,   // null = user does not exist
        bool ExpectedResult,
        Times DeleteCallTimes);

    private static TestCaseData[] DeleteUserAsync_Cases() =>
    [
        new TestCaseData(new DeleteUserCase("admin-id", UserRoles.Admin, "admin-id", null, false, Times.Never()))
            .SetName("SameUserAsSelf_ReturnsFalse"),
        new TestCaseData(new DeleteUserCase("admin-id", UserRoles.Admin, "unknown-id", null, false, Times.Never()))
            .SetName("TargetUserDoesNotExist_ReturnsFalse"),
        new TestCaseData(new DeleteUserCase("admin-id", UserRoles.Admin, "other-admin-id",  UserRoles.Admin, false, Times.Never()))
            .SetName("AdminTriesToDeleteAdmin_ReturnsFalse"),
        new TestCaseData(new DeleteUserCase("admin-id", UserRoles.Admin, "user-id", UserRoles.User, true, Times.Once()))
            .SetName("AdminDeletesPlainUser_Succeeds"),
        new TestCaseData(new DeleteUserCase("superadmin-id", UserRoles.SuperAdmin, "admin-id", UserRoles.Admin, true, Times.Once()))
            .SetName("SuperAdminDeletesAdmin_Succeeds"),
    ];

    [TestCaseSource(nameof(DeleteUserAsync_Cases))]
    public async Task DeleteUserAsync(DeleteUserCase duc)
    {
        var target = duc.TargetRole != null
            ? MakeUser(duc.TargetUserId, $"{duc.TargetUserId}@test.com", duc.TargetRole)
            : null;

        _repoMock.Setup(r => r.GetUserAsync(duc.TargetUserId)).ReturnsAsync(target);
        if (duc.ExpectedResult)
            _repoMock.Setup(r => r.DeleteUserAsync(duc.TargetUserId)).ReturnsAsync(true);

        var result = await _service.DeleteUserAsync(duc.RequesterUserId, duc.RequesterRole, duc.TargetUserId);

        Assert.That(result, Is.EqualTo(duc.ExpectedResult));
        _repoMock.Verify(r => r.DeleteUserAsync(It.IsAny<string>()), duc.DeleteCallTimes);
    }
}
