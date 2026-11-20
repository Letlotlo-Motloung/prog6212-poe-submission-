RaceDay — PROG6212 POE Part 3
A two-project ASP.NET Core solution that satisfies Part 3 of the PROG6212 Portfolio of Evidence. The solution separates data access and business logic into a Web API, while the MVC front-end consumes the API for every operation.

Project	Type	Responsibility
src/RaceDay.Api	ASP.NET Core Web API	All data access, business rules, authentication and persistence (extended from Part 2).
src/RaceDay.Mvc	ASP.NET Core MVC	The Part 3 front-end. Consumes the API for every data operation. Contains no database context, no EF Core reference and no replicated business logic.
Table of Contents
How Part 3 Requirements Are Met

Running the Solution

Demo Accounts

Project Structure

Design Notes

CI / CD

1. How Part 3 Requirements Are Met
MVC application setup
The MVC project talks to the Part 2 API exclusively through a single typed HttpClient (Services/ApiClient.cs). There is no DbContext, no connection string and no EF Core package in the MVC project.

Registration and login are performed by calling POST /api/auth/register and POST /api/auth/login.

Authenticated state and the user's role are stored in session (AddSession, Services/SessionKeys.cs) and survive navigation across every page.

Filters/SessionAuthAttribute.cs protects every non-public controller/action. Unauthenticated users are redirected to /Account/Login?returnUrl=…; users with the wrong role are sent to /Account/AccessDenied.

Each role gets a distinct interface and navigation:

Organiser → Dashboard, My Events (and per-event Categories, Enrolments, Results).

Participant → Events, My Enrolments, My Results.

Guest → Events, Log in, Create account.

Logging out clears the session and redirects to the login page.

Organiser interface
Dashboard — totals for events, upcoming events, enrolments, unique participants and published results, plus enrolments per event and the next event dates.

Events — create, edit and delete events with name, description, date, location, distance, event type, route info and an uploaded banner image.

Categories — add, edit and delete age or distance categories per event, including distance, capacity and entry fee.

Enrolments — every participant enrolled in a specific event, with their selected category, contact details and enrolment status.

Results — capture finish times and finishing positions per participant, then publish results to participants.

Participant interface
Home — all upcoming events with search, event-type, location and include-past filters.

Event details — full event information and available categories, with immediate enrolment into a chosen category.

My Enrolments — every entered event with category and enrolment status, plus cancellation.

My Results — personal race history across completed events showing event name, date, category, finish time and finishing position (e.g. 47th out of 312 finishers).

Profile — view and update personal information and upload a profile picture.

2. Running the Solution
Prerequisites
.NET 10 SDK — the solution targets net10.0.

(Optional) Docker Desktop, if you prefer to run the stack in containers.

Start everything (one step)
Start the RaceDay.Mvc project and the API is launched automatically.

Visual Studio:
Open RaceDay.sln and press F5 (or the green Start button). RaceDay.Mvc is the default startup project; it starts the API in the background, waits until it is ready, then opens the site at http://localhost:5090.

Command line:

bash
dotnet run --project src/RaceDay.Mvc
Alternatively, use the provided scripts:

bash
# Windows (PowerShell)
./run-all.ps1

# Linux / macOS
./run-all.sh
The scripts simply run dotnet run --project src/RaceDay.Mvc --launch-profile http.

The API’s Swagger UI is available at:

http://localhost:5088/swagger

https://localhost:7018/swagger (when the API is started with its https profile).

Opening the API root (/) redirects to Swagger.

Auto-start behaviour: Auto-start only happens in the Development environment and only if nothing is already listening on ApiBaseUrl. To start the API yourself instead, set "AutoStartApi": false in src/RaceDay.Mvc/appsettings.json, then run dotnet run inside src/RaceDay.Api first. The MVC app reads the API address from src/RaceDay.Mvc/appsettings.json:

json
{ "ApiBaseUrl": "http://127.0.0.1:5088/" }
The SQLite database (raceday.db) is created and seeded automatically on first API start.

Run with Docker
A docker-compose.yml file is included for containerised execution.

bash
docker compose up -d --build
This starts:

API on port 5088

MVC on port 5090

The MVC container is configured with ApiSettings__BaseUrl=http://api:8080/, using Docker’s internal network to reach the API.

To stop and remove the containers:

bash
docker compose down -v
3. Demo Accounts
Role	Email	Password
Organiser	thandiwe.mokoena@gmail.co.za	Password123!
Participant	lindiwe.dlamini@gmail.com	Password123!
Participant	sipho.nkosi@gmail.com	Password123!
Participant	ayesha.patel@gmail.com	Password123!
Participant	johan.vanwyk@gmail.com	Password123!
Seed data includes six events (four upcoming, two completed), eleven categories, ten enrolments and five published results, so both interfaces are populated immediately.

4. Project Structure
text
RaceDayPart3/
├── README.md
├── RaceDay.sln
├── docker-compose.yml
├── run-all.ps1
├── run-all.sh
└── src/
    ├── RaceDay.Api/
    │   ├── Controllers/       # Auth, Events, Enrolments, Results, Users
    │   ├── Data/              # RaceDayDbContext + seed data
    │   ├── DTOs/              # Request/response contracts
    │   ├── Models/            # EF Core entities
    │   ├── Migrations/        # (schema created with EnsureCreated)
    │   └── Program.cs         # JWT auth, CORS, Swagger, SQLite
    └── RaceDay.Mvc/
        ├── Controllers/       # Account, Home, Participant, Organiser, Profile
        ├── Filters/           # SessionAuthAttribute
        ├── Models/            # Client-side DTOs and view models
        ├── Services/          # ApiClient, FileUpload, SessionKeys
        ├── Views/             # Razor views for all three role experiences
        └── wwwroot/           # site.css, site.js, uploads/

Key API endpoints used by the MVC app
text
POST   /api/auth/register
POST   /api/auth/login
GET    /api/events
GET    /api/events/{id}
GET    /api/events/mine
GET    /api/events/dashboard
POST   /api/events
PUT    /api/events/{id}
DELETE /api/events/{id}
POST   /api/events/{id}/categories
PUT    /api/events/{id}/categories/{cid}
DELETE /api/events/{id}/categories/{cid}
GET    /api/events/{id}/enrolments
GET    /api/events/{id}/results
POST   /api/events/{id}/publish-results
POST   /api/categories/{cid}/enrolments
GET    /api/users/me/enrolments
DELETE /api/enrolments/{id}
POST   /api/enrolments/{id}/results
PUT    /api/results/{id}
GET    /api/users/me/results
GET/PUT /api/users/me
PUT    /api/users/me/password

5. Design Notes
Custom CSS design system in wwwroot/css/site.css — no external CDN, responsive down to mobile and print-friendly.

File uploads (banners and profile pictures) are validated (type + 5 MB limit) and stored under wwwroot/uploads/{events|profiles}; the relative URL is persisted through the API.

JWT bearer tokens are attached to every API call by ApiClient from the session; the MVC layer never trusts client-supplied roles.

Result publishing is explicit: captured results stay hidden from participants until the organiser publishes them.


6. CI / CD
The repository includes a GitHub Actions workflow (.github/workflows/CI.yml) that runs on every push and pull request to main or master.

Jobs:

Build solution

Runs on ubuntu-latest

Sets up .NET 10.0.x

Restores, builds (Release) and tests RaceDay.sln

Docker build and smoke test

Depends on the build job

Builds and starts the stack with docker compose up -d --build --wait

Runs a smoke test against:

http://localhost:5090/ (MVC)

http://localhost:5088/swagger/v1/swagger.json (API)

Shows container logs on failure and tears down the stack afterwards


