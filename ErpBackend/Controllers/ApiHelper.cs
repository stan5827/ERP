using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

public static class ApiHelper
{
    public static ApiResponse Fail(string message) => ApiResponse.Fail(message);
    public static ApiResponse<T> Fail<T>(string message) => ApiResponse<T>.Fail(message);
    public static ApiResponse<T> FailOf<T>(string message) => ApiResponse<T>.Fail(message);
    public static ApiResponse Success(string? message = null) => ApiResponse.Ok(message);
    public static ApiResponse<T> Success<T>(T data, string? message = null) => ApiResponse<T>.Ok(data, message);
}
