using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetroMania.Application.Auth.Commands;
using MetroMania.Application.Auth.Queries;
using MetroMania.Application.DTOs;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;

namespace MetroMania.Web.Services;

public class MetroManiaApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Auth ──────────────────────────────────────────────────────

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
        return (await httpClient.GetFromJsonAsync<List<UserDto>>("/api/users", JsonOptions))!;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"/api/users/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
    }

    public async Task<bool> ApproveUserAsync(Guid userId, ApprovalStatus newStatus)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/users/{userId}/approve", new { newStatus });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var response = await httpClient.DeleteAsync($"/api/users/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    // ── Levels ────────────────────────────────────────────────────

    public async Task<List<LevelDto>> GetAllLevelsAsync()
    {
        return (await httpClient.GetFromJsonAsync<List<LevelDto>>("/api/levels", JsonOptions))!;
    }

    public async Task<LevelDto?> GetLevelAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"/api/levels/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    public async Task<LevelDto> CreateLevelAsync(string title, string description, int gridWidth, int gridHeight)
    {
        var response = await httpClient.PostAsJsonAsync("/api/levels", new { title, description, gridWidth, gridHeight });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions))!;
    }

    public async Task<LevelDto?> UpdateLevelAsync(Guid id, string title, string description, int gridWidth, int gridHeight)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/levels/{id}", new { title, description, gridWidth, gridHeight });
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    public async Task<bool> DeleteLevelAsync(Guid id)
    {
        var response = await httpClient.DeleteAsync($"/api/levels/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<bool> ReorderLevelAsync(Guid id, int direction)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/levels/{id}/reorder", new { direction });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    public async Task<LevelDto?> UpdateGridDataAsync(Guid levelId, LevelData levelData)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/levels/{levelId}/grid-data", new { levelData }, JsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LevelDto>(JsonOptions);
    }

    // ── Submissions ───────────────────────────────────────────────

    public async Task<List<UserSubmissionOverviewDto>> GetAllSubmissionOverviewsAsync()
    {
        return (await httpClient.GetFromJsonAsync<List<UserSubmissionOverviewDto>>("/api/submissions/overviews", JsonOptions))!;
    }

    public async Task<List<SubmissionDto>> GetUserSubmissionsAsync(Guid userId)
    {
        return (await httpClient.GetFromJsonAsync<List<SubmissionDto>>($"/api/submissions/users/{userId}", JsonOptions))!;
    }

    public async Task<SubmissionDto> SubmitCodeAsync(Guid userId, string code)
    {
        var response = await httpClient.PostAsJsonAsync("/api/submissions", new { userId, code });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmissionDto>(JsonOptions))!;
    }

    // ── Theme ─────────────────────────────────────────────────────

    public async Task<bool> ToggleThemeAsync(Guid userId)
    {
        var response = await httpClient.PostAsJsonAsync("/api/theme/toggle", new { userId });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }

    // ── Language ──────────────────────────────────────────────────

    public async Task<bool> ChangeLanguageAsync(Guid userId, string language)
    {
        var response = await httpClient.PostAsJsonAsync("/api/language/change", new { userId, language });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(JsonOptions);
    }
}
