using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using AspNetCore_RealTimeSharedNotes.PlaywrightTests.Config;
using AspNetCore_RealTimeSharedNotes.PlaywrightTests.PageObjects;

namespace AspNetCore_RealTimeSharedNotes.PlaywrightTests.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ShareNotesTests : PlaywrightTest
{
    private IBrowser _browser = null!;
    private IBrowserContext _adminContext = null!;
    private IBrowserContext _userContext = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        _browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, //hide browser window?
            Timeout = 30000,
            //SlowMo = 3000, //runs test with delays (makes it easier to see whats being tested)
        });

        _adminContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        _userContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _adminContext.DisposeAsync();
        await _userContext.DisposeAsync();
        await _browser.DisposeAsync();
    }

    [Test]
    public async Task TwoUsersShareNotes_SendingAndReceivingNote()
    {
        //superadmin logs in
        var adminPage = await _adminContext.NewPageAsync();
        var loginPage = new LoginPage(adminPage);

        await loginPage.GoToAsync();
        await loginPage.LoginAsync(TestConfig.SuperAdminEmail, TestConfig.SuperAdminPassword);

        //superadmin creates test user if not yet available
        var usersPage = new UsersPage(adminPage);
        await usersPage.GoToAsync();

        var userExists = await usersPage.UserExistsAsync(TestConfig.TestUserEmail);
        if (!userExists)
        {
            await usersPage.CreateUserAsync(TestConfig.TestUserEmail, TestConfig.TestUserPassword, TestConfig.TestUserRole);

            var createdUserExists = await usersPage.UserExistsAsync(TestConfig.TestUserEmail);
            Assert.That(createdUserExists, Is.True, $"user '{TestConfig.TestUserEmail}' should appear in the users list after creation");
        }

        //new browser, test user logs in
        var userPage = await _userContext.NewPageAsync();
        var userLoginPage = new LoginPage(userPage);

        await userLoginPage.GoToAsync();
        await userLoginPage.LoginAsync(TestConfig.TestUserEmail, TestConfig.TestUserPassword);

        //superadmin and test user navigate to notes page
        var adminNotesPage = new NotesPage(adminPage);
        var userNotesPage = new NotesPage(userPage);

        await adminNotesPage.GoToAsync();
        await userNotesPage.GoToAsync();

        //both users must see the same set of notes
        await Assertions.Expect(adminNotesPage.NotesList).ToBeVisibleAsync();
        await Assertions.Expect(userNotesPage.NotesList).ToBeVisibleAsync();

        var adminNoteIds = await adminNotesPage.GetNoteIdsAsync();
        var userNoteIds  = await userNotesPage.GetNoteIdsAsync();
        Assert.That(userNoteIds, Is.EqualTo(adminNoteIds), "both browsers should see the same notes");

        //superadmin posts a note, test user must receive it
        var noteContent = $"e2e test note – {DateTime.UtcNow:HH:mm:ss.fff}";
        var postedNoteId = await adminNotesPage.PostNoteAsync(noteContent);
        Assert.That(postedNoteId, Is.Not.Empty, "posted note must have a server-assigned id");

        var receivedContent = await userNotesPage.WaitForNoteAsync(postedNoteId);
        Assert.That(receivedContent, Is.EqualTo(noteContent), "the note received in real-time must have the same content as the one posted");
    }
}
