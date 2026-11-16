using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponse Fail(string message) => new() { Success = false, Message = message };
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int? TotalCount { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null, int? totalCount = null) =>
        new() { Success = true, Data = data, Message = message, TotalCount = totalCount };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public class BadRequest
{
    public string Message { get; set; } = string.Empty;
}
