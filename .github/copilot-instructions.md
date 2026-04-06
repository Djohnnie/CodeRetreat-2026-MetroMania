# MetroMania - Copilot Instructions

MetroMania is a coding challenge web application inspired by the Mini Metro game. Players write C# bot code to solve metro network puzzles. Admins manage users, levels, and review submissions. Submitted code is validated and executed in isolated Orleans grains, with results scored per level and displayed on a leaderboard.

## Tech Stack

- **.NET 10.0** across all projects, with nullable reference types and implicit usings enabled.
- **Blazor Server** with `InteractiveServerRenderMode` for the UI, communicating over WebSocket.
- **ASP.NET Minimal API** for the backend REST API (`MetroMania.Api`), exposing domain endpoints consumed by the Blazor frontend via `HttpClient`.
- **Microsoft Orleans 10.0.1** as the distributed actor framework for sandboxed script execution and validation (two separate silos).
- **Azure Service Bus 7.20.1** for async submission processing via message queues.
- **MudBlazor v9.2.0** as the Material Design component library.
- **BlazorMonaco v3.4.0** for embedding the Monaco code editor (C# bot code submission).
- **Markdig v1.1.2** for rendering Markdown content (game instructions and user manuals).
- **MediatR v14.1.0** for CQRS command/query dispatching (used in the API and Worker projects).
- **Entity Framework Core 10.0.5** with SQL Server as the database provider.
- **Microsoft.CodeAnalysis.CSharp.Scripting v5.3.0** (Roslyn) for compiling and executing user-submitted C# scripts.
- **SkiaSharp v3.119.2** and **Svg.Skia v3.7.0** for SVG-based game state rendering.
- **BCrypt.Net-Next v4.1.0** for password hashing.
- **Microsoft.Extensions.Http.Resilience v10.4.0** for resilient HTTP calls (retry with exponential backoff).
- **Inter** (Google Fonts) as the primary typeface.
- No Node.js or npm tooling — the frontend is pure Blazor.

## Architecture

The solution follows **Clean Architecture** with 14 projects organized into solution folders:

```
1. Application/
   MetroMania.Application     → CQRS commands/queries, DTOs, handler logic, service interfaces
   MetroMania.Engine           → Game simulation engine, rendering, C# script compilation

2. Domain/
   MetroMania.Domain           → Entities, enums, repository interfaces (no external dependencies)

3. Infrastructure/
   MetroMania.Api              → ASP.NET Minimal API, MediatR dispatching, endpoint groups
   MetroMania.Infrastructure.Sql      → EF Core DbContext, repository implementations, password hashing
   MetroMania.Infrastructure.ServiceBus → Azure Service Bus queue client for async submission processing
   MetroMania.Infrastructure.Orleans   → Orleans client wrappers (service layer for grain communication)
   MetroMania.Orleans.Contracts        → Grain interfaces and serializable DTOs shared between silos and clients
   MetroMania.Orleans.Host             → Orleans silo for game script execution (GameRunnerGrain)
   MetroMania.Orleans.ValidationHost   → Orleans silo for script validation (GameRunnerValidationGrain)
   MetroMania.Web              → Blazor UI, auth services, pages, layout, HttpClient API calls
   MetroMania.Worker           → Background worker service consuming Service Bus messages

4. Tests/
   MetroMania.Engine.Tests     → BDD tests for the game engine (Reqnroll + xUnit v3)

9. Other/
   MetroMania.Demo             → Console demo app for testing engine and script compilation
```

### Domain Layer (`MetroMania.Domain`)

- **Entities:** `User`, `Level`, `Submission`, `SubmissionScore`, plus the `LevelData` value object with nested types `MetroStation`, `Water`, `PassengerSpawnPhase`, `WeeklyGiftOverride`.
- **Enums:**
  - `ApprovalStatus` — Pending, Approved, Rejected
  - `UserRole` — User, Admin
  - `SubmissionStatus` — Waiting, Running, Succeeded, Failed
  - `StationType` — Circle, Rectangle, Triangle, Diamond, Pentagon, Star
  - `ResourceType` — Line, Train
- **Repository interfaces:** `IUserRepository`, `ILevelRepository`, `ISubmissionRepository`, `ISubmissionScoreRepository` — each exposes async methods (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, etc.).
- **Extensions:** `StringExtensions` with `Base64Encode()` and `Base64Decode()` helpers.
- This layer has zero external NuGet dependencies.

### Application Layer (`MetroMania.Application`)

- **CQRS via MediatR:** Every operation is either a command or a query with a dedicated handler implementing `IRequestHandler<TRequest, TResponse>`.
- Commands and queries are organized by feature area:
  - `Auth` — `RegisterCommand`, `LoginQuery`
  - `Users` — `ApproveUserCommand`, `DeleteUserCommand`, `GetAllUsersQuery`, `GetUserByIdQuery`
  - `Levels` — `CreateLevelCommand`, `UpdateLevelCommand`, `DeleteLevelCommand`, `ReorderLevelCommand`, `GetAllLevelsQuery`, `GetLevelQuery`, `UpdateGridDataCommand`
  - `Submissions` — `SubmitCodeCommand`, `UpdateSubmissionStatusCommand`, `SaveSubmissionScoresCommand`, `GetSubmissionByIdQuery`, `GetUserSubmissionsQuery`, `GetAllSubmissionOverviewsQuery`, `GetLeaderboardQuery`, `GetStarterCodeQuery`
  - `Language` — `ChangeLanguageCommand`
  - `Theme` — `ToggleThemeCommand`
- **DTOs:** `UserDto`, `LevelDto`, `SubmissionDto`, `SubmissionScoreDto`, `LeaderboardEntryDto`, `UserSubmissionOverviewDto` — immutable records with static `FromEntity()` factory methods.
- **Service interfaces:**
  - `IPasswordHasher` — password hashing and verification
  - `IScriptValidationService` — validates user-submitted C# scripts (returns `ScriptValidationResult`)
  - `ISubmissionQueueService` — enqueues submissions for async background processing
- **Messages:** `ProcessSubmissionMessage(SubmissionId)` — async message consumed by the Worker.
- **Handlers use primary constructor injection** (C# 12+ syntax).
- Depends on `MetroMania.Domain` and `MetroMania.Engine`.

### Engine (`MetroMania.Engine`)

The core game simulation engine, decoupled from web/API/database concerns:

- **`MetroManiaEngine`** — main simulation class with `Run()` (until game over) and `RunForHours()` (partial simulation) methods.
- **`IMetroManiaRunner`** — callback interface that player bots implement. Receives events: `OnHourTick`, `OnDayStart`, `OnWeeklyGift`, `OnStationSpawned`, `OnPassengerWaiting`, `OnStationOverrun`, `OnGameOver`.
- **Player actions:** `PlayerAction` sealed record hierarchy — `NoAction`, `CreateLine`, `RemoveLine`, `ExtendLineFromTerminal`, `ExtendLineInBetween`, `AddVehicleToLine`, `RemoveVehicle`.
- **Game model:** `GameSnapshot`, `GameResult`, `GameTime`, `StationSnapshot`, `LineSnapshot`, `VehicleSnapshot`, `ResourceSnapshot`, `Passenger`, `Location`.
- **Routing:** Dijkstra-based shortest-path for intelligent passenger pickup — passengers only board if the vehicle's line offers a competitive route.
- **Rendering:** `MetroManiaRenderer` uses SkiaSharp + Svg.Skia to render game state as SVG with 32×32 tile grids, water tile blending (8-way directional), and station icon overlays.
- **Scripting:** `ScriptCompiler<TResult>` compiles user C# scripts via Roslyn. `StarterCode` provides the template `IMetroManiaRunner` implementation. `ScriptGlobals` exposes the `Level` property to scripts.
- **Game rules:**
  - Initial resources: 1 Line + 1 Train
  - Weekly gifts: random (or overridden) resource every Monday (Line/Train)
  - Station overrun alert at 10+ passengers; game over at 20+ passengers
  - Vehicle capacity: configurable per level (default 6)
  - Deterministic gameplay via seed-based RNG
- Depends only on `MetroMania.Domain`.

### Infrastructure Layer

The infrastructure is split into four focused projects:

#### `MetroMania.Infrastructure.Sql`

- **`AppDbContext`** configures EF Core mappings: `LevelData` stored as JSON (`nvarchar(max)` with `JsonSerializer`), string-based enum storage for `UserRole`, `ApprovalStatus`, and `SubmissionStatus`, composite unique indexes on (UserId, Version) for submissions and (SubmissionId, LevelId) for scores.
- **Repository implementations:** `UserRepository`, `LevelRepository`, `SubmissionRepository`, `SubmissionScoreRepository`.
- **`PasswordHasher`** wraps BCrypt with work factor 12.
- **`DependencyInjection.AddInfrastructure(connectionString)`** registers all scoped services.

#### `MetroMania.Infrastructure.ServiceBus`

- **`SubmissionQueueService`** implements `ISubmissionQueueService` — serializes `ProcessSubmissionMessage` as JSON and sends to Azure Service Bus.
- Configured via `SERVICE_BUS_CONNECTION_STRING` and `SERVICE_BUS_QUEUE` environment variables.
- Registered as singleton via `DependencyInjection.AddServiceBus()`.

#### `MetroMania.Infrastructure.Orleans`

- **`GameRunnerService`** wraps `IGameRunnerGrain` calls (script execution against levels).
- **`GameRunnerValidationService`** wraps `IGameRunnerValidationGrain` calls and implements `IScriptValidationService` from the Application layer.
- Registered via `DependencyInjection.AddOrleansClient()` — multi-interface registration so `GameRunnerValidationService` satisfies both `IGameRunnerValidationService` and `IScriptValidationService`.

#### `MetroMania.Orleans.Contracts`

- **Grain interfaces:** `IGameRunnerGrain` (`RunScriptAsync`) and `IGameRunnerValidationGrain` (`ValidateScriptAsync`) — both `IGrainWithGuidKey`.
- **Serializable DTOs:** `ScriptRunResult` (Success, Error, Score, TimeTakenMs, DaysSurvived, TotalPassengersSpawned) and `ScriptValidationResult` (Success, Errors) with `[GenerateSerializer]` and `[Id(N)]` attributes.

### Orleans Silos

#### `MetroMania.Orleans.Host` (Game Execution Silo)

- Hosts `GameRunnerGrain` — receives base64-encoded player script + JSON level data, wraps the script in an outer harness that instantiates `MetroManiaEngine` and `MyMetroManiaRunner`, compiles via Roslyn, and executes. Returns `ScriptRunResult` with score and metrics.
- Configured with localhost clustering, in-memory grain storage, and Orleans Dashboard at `/dashboard`.

#### `MetroMania.Orleans.ValidationHost` (Validation Silo)

- Hosts `GameRunnerValidationGrain` — validates scripts by compiling and test-executing against a minimal 4×1 grid level (seed 42, 2 stations). Catches compilation and runtime errors without running against real levels.
- Does **not** reference `MetroMania.Domain` — minimal dependency footprint.
- Same silo configuration as Host (localhost clustering, in-memory storage, dashboard).

### Worker (`MetroMania.Worker`)

- **.NET Worker Service** (`BackgroundService`) that consumes `ProcessSubmissionMessage` messages from Azure Service Bus.
- **Processing pipeline:**
  1. Receives message → fetches submission and all levels via MediatR
  2. Updates submission status to `Running`
  3. Executes the script against **all levels in parallel** via Orleans `IGameRunnerService`
  4. Saves per-level scores via `SaveSubmissionScoresCommand`
  5. Updates final status to `Succeeded` or `Failed`
  6. Acknowledges (completes) or abandons the message
- Sequential processing: `MaxConcurrentCalls = 1`, `AutoCompleteMessages = false`.
- Creates a DI scope per message for `ISender` (MediatR) and `IGameRunnerService`.

### API Layer (`MetroMania.Api`)

- **ASP.NET Minimal API** project that hosts all backend endpoints.
- **MediatR** is registered here and dispatches commands/queries from the Application layer.
- **Orleans client** is connected for script validation during submission.
- **Service Bus** is registered for enqueueing submissions.
- **JWT Bearer Authentication:** All endpoints (except auth) require a valid JWT token.
- **Auto-migration:** The database is migrated on API startup.
- **Endpoint organization:** Each domain area has its own static class with a `Map*Endpoints()` extension method in the `Endpoints/` folder:
  - `AuthEndpoints.cs` — `POST /api/auth/login`, `POST /api/auth/register` (anonymous — no JWT required)
  - `UserEndpoints.cs` — `GET /api/users`, `GET /api/users/{id}` (authenticated), `POST /api/users/{id}/approve`, `DELETE /api/users/{id}` (**Admin only**)
  - `LevelEndpoints.cs` — `GET /api/levels`, `GET /api/levels/{id}` (authenticated), `POST /api/levels`, `PUT /api/levels/{id}`, `DELETE /api/levels/{id}`, `POST /api/levels/{id}/reorder`, `PUT /api/levels/{id}/grid-data` (**Admin only** for mutations)
  - `SubmissionEndpoints.cs` — `GET /api/submissions/overviews` (**Admin only**), `GET /api/submissions/users/{userId}`, `GET /api/submissions/starter-code`, `POST /api/submissions` (authenticated)
  - `LeaderboardEndpoints.cs` — `GET /api/leaderboard` (authenticated)
  - `ThemeEndpoints.cs` — `POST /api/theme/toggle` (JWT required)
  - `LanguageEndpoints.cs` — `POST /api/language/change` (JWT required)
- **Request records** are defined in each endpoint file (e.g., `LoginRequest`, `CreateLevelRequest`).
- **JSON serialization** uses `JsonStringEnumConverter` for consistent enum handling.
- **CORS** is configured to allow the Blazor Web project to call the API.

### Web Layer (`MetroMania.Web`)

- **`Program.cs`** wires up DI, authentication, authorization, localization, `HttpClient` (with resilience: 5 retries, exponential backoff), `JwtTokenProvider`, and middleware.
- **`MetroManiaApiClient`** — a typed `HttpClient` service that wraps all API calls. Calls `SetAuthHeader()` before each request to attach the JWT Bearer token. Auth endpoints (login, register) do not send a JWT since the user is not yet authenticated.
- **`JwtTokenProvider`** — a scoped (circuit-lifetime) service that generates JWT tokens from the authenticated user's cookie claims. On circuit start, reads claims from `HttpContext` (cookie); caches the token and re-generates it when expired using cached claims (for SignalR).
- **`LoginTicketService`** — stores the full `UserDto` (not just the user ID) in a one-time ticket (1-minute TTL) so the login callback can create cookie claims without making an API call.
- **Pages:**

  | Page | Route | Access | Purpose |
  |------|-------|--------|---------|
  | Home | `/` | Authenticated | Dashboard with welcome, profile info, admin panel link |
  | Login | `/login` | Unauthenticated | Email/password form |
  | Register | `/register` | Unauthenticated | Account creation with pending/approved status display |
  | Info | `/info` | Authenticated | Game instructions from markdown + SVG carousel |
  | Play | `/play` | Authenticated | Monaco code editor, submission history, score display |
  | Leaderboard | `/leaderboard` | Authenticated | Ranked user scores with expandable level breakdown |
  | Levels | `/admin/levels` | Admin | Level CRUD, reorder, metadata editing |
  | LevelEditor | `/admin/levels/{LevelId}` | Admin | Interactive grid editor: stations, water, spawn phases, gift overrides |
  | UserManagement | `/admin/users` | Admin | Approve/reject registrations, delete users |
  | SubmissionManagement | `/admin/submissions` | Admin | All user submission overviews |
  | UserSubmissions | `/admin/submissions/{UserId}` | Admin | Detailed submission history with embedded Monaco viewer and level scores |
  | Error | `/Error` | All | Global error handler |
  | NotFound | `/not-found` | All | 404 page |

- **Layout:** `MainLayout.razor` with `MudAppBar`, `MudDrawer`, navigation menu, language switcher, theme toggle, and user profile chip. Responsive drawer collapses on small screens.
- **Services:** `LoginTicketService`, `CookieAuthStateProvider`, `MetroManiaApiClient`, `JwtTokenProvider`.
- The Web project references `MetroMania.Application` only for DTO types and domain enums — it does **not** reference infrastructure projects or use MediatR.

## Running the Application

Five projects must run simultaneously for full functionality:

1. **Orleans Validation Silo:** `dotnet run --project src/MetroMania.Orleans.ValidationHost`
2. **Orleans Game Silo:** `dotnet run --project src/MetroMania.Orleans.Host`
3. **API:** `dotnet run --project src/MetroMania.Api` (listens on `https://localhost:5101`)
4. **Worker:** `dotnet run --project src/MetroMania.Worker` (processes Service Bus messages)
5. **Web:** `dotnet run --project src/MetroMania.Web` (connects to the API via the `ApiBaseUrl` setting)

Both the API and Worker require `SQL_CONNECTION_STRING`, `SERVICE_BUS_CONNECTION_STRING`, and `SERVICE_BUS_QUEUE` environment variables (or matching `appsettings.json` entries).

The API base URL is configured in `MetroMania.Web/appsettings.json` under the `ApiBaseUrl` key.

The Demo project can be run standalone: `dotnet run --project src/MetroMania.Demo`

## Submission Pipeline

The end-to-end flow for code submissions:

1. **User submits code** on the Play page → `MetroManiaApiClient.SubmitCodeAsync()` → API `POST /api/submissions`
2. **API validates** via Orleans `IScriptValidationService` → `GameRunnerValidationGrain` compiles and test-runs against a minimal level
3. **On validation success**, submission is stored (base64-encoded, status `Waiting`) and enqueued to Azure Service Bus via `ISubmissionQueueService`
4. **Worker receives** `ProcessSubmissionMessage` → marks submission `Running` → executes script against **all levels in parallel** via Orleans `IGameRunnerService` → `GameRunnerGrain` runs full simulation per level
5. **Worker saves** per-level `SubmissionScore` results → marks submission `Succeeded` or `Failed`
6. **Leaderboard** aggregates the best scores across all users

## Authentication

Authentication uses a **two-layer** approach:

### Layer 1: Cookie Authentication (Web ↔ Browser)

Cookie-based auth using the ASP.NET Core `"BlazorServer"` scheme, managed in the Web project.

- Cookies expire after 30 days with sliding expiration.
- **Claims stored in cookie:** `NameIdentifier` (user ID), `Name`, `Role`, `IsDarkMode`, `Language`.
- Login flow: Blazor `Login` page calls `MetroManiaApiClient.LoginAsync()` → API validates credentials via MediatR → on success, `LoginTicketService` stores the full `UserDto` in a one-time ticket → browser redirects to `/api/auth/login-callback` (in Web) which redeems the ticket, creates a `ClaimsPrincipal` with all 5 claims, and calls `SignInAsync`.
- Logout: GET to `/api/auth/logout` (in Web) calls `SignOutAsync` and redirects to `/login`.
- **`CookieAuthStateProvider`** extends `AuthenticationStateProvider`, calls the API to fetch user details, and validates that `ApprovalStatus == Approved`.

### Layer 2: JWT Bearer Authentication (Web → API)

The API requires a valid JWT token on all endpoints except `/api/auth/login` and `/api/auth/register`.

- **`JwtTokenProvider`** (scoped, circuit-lifetime in Web) generates JWT tokens from the authenticated user's cookie claims. On circuit initialization, it reads claims from `HttpContext` (cookie); it caches the token and the `ClaimsPrincipal`, regenerating the JWT when it expires using the cached claims (for SignalR requests where `HttpContext` is unavailable).
- **`MetroManiaApiClient`** calls `SetAuthHeader()` before each authenticated API call, which retrieves the current JWT from `JwtTokenProvider` and sets the `Authorization: Bearer <token>` header.
- **JWT claims** include: `NameIdentifier`, `Name`, `Role`, `IsDarkMode`, `Language` — mirroring the cookie claims.
- **Shared configuration:** Both projects have a `Jwt` section in `appsettings.json` with matching `SecretKey`, `Issuer`, `Audience`, and `ExpirationMinutes`. The signing algorithm is HMAC-SHA256.
- **Token expiration:** Default 60 minutes; auto-refreshed by `JwtTokenProvider` with a 5-minute buffer.
- **Password hashing:** BCrypt with a work factor of 12 via `IPasswordHasher` (in Infrastructure.Sql, called by API handlers).
- **User approval workflow:** The first registered user automatically becomes an Admin with Approved status. All subsequent users start as Pending and must be approved by an Admin before they can log in.
- **Authorization in components:** Uses `AuthorizeView` and claims-based checks (`context.User.Identity?.IsAuthenticated`, role claims).

## Multilanguage / Localization

Localization uses **ASP.NET Core `IStringLocalizer<T>`** with `.resx` resource files.

- **Supported languages:** English (`en`, default) and Dutch (`nl`).
- **Resource files:** `Resources/Localization.resx` (English) and `Resources/Localization.nl.resx` (Dutch), with a marker class `Localization.cs`.
- **Configuration:** `AddLocalization()` in DI; `UseRequestLocalization` with supported cultures `["en", "nl"]`.
- **Usage in components:**
  ```razor
  @inject IStringLocalizer<Localization> Loc
  <MudText>@Loc["AppName"]</MudText>
  ```
- **Language switching:** A dropdown in `MainLayout` with flag emojis (🇬🇧 / 🇳🇱). Changing language calls `MetroManiaApiClient.ChangeLanguageAsync()` to persist the preference, then updates the culture cookie and reloads.
- **Language-specific assets:** User info SVGs and Markdown user manuals have language variants in `wwwroot` (e.g., `en/`, `nl/`).
- When adding new user-facing text, always add localization keys to both `.resx` files.

## Testing

- **Framework:** [Reqnroll](https://reqnroll.net/) v3.3.4 (BDD/Gherkin) with **xUnit v3** (3.2.2) as the test runner and **Moq** v4.20.72 for mocking.
- **Test project:** `MetroMania.Engine.Tests` — covers the game engine simulation logic.
- **Structure:**
  - `Features/*.feature` — 24 Gherkin feature files covering game loop, events, stations, passengers, lines, trains, collisions, removal, resources, determinism, snapshots, and more.
  - `StepDefinitions/*.cs` — 18 step definition classes organized by feature area (e.g., EngineStepDefinitions, PlayerActionsStepDefinitions, TrainCollisionStepDefinitions, RemoveVehicleStepDefinitions, etc.).
  - `Support/EngineTestContext.cs` — Shared per-scenario context holding the engine, `Mock<IMetroManiaRunner>`, level configuration (stations, weekly gift overrides, seed, vehicle capacity), simulation results (`GameSnapshot`, `GameResult`), and comprehensive event tracking (`EventLog`, `DayStartCalls`, `HourTickCalls`, `PassengerWaitingCalls`, `OverrunCalls`, `GameOverCalls`, `WeeklyGiftTypes`, `WeeklyGiftEvents`, `PendingActions`, `StationIdsByLocation`).
- **Conventions:**
  - All new engine tests must be written as Reqnroll feature files — do not use plain `[Fact]`/`[Theory]` xUnit tests.
  - Step definition classes use **primary constructor injection** with `EngineTestContext` for shared state.
  - Group related step definitions in a dedicated file per feature area.
  - Reuse existing Given/When steps from `EngineStepDefinitions.cs` wherever possible; only add new steps for genuinely new behavior.
  - The mock runner captures all engine callbacks in `EventLog` (by event name) and typed lists for assertions.
  - Use `EngineTestContext.BuildLevel()` to construct levels — configure via `ctx.Seed`, `ctx.Stations`, `ctx.WeeklyGiftOverrides`, etc.
  - Player actions use a **deferred action pattern**: `ctx.PendingActions.Add(snapshot => ...)` — actions are queued and executed during `OnHourTick` callbacks.
  - Determinism testing uses `ctx.PrepareForRerun()` to reset tracking while preserving previous gift sequences for comparison.
- **Running tests:** `dotnet test src\MetroMania.Engine.Tests`

## UI Style

- **Component library:** MudBlazor (Material Design 3). Use `Mud*` components for all UI elements — do not use raw HTML controls.
- **Commonly used components:** `MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudContainer`, `MudPaper`, `MudText`, `MudButton`, `MudIconButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudLink`, `MudDivider`, `MudAvatar`, `MudIcon`, `MudMenu`, `MudMenuItem`, `MudTooltip`, `MudProgressCircular`, `MudDialogProvider`, `MudSnackbarProvider`.
- **Theme:** Dark and light modes, toggled per user via `MetroManiaApiClient.ToggleThemeAsync()` and cascaded as `IsDarkMode` through `CascadingValue`. The `MudThemeProvider` applies the active theme. Light primary: `#2563eb`, dark primary: `#60a5fa`. Border radius: 12px.
- **Code editor:** BlazorMonaco for C# code input on the Play page and read-only code viewing on UserSubmissions.
- **Markdown rendering:** Markdig converts Markdown to HTML for game info pages.
- **Custom CSS:** `wwwroot/app.css` and scoped `.razor.css` files. Includes glassmorphism AppBar (`backdrop-filter: blur(12px)`), `.card-hover` (scale + border color hover animation), page enter animation (`pageEnter` with fade-in + slide), smooth theme-transition styles (300ms ease), and monospace font stack (`Cascadia Code`, `Fira Code`, `Consolas`) for code editors.
- **Font:** Inter (Google Fonts), weights 300–700.
- **Form validation:** `EditForm` with `DataAnnotationsValidator`.
- **Loading states:** `MudProgressCircular` during async operations.
- **Error display:** `MudAlert` with dismiss functionality.
- **Layout pattern:** `MudContainer` with `MaxWidth` for responsive content sizing. Responsive breakpoints via MudBlazor's `Breakpoint` enum.
- **Performance patterns:** Debounced auto-save (300ms) in LevelEditor with semaphore lock to prevent concurrent saves. SVG tile caching with color overlay replacement.
