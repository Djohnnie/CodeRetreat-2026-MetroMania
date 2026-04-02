using MetroMania.Application.Auth.Commands;
using MetroMania.Application.Auth.Queries;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroMania.Web.Services;

public class MetroManiaApiClient(HttpClient httpClient, JwtTokenProvider tokenProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private void SetAuthHeader()
    {
        var token = tokenProvider.GetToken();
        httpClient.DefaultRequestHeaders.Authorization = token is not null
            ? new AuthenticationHeaderValue("Bearer", token)
            : null;
    }

    // ── Auth (no JWT required — user is not yet authenticated) ───

    public async Task<LoginResult> LoginAsync(string name, string password)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", new { name, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions))!;
    }

    public async Task<RegisterResult> RegisterAsync(string name, string password)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", new { name, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResult>(JsonOptions))!;
    }

    // ── Users ─────────────────────────────────────────────────────

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<UserDto>>("/api/users", JsonOptions))!;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        SetAuthHeader();
        var response = await httpClient.GetAsync($"/api/users/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
    }

    public async Task<bool> ApproveUserAsync(Guid userId, ApprovalStatus newStatus)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync($"/api/users/{userId}/approve", new { newStatus });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        SetAuthHeader();
        var response = await httpClient.DeleteAsync($"/api/users/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    // ── Levels ────────────────────────────────────────────────────

    public async Task<List<LevelDto>> GetAllLevelsAsync()
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<LevelDto>>("/api/levels", JsonOptions))!;
    }

    public async Task<LevelDto?> GetLevelAsync(Guid id)
    {
        SetAuthHeader();
        var response = await httpClient.GetAsync($"/api/levels/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    public async Task<LevelDto> CreateLevelAsync(string title, string description, int gridWidth, int gridHeight, Dictionary<string, LocalizedLevelText> localizedContent)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/levels", new { title, description, gridWidth, gridHeight, localizedContent }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions))!;
    }

    public async Task<LevelDto?> UpdateLevelAsync(Guid id, string title, string description, int gridWidth, int gridHeight, Dictionary<string, LocalizedLevelText> localizedContent)
    {
        SetAuthHeader();
        var response = await httpClient.PutAsJsonAsync($"/api/levels/{id}", new { title, description, gridWidth, gridHeight, localizedContent }, JsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    public async Task<bool> DeleteLevelAsync(Guid id)
    {
        SetAuthHeader();
        var response = await httpClient.DeleteAsync($"/api/levels/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<bool> ReorderLevelAsync(Guid id, int direction)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync($"/api/levels/{id}/reorder", new { direction });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<LevelDto?> UpdateGridDataAsync(Guid levelId, LevelData levelData)
    {
        SetAuthHeader();
        var response = await httpClient.PutAsJsonAsync($"/api/levels/{levelId}/grid-data", new { levelData }, JsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    // ── Submissions ───────────────────────────────────────────────

    public async Task<List<UserSubmissionOverviewDto>> GetAllSubmissionOverviewsAsync()
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<UserSubmissionOverviewDto>>("/api/submissions/overviews", JsonOptions))!;
    }

    public async Task<List<SubmissionDto>> GetUserSubmissionsAsync(Guid userId)
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<SubmissionDto>>($"/api/submissions/users/{userId}", JsonOptions))!;
    }

    public async Task<string> GetStarterCodeAsync()
    {
        SetAuthHeader();
        var base64 = (await httpClient.GetFromJsonAsync<string>("/api/submissions/starter-code", JsonOptions))!;
        return base64.Base64Decode();
    }

    public async Task<SubmitCodeResponse> SubmitCodeAsync(Guid userId, string code)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/submissions", new { userId, code });

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var errorBody = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
            return new SubmitCodeResponse(false, errorBody?.Errors, null);
        }

        response.EnsureSuccessStatusCode();
        var submission = await response.Content.ReadFromJsonAsync<SubmissionDto>(JsonOptions);
        return new SubmitCodeResponse(true, null, submission);
    }

    // ── Theme ─────────────────────────────────────────────────────

    public async Task<bool> ToggleThemeAsync(Guid userId)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/theme/toggle", new { userId });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    // ── Language ──────────────────────────────────────────────────

    public async Task<bool> ChangeLanguageAsync(Guid userId, string language)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/language/change", new { userId, language });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    // ── Renders ───────────────────────────────────────────────────

    public async Task<List<SubmissionRenderDto>> GetSubmissionRendersAsync(Guid submissionId, Guid levelId)
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<SubmissionRenderDto>>(
            $"/api/submissions/{submissionId}/levels/{levelId}/renders", JsonOptions))!;
    }

    public async Task<bool> DeleteSubmissionAsync(Guid submissionId)
    {
        SetAuthHeader();
        var response = await httpClient.DeleteAsync($"/api/submissions/{submissionId}");
        return response.IsSuccessStatusCode;
    }

    // ── Leaderboard ──────────────────────────────────────────────
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync()
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<LeaderboardEntryDto>>("/api/leaderboard", JsonOptions))!;
    }

    // ── Translation ───────────────────────────────────────────────

    public async Task<LevelTranslationApiResponse?> TranslateLevelAsync(string titleEn, string descriptionEn, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/translate/level", new { titleEn, descriptionEn }, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LevelTranslationApiResponse>(JsonOptions, ct);
    }

    // ── Conductor ─────────────────────────────────────────────────

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        SetAuthHeader();
        return (await httpClient.GetFromJsonAsync<List<ChatMessageDto>>($"/api/conductor/history/{userId}", JsonOptions, ct))!;
    }

    public async Task<(string Reply, bool HistoryCleared)> ChatWithConductorAsync(Guid userId, string message, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await httpClient.PostAsJsonAsync("/api/conductor/chat", new { userId, message }, ct);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<ConductorChatApiResponse>(JsonOptions, ct))!;
        return (result.Reply, result.HistoryCleared);
    }
}

public record SubmitCodeResponse(bool Success, IReadOnlyList<string>? ValidationErrors, SubmissionDto? Submission);

record ValidationErrorResponse(IReadOnlyList<string>? Errors);

record ConductorChatApiResponse(string Reply, bool HistoryCleared);

public record LevelTranslationApiResponse(string TitleNl, string DescriptionNl, string TitleAr, string DescriptionAr);
