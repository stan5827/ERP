namespace ErpBackend.DTOs;

public class MrpRunRequest
{
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string? RunType { get; set; }
}
