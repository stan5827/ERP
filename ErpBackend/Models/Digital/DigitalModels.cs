using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Digital;

public class GhgEmissionSource
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string SourceCode { get; set; } = null!;
    [Required, MaxLength(200)] public string SourceName { get; set; } = null!;
    public Guid? FacilityId { get; set; }
    [MaxLength(20)] public string Scope { get; set; } = "scope1";
    public Guid? MachineId { get; set; }
    [MaxLength(50)] public string? EnergyType { get; set; }
    [Column(TypeName = "decimal(12,6)")] public decimal EmissionFactor { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal? GwpValue { get; set; }
}

public class GhgEmissionRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    [MaxLength(7)] public string Period { get; set; } = null!;
    [Column(TypeName = "decimal(18,4)")] public decimal ActivityData { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal CalculatedEmissions { get; set; }
    [MaxLength(50)] public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CarbonFootprintMapping
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GhgEmissionRecordId { get; set; }
    [MaxLength(50)] public string? SourceDocType { get; set; }
    public Guid? SourceDocId { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal ActivityData { get; set; }
    [Column(TypeName = "decimal(12,6)")] public decimal EmissionFactor { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal CalculatedCo2e { get; set; }
    [MaxLength(20)] public string MappingStatus { get; set; } = "auto";
}

public class AiInsight
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string InsightType { get; set; } = null!;
    [MaxLength(200)] public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? DataSources { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? ConfidenceScore { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiAnomalyDetection
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public string TargetType { get; set; } = null!;
    public Guid? TargetId { get; set; }
    [MaxLength(100)] public string? AnomalyLabel { get; set; }
    public string? AiExplanation { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? ConfidenceScore { get; set; }
    [MaxLength(30)] public string? HumanReviewResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiFormGuidance
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string? Scenario { get; set; }
    public string? UserInput { get; set; }
    public string? MappedFields { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? ConfidenceScore { get; set; }
    public bool? UserAccepted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LineConfig
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public string? ChannelId { get; set; }
    [MaxLength(200)] public string? ChannelSecret { get; set; }
    [MaxLength(500)] public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LineUserMapping
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    [Required, MaxLength(50)] public string LineUserId { get; set; } = null!;
    [MaxLength(100)] public string? LineDisplayName { get; set; }
    public DateTime BoundAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class LineNotificationLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public string NotificationType { get; set; } = null!;
    [MaxLength(50)] public string? TargetLineUserId { get; set; }
    [MaxLength(50)] public string? TargetGroupId { get; set; }
    public string? MessageContent { get; set; }
    [MaxLength(20)] public string SendStatus { get; set; } = "pending";
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LineApprovalToken
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string Token { get; set; } = null!;
    [MaxLength(50)] public string DocType { get; set; } = null!;
    public Guid DocId { get; set; }
    [MaxLength(20)] public string Action { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiVisionCapture
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string CaptureNumber { get; set; } = null!;
    public Guid SkuId { get; set; }
    [MaxLength(50)] public string? WarehouseCode { get; set; }
    [MaxLength(50)] public string? BinLocationCode { get; set; }
    [MaxLength(50)] public string? RecognizedSkuNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AiCountedQty { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal ConfidenceScore { get; set; }
    [MaxLength(500)] public string? ScreenshotPath { get; set; }
    [MaxLength(50)] public string? DeviceId { get; set; }
    [MaxLength(50)] public string? CaptureType { get; set; } = "camera";
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public bool IsReviewed { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiStocktakeAnalysis
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StocktakeOrderId { get; set; }
    public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(50)] public string? BinLocationCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SystemQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AiCapturedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Difference { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? DifferenceAmount { get; set; }
    [MaxLength(200)] public string? AiReasoning { get; set; }
    [MaxLength(20)] public string? AnomalyTag { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? AiConfidence { get; set; }
    [MaxLength(20)] public string ReviewStatus { get; set; } = "pending";
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiStocktakeMission
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string MissionNumber { get; set; } = null!;
    [MaxLength(50)] public string MissionType { get; set; } = "auto";
    public Guid? WarehouseId { get; set; }
    [MaxLength(50)] public string? TargetZone { get; set; }
    [MaxLength(50)] public string? DeviceType { get; set; } = "AMR";
    [MaxLength(500)] public string? PathCode { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    [MaxLength(20)] public string Frequency { get; set; } = "daily";
    public bool IsActive { get; set; } = true;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EsgDashboardConfig
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string ConfigKey { get; set; } = null!;
    [MaxLength(200)] public string? KpiName { get; set; }
    [MaxLength(50)] public string? KpiCategory { get; set; } = "environmental";
    [MaxLength(20)] public string? VisualizationType { get; set; } = "line_chart";
    [MaxLength(50)] public string? TargetAudience { get; set; } = "executive";
    [Column(TypeName = "decimal(14,4)")] public decimal? AlertThreshold { get; set; }
    [MaxLength(20)] public string? AlertLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
