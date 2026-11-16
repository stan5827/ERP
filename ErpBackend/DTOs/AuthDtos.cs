namespace ErpBackend.DTOs;

public class ChangePasswordRequest
{
    public string? OldPassword { get; set; }
    public string NewPassword { get; set; } = null!;
}

public class AdminResetRequest
{
    public string Secret { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
