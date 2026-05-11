# ASP.NET Core: Real-Time Shared Notes

A real-time collaborative notes application. Logged-in users can write and post notes, which are instantly replicated and shown to all users without page reloads. The system uses SignalR for real-time updates, Vue.js for a responsive frontend, and a secure ASP.NET Core backend with MS SQL Server and Entity Framework Core.

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/noteslist.PNG?raw=true)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/userslist.PNG?raw=true)

  
**2 users sharing notes with each other in real-time (recorded Playwright test case):**
  
![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/playwright_AspNetCore-RealTimeSharedNotes.gif?raw=true)


## Technologies & Versions

- **Backend:** ASP.NET Core (.NET 8)
- **Frontend:** Vue.js 3.4.0 with Vite 5.0.0 (Node.JS 20.17.0, Npm 10.8.2)
- **Database:** MS SQL Server 2022
- **ORM:** Entity Framework Core
- **Authentication:** ASP.NET Core Identity (roles: user, admin, superadmin)
- **Real-Time:** SignalR (with auto-reconnect and offline detection)
- **API:** Web API (REST), OAuth2
- **API Documentation:** Swagger (`https://localhost:7194/swagger`)
- **Testing:** Unit testing (NUnit, Moq), E2E testing (Playwright)


## Architecture

Monolithic design: MVC + service layer + data layer (communicates with DB). The backend exposes REST APIs and SignalR endpoints. Controllers use dependency-injected services, which call the data layer (Entity Framework Core). Notes and users are stored in the database. API keys are securely stored (client id  + encrypted client secrets) in the database. All note changes are broadcast to the frontend via SignalR (delta updates only). Frontend showcases different approaches: Login page (ASP.NET Razor SSR), Notes page (Vue.js component with Signal R), Users page (Vue.js component with async fetches).


## Features

- **Real-Time Notes:** Users post notes, instantly visible to all via `SignalR`.
- **User & Role Management:** Admins/superadmins manage users and roles. Superadmins can assign admin and user roles. Admins can only assign user role.
- **Role-Based Access:** Notes and user management restricted by role. Superadmins can delete any note. Admins can delete their own and user's notes. Users can only delete their own notes.
- **Authentication:** Email/password login. API key support (`OAuth2` via client id + client secret).
- **Notifications:** Queued, and responsive notification system (e.g., logout, errors, offline).
- **UI/UX:** Responsive, minimalistic design (includes desktop + mobile resolutions). Loading texts and disabled buttons during async ops.
- **Security:** SQL injection protection, encrypted API client secrets (via ASP.NET Core's `IDataProtector`), hashed passwords (no raw).
- **Performance:** Optimized queries for frequent operations (e.g., load all notes/users).
- **Cascade Delete:** Deleting a user removes their notes and API keys.
- **Logging / Exception Handling:** All exceptions logged asynchronously (non-blocking) to file using a thread-safe producer/consumer queue (`System.Threading.Channels`). Robust user feedback in frontend (not exposing error exception to user).
- **SignalR:** Auto-reconnects after internet loss, with overlay notification.
- **User Deletion:** If a logged-in user is deleted, they immediately lose posting ability and are logged out on page refresh.
- **API / Swagger:** API documentation & playground at `https://localhost:7194/swagger`.
- **Database:** Async requests whereever possible. EF writing operations are automatically rolled back when necessary (on error) to prevent partial saves and ensure data consistency.


## Database Structure

- `AspNetUsers` (user info, email, etc.)
- `AspNetRoles`, `AspNetUserRoles` (role management)
- `Api` (userId, clientId, encrypted clientSecret)
- `Notes` (noteId, userId, content)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/db1.png?raw=true)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/db2.png?raw=true)


## Installation

**Database Setup:**

Run once (from main project's package manager console) before running web app. Database connection string in `appsettings.json`.

```
dotnet ef database drop --force
Remove-Item Migrations* -Recurse
dotnet ef migrations add Init
dotnet ef database update
```

---

**First run of the main project:**

On the very first start of the main project `AspNetCore-RealTimeSharedNotes`, a superadmin user will be created with the following credentials (from `appsettings.json`):  

**Email/Username**: `superadmin@email.local`  
**Password**: `superadmin123!`  
**Client ID (API/Oauth2)**: `superadmin-sdq9`  
**Client Secret (API/Oauth2)**: `09cb65b4-ada9-42e9-8779-4f55e68b85a2`  
**Grant Type (API/Oauth2)**: `client_credentials`  

---

**Playwright Browser Install (only needed for playwright test project):**

**Note**: Launch the main project at least once before running Playwright tests to ensure the required superadmin user is initialized.  

To install Playwright's chromimum browser, build `AspNetCore-RealTimeSharedNotes.PlaywrightTests` project once. Then run the following in PowerShell (as admin) from directory `AspNetCore-RealTimeSharedNotes/PlaywrightTests/bin/Debug/net8.0/` :

```
./playwright.ps1 install chromium
```


## Usage

- Login with email/password
- Write, post, and delete notes in real-time
- Admins manage users; superadmins manage admins + users
- API access via OAuth2


## Screenshots

<p>
    <img src="https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/login.PNG" hspace="10">
    <img src="https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/mobilenoteslist.PNG" >
</p>

<p>
    <img src="https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/apicreated.PNG" hspace="10">
    <img src="https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/internetoffline.PNG" hspace="10" >
</p>

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/apiswagger.png?raw=true)


## License

MIT
