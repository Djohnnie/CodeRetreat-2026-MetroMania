# MetroMania - Copilot Instructions

MetroMania is a coding challenge web application inspired by the Mini Metro game. Players write C# bot code to solve metro network puzzles. Admins manage users, levels, and review submissions.

## Tech Stack

- **.NET 10.0** across all projects, with nullable reference types and implicit usings enabled.
- **Blazor Server** with `InteractiveServerRenderMode` for the UI, communicating over WebSocket.
- **MudBlazor v9.2.0** as the Material Design component library.
- **BlazorMonaco v3.4.0** for embedding the Monaco code editor (C# bot code submission).
- **Markdig v0.40.0** for rendering Markdown content (game instructions and user manuals).
- **MediatR v14.1.0** for CQRS command/query dispatching.
- **Entity Framework Core 10.0.5** with SQL Server as the database provider.
- **BCrypt.Net-Next v4.1.0** for password hashing.
- **Inter** (Google Fonts) as the primary typeface.
- No Node.js or npm tooling — the frontend is pure Blazor.

## Architecture

The solution follows **Clean Architecture** with four projects:

```
MetroMania.Domain          → Entities, enums, repository interfaces (no external dependencies)
MetroMania.Application     → CQRS commands/queries, DTOs, handler logic (depends on Domain)
MetroMania.Infrastructure  → EF Core DbContext, repository implementations, services (depends on Application)
MetroMania.Web             → Blazor UI, auth services, pages, layout (depends on Application + Infrastructure)
```

### Domain Layer (`MetroMania.Domain`)

- **Entities:** `User`, `Level`, `Submission`, plus value objects `LevelData`, `StationPlacement`, `WaterTile`.
- **Enums:** `ApprovalStatus` (Pending, Approved, Rejected), `UserRole` (User, Admin), `StationType` (Circle, Rectangle, Triangle, Diamond, Cross, Ruby).
- **Repository interfaces:** `IUserRepository`, `ILevelRepository`, `ISubmissionRepository` — each exposes async methods (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, etc.).
- **Service interfaces:** `IPasswordHasher`.
- This layer has zero external NuGet dependencies.

### Application Layer (`MetroMania.Application`)

- **CQRS via MediatR:** Every operation is either a command or a query with a dedicated handler implementing `IRequestHandler<TRequest, TResponse>`.
- Commands and queries are organized by feature area:
  - `Auth` — `RegisterCommand`, `LoginQuery`
  - `Users` — `ApproveUserCommand`, `DeleteUserCommand`, `GetAllUsersQuery`, `GetUserByIdQuery`
  - `Levels` — `CreateLevelCommand`, `UpdateLevelCommand`, `DeleteLevelCommand`, `ReorderLevelsCommand`, `GetAllLevelsQuery`, `GetLevelQuery`
  - `Submissions` — `SubmitCodeCommand`, `GetAllSubmissionsQuery`, `GetUserSubmissionsQuery`
  - `Language` — `ChangeLanguageCommand`
  - `Theme` — `ToggleThemeCommand`
- **DTOs:** `UserDto`, `LevelDto`, `SubmissionDto`, `UserSubmissionOverviewDto` — typically records with static `FromEntity()` factory methods.
- **Handlers use primary constructor injection** (C# 12+ syntax).

### Infrastructure Layer (`MetroMania.Infrastructure`)

- **`AppDbContext`** configures EF Core mappings, including JSON serialization for `LevelData` (stored as `nvarchar(max)` with `JsonSerializer`), string-based enum storage for `UserRole` and `ApprovalStatus`, and composite unique indexes.
- **Repository implementations:** `UserRepository`, `LevelRepository`, `SubmissionRepository`.
- **`PasswordHasher`** wraps BCrypt with work factor 12.
- **`DependencyInjection.AddInfrastructure()`** registers all scoped services (`DbContext`, repositories, `IPasswordHasher`).
- **Auto-migration:** The database is migrated on application startup.

### Web Layer (`MetroMania.Web`)

- **`Program.cs`** wires up DI, authentication, authorization, localization, and middleware.
- **Pages:** `Login`, `Register`, `Home` (dashboard), `Play` (code editor), `Info` (game instructions), `UserManagement`, `Levels`, `LevelEditor`, `SubmissionManagement`, `UserSubmissions`, `NotFound`, `Error`.
- **Layout:** `MainLayout.razor` with `MudAppBar`, `MudDrawer`, navigation menu, language switcher, theme toggle, and user profile chip.
- **Services:** `LoginTicketService` (one-time login tickets), `CookieAuthStateProvider` (custom `AuthenticationStateProvider`).

## Authentication

Authentication is **cookie-based** using the ASP.NET Core `"BlazorServer"` authentication scheme.

- Cookies expire after 30 days with sliding expiration.
- Login flow: `LoginQuery` validates credentials → `LoginTicketService` issues a one-time ticket → POST to `/api/auth/login-callback` redeems the ticket, creates a `ClaimsPrincipal`, and calls `SignInAsync`.
- Logout: POST to `/api/auth/logout` calls `SignOutAsync` and redirects to `/login`.
- **`CookieAuthStateProvider`** extends `AuthenticationStateProvider`, fetches the user from the database on each check, and validates that `ApprovalStatus == Approved`. Claims include `NameIdentifier`, `Name`, `Role`, `IsDarkMode`, and `Language`.
- **Password hashing:** BCrypt with a work factor of 12 via `IPasswordHasher`.
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
- **Language switching:** A dropdown in `MainLayout` with flag emojis (🇬🇧 / 🇳🇱). Changing language dispatches a `ChangeLanguageCommand` that persists the preference to the `User.Language` property, and the UI updates accordingly.
- **Language-specific assets:** User info SVGs and Markdown user manuals have language variants in `wwwroot` (e.g., `en/`, `nl/`).
- When adding new user-facing text, always add localization keys to both `.resx` files.

## UI Style

- **Component library:** MudBlazor (Material Design 3). Use `Mud*` components for all UI elements — do not use raw HTML controls.
- **Commonly used components:** `MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudContainer`, `MudPaper`, `MudText`, `MudButton`, `MudIconButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudLink`, `MudDivider`, `MudAvatar`, `MudIcon`, `MudMenu`, `MudMenuItem`, `MudTooltip`, `MudProgressCircular`, `MudDialogProvider`, `MudSnackbarProvider`.
- **Theme:** Dark and light modes, toggled per user via `ToggleThemeCommand` and cascaded as `IsDarkMode` through `CascadingValue`. The `MudThemeProvider` applies the active theme.
- **Code editor:** BlazorMonaco for C# code input on the Play page.
- **Markdown rendering:** Markdig converts Markdown to HTML for game info pages.
- **Custom CSS:** `wwwroot/app.css` includes `.card-hover` (hover animation with lift effect), `.blazor-error-boundary`, smooth theme-transition styles, and classes like `appbar-blur`, `hamburger-btn`, `theme-toggle-btn`, `drawer-animate`.
- **Font:** Inter (Google Fonts), weights 300–700.
- **Form validation:** `EditForm` with `DataAnnotationsValidator`.
- **Loading states:** `MudProgressCircular` during async operations.
- **Error display:** `MudAlert` with dismiss functionality.
- **Layout pattern:** `MudContainer` with `MaxWidth` for responsive content sizing.
