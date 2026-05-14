# ASP.NET Core: Real-Time Shared Notes

A real-time collaborative notes application. Logged-in users can write and post notes, which are instantly replicated and shown to all users without page reloads. The system uses SignalR for real-time updates, Vue.js for a responsive frontend, and a secure ASP.NET Core backend with MS SQL Server and Entity Framework Core.

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/noteslist.PNG?raw=true)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/userslist.PNG?raw=true)


## Technologies & Versions

- **Backend:** ASP.NET Core (.NET 8)
- **Frontend:** Vue.js 3.4.0 with Vite 5.0.0 (Node.JS 20.17.0, Npm 10.8.2)
- **Database:** MS SQL Server 2022
- **ORM:** Entity Framework Core 8.0
- **Authentication:** ASP.NET Core Identity (roles: user, admin, superadmin)
- **Real-Time:** SignalR (with auto-reconnect and offline detection)
- **API:** Web API (REST), OAuth2
- **API Documentation:** Swagger (`https://localhost:7194/swagger`)
- **Testing:** Unit testing (NUnit, Moq), E2E testing (Playwright), Load testing (Grafana k6)


## Architecture

Monolithic design: MVC + service layer + data layer (communicates with DB). The backend exposes REST APIs and SignalR endpoints. Controllers use dependency-injected services, which call the data layer (Entity Framework Core). Notes and users are stored in the database. API keys are securely stored (client id  + encrypted client secrets) in the database. All note changes are broadcast to the frontend via SignalR (delta updates only). Frontend showcases different approaches: Login page (ASP.NET Razor SSR), Notes page (Vue.js component with Signal R), Users page (Vue.js component with async fetches).


## Features

- **Real-Time Notes:** Users post notes, instantly visible to all via `SignalR`.
- **User & Role Management:** Admins/superadmins manage users and roles. Superadmins can assign admin and user roles. Admins can only assign user role.
- **Role-Based Access:** Notes and user management restricted by role. Superadmins can delete any note. Admins can delete their own and user's notes. Users can only delete their own notes.
- **Authentication:** Email/password login. API key support (`OAuth2` via client id + client secret).
- **Notifications:** Queued, and responsive notification system (e.g., logout, errors, offline).
- **UI/UX:** Responsive, minimalistic design (includes desktop + mobile resolutions). Loading texts and disabled buttons during async ops.
- **Logging / Exception Handling:** All exceptions logged asynchronously (non-blocking) to file using a thread-safe producer/consumer queue (`System.Threading.Channels`). Robust user feedback in frontend (not exposing error exception to user).
- **SignalR:** Auto-reconnects after internet loss, with overlay notification.
- **User Deletion:** If a logged-in user is deleted, they immediately lose posting ability and are logged out on page refresh. Deleting a user removes their notes and API keys (SQL Cascade Delete).
- **API / Swagger:** API documentation & playground at `https://localhost:7194/swagger`.
- **Database:** Async queries whereever possible. EF writing operations are automatically rolled back when necessary (on error) to prevent partial saves and ensure data consistency.
- **Performance:** Frequent operations (e.g.: load all notes/users) have optimized queries (using .Include in EF Core to avoid N+1 issues).
- **Security:** SQL injection protection, encrypted API client secrets (via ASP.NET Core's `IDataProtector`), hashed passwords (no raw).

## Usage

- Login with email/password
- Write, post, and delete notes in real-time
- Admins manage users; superadmins manage admins + users
- API access via OAuth2

## Database Structure

- `AspNetUsers` (user info, email, etc.)
- `AspNetRoles`, `AspNetUserRoles` (role management)
- `Api` (userId, clientId, encrypted clientSecret)
- `Notes` (noteId, userId, content)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/db1.png?raw=true)

![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/db2.png?raw=true)


## Installation

**Database Setup:**

Run once (for main project) before running web app:

```
dotnet ef database drop --force
dotnet ef migrations add Init
dotnet ef database update
```

Database connection string in `appsettings.json`.

---

**First run of the main project:**

On the very first start of the main project `AspNetCore-RealTimeSharedNotes`, a superadmin user will be created with the following credentials (from `appsettings.json`):  

**Email/Username**: `superadmin@email.local`  
**Password**: `superadmin123!`  
**Client ID (API/Oauth2)**: `superadmin-sdq9`  
**Client Secret (API/Oauth2)**: `09cb65b4-ada9-42e9-8779-4f55e68b85a2`  
**Grant Type (API/Oauth2)**: `client_credentials`  

---

**Playwright browser installation (only needed for playwright test project):**

**Note**: Launch the main project at least once before running Playwright tests to ensure the required superadmin user is initialized.  

To install Playwright's chromimum browser, build `AspNetCore-RealTimeSharedNotes.PlaywrightTests` project once. Then run the following in PowerShell (as admin) from directory `AspNetCore-RealTimeSharedNotes/PlaywrightTests/bin/Debug/net8.0/` :

```
./playwright.ps1 install chromium
```

---

**Grafana k6 installation (only needed for load testing):**

To run the load test via Grafana k6, first install k6 via CLI (https://grafana.com/docs/k6/latest/set-up/install-k6/). Then run the following from directory `AspNetCore-RealTimeSharedNotes\LoadTests\` :

```
k6 run post_notes_load_test.js
```

## Testing

**End-2-End Testing (Playwright):** 2 users sharing notes with each other in real-time (video recorded test case):
  
![screenshot](https://github.com/97131004/AspNetCore-RealTimeSharedNotes/blob/main/Screenshots/playwright_AspNetCore-RealTimeSharedNotes_2.gif?raw=true)

---

**Load Testing (Grafana k6):** Stress testing concurrent posting + reading notes with multiple virtual users (VUs) in a controlled local network environment (isolated runner and server) using a slow ramp up from 0 to 5000 VUs:

**Infrastructure**: 2x Physical PCs (1x Test Runner, 1x Deployed Webserver [CPU: Intel Core i7-12700 12x 2.10GHz, RAM: 32GB DDR4 SDRAM]) on a 100Mbps local network.

**Test user configuration:** To simulate a realistic scenario, users are initialized with  different profiles/behaviours:  
80% of users are Readers: Only reading.  
15% of users are Casual Posters: 1 msg/60s + reading.  
5% of users are Power Posters: 1 msg/10s + reading.  

**Test process:** Users authenticate to obtain a token and connect to the SignalR notes hub via web sockets. All users receive real-time note updates, while a subset also broadcasts notes to all others. The test measures note RTT (round trip time) latency and aborts if thresholds are exceeded for RTT latency, web socket handshake or http timeouts. 

**Results**: The webserver reliably handles ≈2,400 VUs after 10 iterations of this load test. Maximum reliable send rate was 18 msg/s at 2400 VUs. Maximum reliable receive rate was 43182 msg/s at 2400 VUs. Failure occurs beyond 2400 VUs when network saturation causes signalr_note_rtt_lag to exceed the `p(90) < 3s` threshold (90% of all messages were delivered in under 3 seconds => has been exceeded). Webserver's CPU (≈10-15%) and RAM (≈700MB) usage remain within the system's capacity.

```
         /\      Grafana   /‾‾/  
    /\  /  \     |\  __   /  /   
   /  \/    \    | |/ /  /   ‾‾\ 
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/ 


     execution: local
        script: post_notes_load_test.js
        output: -

     scenarios: (100.00%) 1 scenario, 5000 max VUs, 17m30s max duration:
              * ramping_load_test: Up to 5000 looping VUs for 17m30s over 10 stages (gracefulRampDown: 30s)



  THRESHOLDS 

    http_req_failed
    ✓ 'rate < 0.01' rate=0.00%

    signalr_handshake_lag
    ✓ 'p(90) < 5000' p(90)=450.2

    signalr_note_rtt_lag
    ✗ 'p(90) < 3000' p(90)=3510.2

    ws_connecting
    ✓ 'p(90) < 5000' p(90)=449.9ms


  TOTAL RESULTS 

    checks_total.......: 4964    33.222918/s
    checks_succeeded...: 100.00% 4964 out of 4964
    checks_failed......: 0.00%   0 out of 4964

    ✓ redirected to notes
    ✓ redirected after login, status correct

    CUSTOM
    signalr_handshake_lag..........: avg=175.432323  min=3      med=19      max=7449  p(90)=450.2    p(95)=1057    
    signalr_note_rtt_lag...........: avg=1470.598446 min=12     med=296     max=32414 p(90)=3510.2   p(95)=9714.85 

    HTTP
    http_req_duration..............: avg=119.03ms    min=2.12ms med=28.63ms max=9.44s p(90)=309.66ms p(95)=472.93ms
      { expected_response:true }...: avg=119.03ms    min=2.12ms med=28.63ms max=9.44s p(90)=309.66ms p(95)=472.93ms
    http_req_failed................: 0.00%   0 out of 7469
    http_reqs......................: 7469    49.988311/s

    EXECUTION
    iteration_duration.............: avg=1m3s        min=21.96s med=59.38s  max=2m17s p(90)=1m59s    p(95)=2m1s    
    iterations.....................: 28      0.187398/s
    vus............................: 2480    min=0         max=2480
    vus_max........................: 5000    min=4021      max=5000

    NETWORK
    data_received..................: 356 MB  2.4 MB/s
    data_sent......................: 8.2 MB  55 kB/s

    WEBSOCKET
    ws_connecting..................: avg=175.21ms    min=3.1ms  med=18.43ms max=7.44s p(90)=449.9ms  p(95)=1.05s   
    ws_msgs_received...............: 1138942 7622.678722/s
    ws_msgs_sent...................: 3352    22.434171/s
    ws_session_duration............: avg=1m3s        min=21.55s med=59.28s  max=2m17s p(90)=1m59s    p(95)=2m1s    
    ws_sessions....................: 2475    16.56461/s
```

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
