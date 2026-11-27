using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.Api;

public sealed record ApiStudentDto(
    string Id,
    string StudentNumber,
    string FullName,
    string? Contact,
    bool IsEligible);

public sealed record ApiEquipmentDto(
    string Id,
    string EquipmentCode,
    string Name,
    string? Description,
    bool IsAvailable);

public sealed record ApiBorrowingDto(
    string Id,
    string StudentId,
    string EquipmentId,
    string BorrowedDate,
    string ExpectedReturnDate,
    string? ReturnedDate,
    string Status,
    string StudentNumber,
    string StudentName,
    string EquipmentCode,
    string EquipmentName);

public sealed record ApiCheckoutResponseDto(List<ApiBorrowingDto> Borrowings);

public sealed record ApiErrorBody(string? Code, string? Message);

public sealed class ApiClientException : Exception
{
    public bool IsUnreachable { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? ErrorCode { get; }
    public bool IsUnknownOutcome { get; }

    public ApiClientException(
        string message,
        bool isUnreachable = false,
        HttpStatusCode? statusCode = null,
        string? errorCode = null,
        bool isUnknownOutcome = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        IsUnreachable = isUnreachable;
        StatusCode = statusCode;
        ErrorCode = errorCode;
        IsUnknownOutcome = isUnknownOutcome;
    }
}

public sealed class BorrowingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public BorrowingApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ApiStudentDto>> GetStudentsAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "/api/v1/students"
            : $"/api/v1/students?search={Uri.EscapeDataString(search.Trim())}";
        return await GetJsonAsync<List<ApiStudentDto>>(url, ct);
    }

    public Task<ApiStudentDto> GetStudentByIdAsync(string id, CancellationToken ct = default) =>
        GetJsonAsync<ApiStudentDto>($"/api/v1/students/{Uri.EscapeDataString(id)}", ct);

    public Task<ApiStudentDto> CreateStudentAsync(string studentNumber, string fullName, string? contact, CancellationToken ct = default) =>
        PostJsonAsync<object, ApiStudentDto>("/api/v1/students", new { studentNumber, fullName, contact }, ct, isMutation: true);

    public Task<ApiStudentDto> UpdateStudentAsync(string id, string studentNumber, string fullName, string? contact, CancellationToken ct = default) =>
        PutJsonAsync<object, ApiStudentDto>($"/api/v1/students/{Uri.EscapeDataString(id)}", new { studentNumber, fullName, contact }, ct, isMutation: true);

    public Task<ApiStudentDto> SetStudentEligibilityAsync(string id, bool isEligible, CancellationToken ct = default) =>
        PatchJsonAsync<object, ApiStudentDto>($"/api/v1/students/{Uri.EscapeDataString(id)}/eligibility", new { isEligible }, ct, isMutation: true);

    public Task<List<ApiBorrowingDto>> GetStudentBorrowingsAsync(string studentId, CancellationToken ct = default) =>
        GetJsonAsync<List<ApiBorrowingDto>>($"/api/v1/students/{Uri.EscapeDataString(studentId)}/borrowings", ct);

    public Task<List<ApiEquipmentDto>> GetEquipmentAsync(bool? availableOnly = null, CancellationToken ct = default)
    {
        var url = availableOnly is true ? "/api/v1/equipment?available=true" : "/api/v1/equipment";
        return GetJsonAsync<List<ApiEquipmentDto>>(url, ct);
    }

    public Task<ApiEquipmentDto> GetEquipmentByIdAsync(string id, CancellationToken ct = default) =>
        GetJsonAsync<ApiEquipmentDto>($"/api/v1/equipment/{Uri.EscapeDataString(id)}", ct);

    public Task<ApiEquipmentDto> CreateEquipmentAsync(string equipmentCode, string name, string? description, CancellationToken ct = default) =>
        PostJsonAsync<object, ApiEquipmentDto>("/api/v1/equipment", new { equipmentCode, name, description }, ct, isMutation: true);

    public Task<ApiEquipmentDto> UpdateEquipmentAsync(string id, string equipmentCode, string name, string? description, CancellationToken ct = default) =>
        PutJsonAsync<object, ApiEquipmentDto>($"/api/v1/equipment/{Uri.EscapeDataString(id)}", new { equipmentCode, name, description }, ct, isMutation: true);

    public Task<List<ApiBorrowingDto>> GetEquipmentBorrowingsAsync(string equipmentId, CancellationToken ct = default) =>
        GetJsonAsync<List<ApiBorrowingDto>>($"/api/v1/equipment/{Uri.EscapeDataString(equipmentId)}/borrowings", ct);

    public Task<List<ApiBorrowingDto>> SearchBorrowingsAsync(
        string? search = null,
        string? status = null,
        string? sort = null,
        string? direction = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(sort)) query.Add($"sort={Uri.EscapeDataString(sort)}");
        if (!string.IsNullOrWhiteSpace(direction)) query.Add($"direction={Uri.EscapeDataString(direction)}");
        var url = query.Count == 0 ? "/api/v1/borrowings" : "/api/v1/borrowings?" + string.Join("&", query);
        return GetJsonAsync<List<ApiBorrowingDto>>(url, ct);
    }

    public Task<ApiBorrowingDto> BorrowSingleAsync(string studentId, string equipmentId, string expectedReturnDate, CancellationToken ct = default) =>
        PostJsonAsync<object, ApiBorrowingDto>("/api/v1/borrowings", new { studentId, equipmentId, expectedReturnDate }, ct, isMutation: true);

    public async Task<List<ApiBorrowingDto>> CheckoutMultipleAsync(
        string studentId,
        IReadOnlyList<string> equipmentIds,
        string expectedReturnDate,
        CancellationToken ct = default)
    {
        var resp = await PostJsonAsync<object, ApiCheckoutResponseDto>(
            "/api/v1/checkout-requests",
            new { studentId, equipmentIds, expectedReturnDate },
            ct,
            isMutation: true);
        return resp.Borrowings ?? new List<ApiBorrowingDto>();
    }

    public Task<ApiBorrowingDto> ReturnBorrowingAsync(string borrowingId, CancellationToken ct = default) =>
        PostJsonAsync<object, ApiBorrowingDto>($"/api/v1/borrowings/{Uri.EscapeDataString(borrowingId)}/return", new { }, ct, isMutation: true);

    private async Task<T> GetJsonAsync<T>(string relativeUrl, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl, ct);
            return await ReadResponseAsync<T>(response, isMutation: false, ct);
        }
        catch (ApiClientException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                "Cannot reach the API server. Make sure the API is running and check the configured API base URL.",
                isUnreachable: true,
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiClientException(
                "The request to the API timed out. Check whether the API server is reachable and responsive.",
                isUnreachable: true,
                innerException: ex);
        }
    }

    private async Task<TOut> PostJsonAsync<TIn, TOut>(string relativeUrl, TIn payload, CancellationToken ct, bool isMutation)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(relativeUrl, payload, JsonOptions, ct);
            return await ReadResponseAsync<TOut>(response, isMutation, ct);
        }
        catch (ApiClientException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                isMutation
                    ? "Connection was lost before a response was received. Outcome is unknown; refresh state before retrying."
                    : "Cannot reach the API server. Make sure the API is running and check the configured API base URL.",
                isUnreachable: !isMutation,
                isUnknownOutcome: isMutation,
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiClientException(
                isMutation
                    ? "Request timed out before a response was received. Outcome is unknown; refresh state before retrying."
                    : "The request to the API timed out.",
                isUnreachable: !isMutation,
                isUnknownOutcome: isMutation,
                innerException: ex);
        }
    }

    private async Task<TOut> PutJsonAsync<TIn, TOut>(string relativeUrl, TIn payload, CancellationToken ct, bool isMutation)
    {
        try
        {
            using var response = await _http.PutAsJsonAsync(relativeUrl, payload, JsonOptions, ct);
            return await ReadResponseAsync<TOut>(response, isMutation, ct);
        }
        catch (ApiClientException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                "Connection was lost before a response was received. Outcome is unknown; refresh state before retrying.",
                isUnknownOutcome: true,
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiClientException(
                "Request timed out before a response was received. Outcome is unknown; refresh state before retrying.",
                isUnknownOutcome: true,
                innerException: ex);
        }
    }

    private async Task<TOut> PatchJsonAsync<TIn, TOut>(string relativeUrl, TIn payload, CancellationToken ct, bool isMutation)
    {
        try
        {
            using var response = await _http.PatchAsJsonAsync(relativeUrl, payload, JsonOptions, ct);
            return await ReadResponseAsync<TOut>(response, isMutation, ct);
        }
        catch (ApiClientException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                "Connection was lost before a response was received. Outcome is unknown; refresh state before retrying.",
                isUnknownOutcome: true,
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiClientException(
                "Request timed out before a response was received. Outcome is unknown; refresh state before retrying.",
                isUnknownOutcome: true,
                innerException: ex);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, bool isMutation, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            var rawBody = await response.Content.ReadAsStringAsync(ct);
            ApiErrorBody? parsed = null;
            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                try { parsed = JsonSerializer.Deserialize<ApiErrorBody>(rawBody, JsonOptions); }
                catch { /* ignore non-JSON error body */ }
            }

            var message = !string.IsNullOrWhiteSpace(parsed?.Message)
                ? parsed!.Message!
                : (int)status >= 500
                    ? $"Server error ({(int)status}): the API encountered a server or database error."
                    : $"Request failed with status {(int)status} ({status}).";

            if ((int)status >= 500 && string.Equals(parsed?.Code, "outcome_unknown", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiClientException(message, statusCode: status, errorCode: parsed?.Code, isUnknownOutcome: true);
            }

            throw new ApiClientException(message, isUnreachable: false, statusCode: status, errorCode: parsed?.Code);
        }

        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        if (data is null)
        {
            throw new ApiClientException(
                "The API returned an empty response body.",
                statusCode: response.StatusCode,
                isUnknownOutcome: isMutation);
        }

        return data;
    }
}