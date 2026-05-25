using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class SupabaseMemoStore(AppConfig config, SupabaseAuthService? authService = null)
{
    private readonly HttpClient _http = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<List<MemoItem>> LoadMemosAsync(AuthTokenStoreModel token)
    {
        await EnsureTokenAsync(token);
        var url = $"{BaseRestUrl}/memos?select=*&is_deleted=eq.false&order=updated_at.desc";
        using var request = CreateRequest(HttpMethod.Get, url, token);
        using var response = await SendWithRefreshRetryAsync(request, token, () => CreateRequest(HttpMethod.Get, url, token));
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseMemoDto>>(JsonOptions) ?? [];
        return rows.Select(ToMemoItem).ToList();
    }

    public async Task<SaveResult> InsertAsync(MemoItem memo, AuthTokenStoreModel token)
    {
        try
        {
            var body = SupabaseMemoDto.FromMemo(memo);
            body.Id = null; // Let Supabase generate the permanent id for first insert.
            body.OwnerId = token.UserId;
            body.Version = 1;

            await EnsureTokenAsync(token);
            var url = $"{BaseRestUrl}/memos";
            using var request = CreateRequest(HttpMethod.Post, url, token);
            request.Headers.Add("Prefer", "return=representation");
            request.Content = JsonContent.Create(body, options: JsonOptions);

            using var response = await SendWithRefreshRetryAsync(request, token, () =>
            {
                var retry = CreateRequest(HttpMethod.Post, url, token);
                retry.Headers.Add("Prefer", "return=representation");
                retry.Content = JsonContent.Create(body, options: JsonOptions);
                return retry;
            });
            if (!response.IsSuccessStatusCode)
            {
                return SaveResult.Offline(await response.Content.ReadAsStringAsync());
            }

            var returned = await ReadSingleMemoAsync(response);
            return returned is null ? SaveResult.Failed("Insert succeeded but Supabase returned no row.") : SaveResult.Ok(returned);
        }
        catch (HttpRequestException ex)
        {
            return SaveResult.Offline(ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return SaveResult.Offline(ex.Message);
        }
    }

    public async Task<SaveResult> UpdateAsync(MemoItem memo, AuthTokenStoreModel token)
    {
        try
        {
            await EnsureTokenAsync(token);
            var url = $"{BaseRestUrl}/memos?id=eq.{memo.Id}&version=eq.{memo.BaseVersion}";
            var body = SupabaseMemoDto.FromMemo(memo);
            body.Id = null;
            body.OwnerId = null;
            body.CreatedAt = null;
            body.UpdatedAt = null;
            body.Version = memo.BaseVersion + 1;

            using var request = CreateRequest(HttpMethod.Patch, url, token);
            request.Headers.Add("Prefer", "return=representation");
            request.Content = JsonContent.Create(body, options: JsonOptions);

            using var response = await SendWithRefreshRetryAsync(request, token, () =>
            {
                var retry = CreateRequest(HttpMethod.Patch, url, token);
                retry.Headers.Add("Prefer", "return=representation");
                retry.Content = JsonContent.Create(body, options: JsonOptions);
                return retry;
            });
            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode == HttpStatusCode.Conflict
                    ? SaveResult.Conflict(await response.Content.ReadAsStringAsync())
                    : SaveResult.Offline(await response.Content.ReadAsStringAsync());
            }

            var returned = await ReadSingleMemoAsync(response);
            return returned is null ? SaveResult.Conflict("No row matched id and base version.") : SaveResult.Ok(returned);
        }
        catch (HttpRequestException ex)
        {
            return SaveResult.Offline(ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return SaveResult.Offline(ex.Message);
        }
    }

    public async Task<SaveResult> SoftDeleteAsync(MemoItem memo, AuthTokenStoreModel token)
    {
        memo.IsDeleted = true;
        return await UpdateAsync(memo, token);
    }

    private string BaseRestUrl => $"{config.SupabaseUrl.TrimEnd('/')}/rest/v1";

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, AuthTokenStoreModel token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", config.SupabaseAnonKey);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        return request;
    }

    private async Task EnsureTokenAsync(AuthTokenStoreModel token)
    {
        if (authService is not null)
        {
            await authService.EnsureValidTokenAsync(token);
        }
    }

    private async Task<HttpResponseMessage> SendWithRefreshRetryAsync(
        HttpRequestMessage request,
        AuthTokenStoreModel token,
        Func<HttpRequestMessage> retryFactory)
    {
        var response = await _http.SendAsync(request);
        if (!await LooksLikeExpiredJwtAsync(response) || authService is null)
        {
            return response;
        }

        response.Dispose();
        await authService.RefreshAsync(token);
        using var retry = retryFactory();
        return await _http.SendAsync(retry);
    }

    private static async Task<bool> LooksLikeExpiredJwtAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync();
        return body.Contains("JWT expired", StringComparison.OrdinalIgnoreCase)
            || body.Contains("expired", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<MemoItem?> ReadSingleMemoAsync(HttpResponseMessage response)
    {
        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseMemoDto>>(JsonOptions);
        return rows is { Count: > 0 } ? ToMemoItem(rows[0]) : null;
    }

    private static MemoItem ToMemoItem(SupabaseMemoDto dto) => new()
    {
        Id = dto.Id ?? Guid.NewGuid(),
        OwnerId = dto.OwnerId ?? Guid.Empty,
        Title = dto.Title ?? "Memo",
        Content = dto.Content ?? "",
        IsTopMost = dto.IsTopMost,
        LeftPos = dto.LeftPos,
        TopPos = dto.TopPos,
        Width = dto.Width,
        Height = dto.Height,
        IsOpen = dto.IsOpen,
        Version = dto.Version,
        BaseVersion = dto.Version,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
        UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow,
        SyncState = SyncState.Synced,
        IsLocalOnly = false
    };

    private sealed class SupabaseMemoDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }
        [JsonPropertyName("owner_id")]
        public Guid? OwnerId { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("is_topmost")]
        public bool IsTopMost { get; set; } = true;
        [JsonPropertyName("left_pos")]
        public double LeftPos { get; set; } = 100;
        [JsonPropertyName("top_pos")]
        public double TopPos { get; set; } = 100;
        [JsonPropertyName("width")]
        public double Width { get; set; } = 260;
        [JsonPropertyName("height")]
        public double Height { get; set; } = 380;
        [JsonPropertyName("is_open")]
        public bool IsOpen { get; set; } = true;
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;
        [JsonPropertyName("is_deleted")]
        public bool IsDeleted { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public static SupabaseMemoDto FromMemo(MemoItem memo) => new()
        {
            Id = memo.Id,
            OwnerId = memo.OwnerId,
            Title = string.IsNullOrWhiteSpace(memo.Title) ? "Untitled" : memo.Title,
            Content = memo.Content,
            IsTopMost = memo.IsTopMost,
            LeftPos = memo.LeftPos,
            TopPos = memo.TopPos,
            Width = memo.Width,
            Height = memo.Height,
            IsOpen = memo.IsOpen,
            Version = memo.Version,
            IsDeleted = memo.IsDeleted,
            CreatedAt = memo.CreatedAt,
            UpdatedAt = memo.UpdatedAt
        };
    }
}
