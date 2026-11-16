using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VoucherType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountsPayables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierInvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseInvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastAgingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThreeWayMatchResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllowanceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    DeductionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsPayables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SuccessRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Duration = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Context = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiAnomalyDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AiExplanation = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HumanReviewResult = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnomalyDetections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiFormGuidances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserInput = table.Column<string>(type: "text", nullable: true),
                    MappedFields = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    UserAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFormGuidances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InsightType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    DataSources = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiStocktakeAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StocktakeOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BinLocationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SystemQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AiCapturedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DifferenceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    AiReasoning = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AnomalyTag = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AiConfidence = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ReviewStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiStocktakeAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiStocktakeMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MissionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PathCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiStocktakeMissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiVisionCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BinLocationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RecognizedSkuNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AiCountedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ScreenshotPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CaptureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReviewed = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiVisionCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    PublishDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TargetAudience = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsUrgent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApsScheduleResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: true),
                    ResourceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MrpPlannedStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ScheduleVersion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApsScheduleResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsrsTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Lpn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErpSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AsrsCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsrsTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepreciationMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefaultUsefulLife = table.Column<int>(type: "integer", nullable: false),
                    DefaultResidualRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetDisposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisposalReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BookValue = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DisposalIncome = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DisposalGainLoss = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnvironmentalCertNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDisposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetStocktakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StocktakeNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StocktakeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StocktakeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStocktakes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoReplenishments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Rop = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SuggestedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RelatedPrId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoReplenishments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BranchName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BarcodeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BarcodeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RefId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RefName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Qty = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastScanTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BarcodeScanHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BarcodeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeScanHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BomPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AncestorSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    DescendantSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Distance = table.Column<int>(type: "integer", nullable: false),
                    PathId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomPaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BomCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarBuckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BucketCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BucketType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BucketStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BucketEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarBuckets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarCategories", x => x.Id);
                    table.UniqueConstraint("AK_CalendarCategories_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CalendarShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SharedWithDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    SharedWithRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permission = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CanSeeDetails = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapexBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlannedPurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UsefulLife = table.Column<int>(type: "integer", nullable: false),
                    FundingSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EstimatedMonthlyDepreciation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PlannedCommissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Roi = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    PaybackPeriodYears = table.Column<decimal>(type: "numeric(5,1)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapexBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarbonFootprintMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GhgEmissionRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceDocId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityData = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    EmissionFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    CalculatedCo2e = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MappingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonFootprintMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HolidayType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompletionOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletionNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QaStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ElementName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ElementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GLAccountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GLAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostElements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossDeptTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InitiatedDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Assistants = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedCompleteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualCompleteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SatisfactionScore = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossDeptTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CurrencyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactTitle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FixedDiscount = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    BillingDay = table.Column<int>(type: "integer", nullable: false),
                    CollectionDay = table.Column<int>(type: "integer", nullable: false),
                    InvoiceMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreditStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompanyType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fax = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaxCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PriceLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SalesTerritory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SalesPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LeadSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChurnRisk = table.Column<bool>(type: "boolean", nullable: true),
                    LastPurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SatisfactionScore = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ForecastQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Accuracy = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ActualQty = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PageContent = table.Column<string>(type: "text", nullable: true),
                    SopsJson = table.Column<string>(type: "text", nullable: true),
                    KpiDataJson = table.Column<string>(type: "text", nullable: true),
                    QuickLinksJson = table.Column<string>(type: "text", nullable: true),
                    PrimaryContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeptCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeptName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeptType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ParentDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ManagerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCostCenter = table.Column<bool>(type: "boolean", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    BudgetUsed = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDeptId",
                        column: x => x.ParentDeptId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false),
                    CanDownload = table.Column<bool>(type: "boolean", nullable: false),
                    CanPrint = table.Column<bool>(type: "boolean", nullable: false),
                    WatermarkEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DocName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedSkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEcnId = table.Column<Guid>(type: "uuid", nullable: true),
                    LifecycleStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfidentialityLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ecrs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcrNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetBomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ecrs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RelatedRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EsgDashboardConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KpiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    KpiCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VisualizationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TargetAudience = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AlertThreshold = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    AlertLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EsgDashboardConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ToCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RateType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpiryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DaysRemaining = table.Column<int>(type: "integer", nullable: false),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpiryAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GhgEmissionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ActivityData = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CalculatedEmissions = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SourceDocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhgEmissionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GhgEmissionSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnergyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmissionFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    GwpValue = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhgEmissionSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GLAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCashAccount = table.Column<bool>(type: "boolean", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GLAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GLAccounts_GLAccounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Guarantees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuaranteeNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GuaranteeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Beneficiary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuaranteeAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FeeRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FeePaid = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    RelatedPoId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedContractNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guarantees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvestmentCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    InvestmentCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Fees = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AccountingClassification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentMarketValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnrealizedGainLoss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DividendIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DisposalGainLoss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShareholdingPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpqcReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InspectionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InspectionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CpkValue = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    AbnormalDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsLineStopped = table.Column<bool>(type: "boolean", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpqcReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobGrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    GradeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinSalary = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    MaxSalary = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OvertimeRate = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    TravelLimit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ApprovalLimit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KpiCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KpiName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Formula = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    WarningThreshold = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    CriticalThreshold = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    CalculationFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaborBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentHeadcount = table.Column<int>(type: "integer", nullable: false),
                    CurrentSalaryTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineApprovalTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineApprovalTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChannelSecret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetLineUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetGroupId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MessageContent = table.Column<string>(type: "text", nullable: true),
                    SendStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineNotificationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineUserMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LineDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BoundAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineUserMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BranchName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LoanAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    InterestType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LoanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CollateralInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Guarantor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NextInterestAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NextInterestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FrequencyDays = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckpointList = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenancePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PartsReplaced = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LaborHours = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PartsCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCostForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCostForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReturnedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialSupplements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplementNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSupplements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Equipment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CollectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OutstandingAR = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MpsExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MpsHeaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpsExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MpsHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MpsNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PlantCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlanName = table.Column<string>(type: "text", nullable: false),
                    HorizonStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorizonEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeBucketType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BaseOn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BackwardConsumptionDays = table.Column<int>(type: "integer", nullable: false),
                    ForwardConsumptionDays = table.Column<int>(type: "integer", nullable: false),
                    ConsumptionStrategy = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpsHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MrpRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RunType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlanningHorizonStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlanningHorizonEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalItemsProcessed = table.Column<int>(type: "integer", nullable: false),
                    PlannedOrdersGenerated = table.Column<int>(type: "integer", nullable: false),
                    PurchaseRequisitionsGenerated = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TriggeredBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MrpRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NonConformanceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NcrNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DefectQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DefectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Disposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CostImpact = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformanceReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SetupTime = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    RunTime = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    CostPerHour = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsOutsourced = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OqcReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackagingCheck = table.Column<bool>(type: "boolean", nullable: false),
                    ReliabilityTestResult = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FinalDecision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OqcReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PermissionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Permissions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PortalMessageThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RelatedDocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedDocId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalMessageThreads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ScheduleType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorizonStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorizonEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StandardPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSpecifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TempSkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductSeries = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TechnicalParams = table.Column<string>(type: "text", nullable: true),
                    PackagingSpec = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificationRequirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSpecifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RndLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperimentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ResearcherId = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TestConditions = table.Column<string>(type: "text", nullable: true),
                    Results = table.Column<string>(type: "text", nullable: true),
                    Conclusion = table.Column<string>(type: "text", nullable: true),
                    ConfidentialityLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RndProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProjectManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TargetSampleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TargetProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedBudget = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TechnicalFeasibility = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ComplianceStandards = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MarketResearchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SalesPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerTier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SalesTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredQty = table.Column<int>(type: "integer", nullable: false),
                    MaterialSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PilotRunYield = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DfmFeedback = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DispositionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CostAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StocktakeNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    StocktakeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubstituteGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubstitutionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllowAutoSubstitute = table.Column<bool>(type: "boolean", nullable: false),
                    IsBidirectional = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstituteGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAsns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAsns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierEsgQuestionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QuestionnaireType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    ResponsesJson = table.Column<string>(type: "text", nullable: true),
                    EnvironmentalCertifications = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SocialInitiatives = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GovernancePractices = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    OverallRating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierEsgQuestionnaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPerformanceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    OtifRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ppm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OrderAccuracyRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ResponseTimeHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Rating = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPerformanceScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    Moq = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodMonths = table.Column<int>(type: "integer", nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveryScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CostScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ServiceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Rating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: true),
                    DataType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Link = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransferOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreasuryTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FromBankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToBankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    ExchangeGainLoss = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreasuryTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UomGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UomGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UomName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UomNameEn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UomType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDecimal = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uoms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ThemeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomPrimaryColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CustomAccentColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordHistoryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WorkCenterName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Efficiency = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountsPayableItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountsPayableId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsPayableItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountsPayableItems_AccountsPayables_AccountsPayableId",
                        column: x => x.AccountsPayableId,
                        principalTable: "AccountsPayables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToolUsed = table.Column<string>(type: "text", nullable: true),
                    Output = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Dependencies = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentTasks_AgentExecutions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "AgentExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementReadLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementReadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementReadLogs_Announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustodianDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustodianEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcquisitionCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AcquisitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsefulLife = table.Column<int>(type: "integer", nullable: false),
                    ResidualValue = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DepreciationMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetStocktakeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetStocktakeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ActualStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LabelVerified = table.Column<bool>(type: "boolean", nullable: false),
                    BarcodeScanned = table.Column<bool>(type: "boolean", nullable: false),
                    CustodianConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStocktakeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetStocktakeItems_AssetStocktakes_AssetStocktakeId",
                        column: x => x.AssetStocktakeId,
                        principalTable: "AssetStocktakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScrapRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    SubstituteGroupId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsPhantom = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BomItems_Boms_BomId",
                        column: x => x.BomId,
                        principalTable: "Boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VersionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetVersions_BudgetYears_BudgetYearId",
                        column: x => x.BudgetYearId,
                        principalTable: "BudgetYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrossDeptTaskReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyContent = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossDeptTaskReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossDeptTaskReplies_CrossDeptTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CrossDeptTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HandlingFee = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReceipts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerPoNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SalesAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseSalesAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ecns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EcrId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeDescription = table.Column<string>(type: "text", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ecns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ecns_Ecrs_EcrId",
                        column: x => x.EcrId,
                        principalTable: "Ecrs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseBudgetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AnnualBudget = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month01 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month02 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month03 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month04 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month05 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month06 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month07 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month08 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month09 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month10 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month11 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Month12 = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PriorYearActual = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ControlLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBudgetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseBudgetItems_ExpenseBudgets_ExpenseBudgetId",
                        column: x => x.ExpenseBudgetId,
                        principalTable: "ExpenseBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MarketValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnrealizedGainLoss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentValuations_Investments_InvestmentId",
                        column: x => x.InvestmentId,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IpqcReportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpqcReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    KpcItem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    LowerLimit = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    MeasuredValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpqcReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpqcReportItems_IpqcReports_IpqcReportId",
                        column: x => x.IpqcReportId,
                        principalTable: "IpqcReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalAuthorities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalAuthorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalAuthorities_JobGrades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TitleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicableDeptType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsSupervisor = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitles_JobGrades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KpiValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    AchievementRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RawData = table.Column<string>(type: "text", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiValues_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaborBudgetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaborBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AdditionalHeadcount = table.Column<int>(type: "integer", nullable: false),
                    PlannedHireMonth = table.Column<int>(type: "integer", nullable: true),
                    EstimatedStartingSalary = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EstimatedOvertimeBudget = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EstimatedInsuranceBudget = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    RecruitmentBudget = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborBudgetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborBudgetItems_LaborBudgets_LaborBudgetId",
                        column: x => x.LaborBudgetId,
                        principalTable: "LaborBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanRepaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRepaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanRepaymentSchedules_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCostForecastItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCostForecastId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    MainSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ForecastDemandQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ExpectedPriceChange = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    EstimatedTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RiskNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCostForecastItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialCostForecastItems_MaterialCostForecasts_MaterialCos~",
                        column: x => x.MaterialCostForecastId,
                        principalTable: "MaterialCostForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReturnQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialReturnItems_MaterialReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "MaterialReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialSupplementItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplementId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomStandardQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualConsumedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SupplementQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSupplementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialSupplementItems_MaterialSupplements_SupplementId",
                        column: x => x.SupplementId,
                        principalTable: "MaterialSupplements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: true),
                    RecurrenceParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Participants = table.Column<string>(type: "text", nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Reminders = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SyncToOutlook = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_CalendarCategories_CategoryCode",
                        column: x => x.CategoryCode,
                        principalTable: "CalendarCategories",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_MeetingRooms_MeetingRoomId",
                        column: x => x.MeetingRoomId,
                        principalTable: "MeetingRooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MeetingReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReserverId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attendees = table.Column<string>(type: "text", nullable: true),
                    CateringRequired = table.Column<bool>(type: "boolean", nullable: false),
                    EquipmentRequired = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingReservations_MeetingRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "MeetingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountsPayableId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ExchangeGainLoss = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BankReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_PaymentBatches_PaymentBatchId",
                        column: x => x.PaymentBatchId,
                        principalTable: "PaymentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PortalMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalMessages_PortalMessageThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "PortalMessageThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstraintType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleConstraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleConstraints_ProductionSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MassProductionTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBomId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassProductionTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassProductionTransitions_Boms_ProductionBomId",
                        column: x => x.ProductionBomId,
                        principalTable: "Boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MassProductionTransitions_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RndBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndBudgets_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RndMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RndMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RndMilestones_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrialProductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RndProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrialNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantityProduced = table.Column<int>(type: "integer", nullable: false),
                    GoodQuantity = table.Column<int>(type: "integer", nullable: false),
                    DefectiveQuantity = table.Column<int>(type: "integer", nullable: false),
                    YieldRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DfmFeedback = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialProductions_RndProjects_RndProjectId",
                        column: x => x.RndProjectId,
                        principalTable: "RndProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesForecastItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesForecastId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriorYearQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ForecastQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    GrowthRate = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EstimatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Probability = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesForecastItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesForecastItems_SalesForecasts_SalesForecastId",
                        column: x => x.SalesForecastId,
                        principalTable: "SalesForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StocktakeOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SystemQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DifferenceAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeOrderItems_StocktakeOrders_StocktakeOrderId",
                        column: x => x.StocktakeOrderId,
                        principalTable: "StocktakeOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractItems_SubcontractOrders_SubcontractOrderId",
                        column: x => x.SubcontractOrderId,
                        principalTable: "SubcontractOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAsnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkuNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SkuName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShippedQty = table.Column<int>(type: "integer", nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAsnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierAsnItems_SupplierAsns_AsnId",
                        column: x => x.AsnId,
                        principalTable: "SupplierAsns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IqcReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SamplingLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Aql = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    AcceptNumber = table.Column<int>(type: "integer", nullable: false),
                    RejectNumber = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IqcReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IqcReports_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrId = table.Column<Guid>(type: "uuid", nullable: true),
                    PoDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TradeTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseSubtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    BlanketOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TermsAndConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SpecialPackaging = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierBankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierBankAccounts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierComplaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DefectQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Ppm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DefectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierComplaints_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierContacts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierUsers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UomGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UomGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UomGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UomGroupMembers_UomGroups_UomGroupId",
                        column: x => x.UomGroupId,
                        principalTable: "UomGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UomGroupMembers_Uoms_UomId",
                        column: x => x.UomId,
                        principalTable: "Uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TableName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserThemePreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomPrimaryColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CustomAccentColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserThemePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserThemePreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BinLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Zone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Aisle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Rack = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Position = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaxWeight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    GridX = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GridY = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapacityCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalendarType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapacityCalendars_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapacityLoads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftNumber = table.Column<int>(type: "integer", nullable: false),
                    AvailableHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    ScheduledHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    LoadedHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    LoadPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LoadStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CapacityUtilization = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityLoads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapacityLoads_ProductionSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CapacityLoads_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EquipmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaintenanceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Equipments_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RccpResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResourceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapacityPerBucket = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Efficiency = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Utilization = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RccpResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RccpResources_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetDepreciations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DepreciationAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BookValue = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDepreciations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetDepreciations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PartsCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ExternalCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    IsCapitalized = table.Column<bool>(type: "boolean", nullable: false),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMaintenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetMaintenances_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashReceiptAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ExchangeGainLoss = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceiptAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReceiptAllocations_CashReceipts_CashReceiptId",
                        column: x => x.CashReceiptId,
                        principalTable: "CashReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EcnAffectedBoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcnId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcnAffectedBoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EcnAffectedBoms_Ecns_EcnId",
                        column: x => x.EcnId,
                        principalTable: "Ecns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChineseName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NationalId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PassportNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ResidencePermitNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ResidencePermitExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RegisteredAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MailingAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobTitleId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobGrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    JobGradeCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    JobCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupervisorId = table.Column<Guid>(type: "uuid", nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProbationEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResignationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResignationReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SalaryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    FoodAllowance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FullAttendanceBonus = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LaborInsuranceGrade = table.Column<int>(type: "integer", nullable: false),
                    HealthInsuranceGrade = table.Column<int>(type: "integer", nullable: false),
                    PensionContributionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxDependents = table.Column<int>(type: "integer", nullable: false),
                    DisabilityCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CardNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BiometricId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShiftType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OvertimePermission = table.Column<bool>(type: "boolean", nullable: false),
                    HighestEducation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Major = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GraduationYear = table.Column<int>(type: "integer", nullable: true),
                    ForeignLanguages = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Skills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdCardScanUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DiplomaScanUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsuranceTransferUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Employees_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_JobGrades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalendarReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinutesBefore = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarReminderLogs_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IqcReportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IqcReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionItem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MeasuredValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IqcReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IqcReportItems_IqcReports_IqcReportId",
                        column: x => x.IqcReportId,
                        principalTable: "IqcReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asns_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCapas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapaNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    D1_Team = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    D2_ProblemDescription = table.Column<string>(type: "text", nullable: true),
                    D3_InterimContainment = table.Column<string>(type: "text", nullable: true),
                    D4_RootCause = table.Column<string>(type: "text", nullable: true),
                    D5_PermanentAction = table.Column<string>(type: "text", nullable: true),
                    D6_Verification = table.Column<string>(type: "text", nullable: true),
                    D7_Prevention = table.Column<string>(type: "text", nullable: true),
                    D8_Closure = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierResponseDue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupplierRespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCapas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCapas_SupplierComplaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "SupplierComplaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Skus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SkuName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PurchaseUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PurchaseUomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ConversionRate = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    LotControlled = table.Column<bool>(type: "boolean", nullable: false),
                    SerialControlled = table.Column<bool>(type: "boolean", nullable: false),
                    Length = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Width = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Height = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DefaultWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultBinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AbcClassification = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SafetyStock = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MaxStock = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    Moq = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    StandardCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LowLevelCode = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitsPerCarton = table.Column<int>(type: "integer", nullable: true),
                    CartonsPerPallet = table.Column<int>(type: "integer", nullable: true),
                    HumidityRequirement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BaseUomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SalesUomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProductionUomCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PurchaseToBaseFactor = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    SalesToBaseFactor = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    ProductionToBaseFactor = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    BaseUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSellable = table.Column<bool>(type: "boolean", nullable: false),
                    IsPurchasable = table.Column<bool>(type: "boolean", nullable: false),
                    IsManufacturable = table.Column<bool>(type: "boolean", nullable: false),
                    IsOutsourced = table.Column<bool>(type: "boolean", nullable: false),
                    CostMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SuggestedPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    PriceA = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    PriceB = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    PriceC = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    PriceD = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    PriceE = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsBomTop = table.Column<bool>(type: "boolean", nullable: false),
                    IsBomComponent = table.Column<bool>(type: "boolean", nullable: false),
                    IsPhantom = table.Column<bool>(type: "boolean", nullable: false),
                    IsMto = table.Column<bool>(type: "boolean", nullable: false),
                    IsMts = table.Column<bool>(type: "boolean", nullable: false),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: false),
                    IsMpsManaged = table.Column<bool>(type: "boolean", nullable: false),
                    PlanningStrategy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultBomId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultRoutingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MakeOrBuy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierLeadTime = table.Column<int>(type: "integer", nullable: true),
                    DrawingNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DrawingRevision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QualitySpec = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductionDept = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MachineCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MoldCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CostUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CostUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    LatestPurchaseCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxRate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsSubstitute = table.Column<bool>(type: "boolean", nullable: false),
                    IsAlternate = table.Column<bool>(type: "boolean", nullable: false),
                    IqcRequired = table.Column<bool>(type: "boolean", nullable: false),
                    OqcRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PackingMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinPackingQty = table.Column<int>(type: "integer", nullable: true),
                    IsStocked = table.Column<bool>(type: "boolean", nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DesignOwner = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShelfLifeDays = table.Column<int>(type: "integer", nullable: true),
                    AvgDailyConsumption = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PurchasePriceCeiling = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    FloorPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CommissionAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedSkuId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BomType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RoutingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubstituteSkuId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OutsourceSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingFeeAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OutsourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSupplierProvided = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skus_BinLocations_DefaultBinLocationId",
                        column: x => x.DefaultBinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Suppliers_DefaultSupplierId",
                        column: x => x.DefaultSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Uoms_BaseUomId",
                        column: x => x.BaseUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Uoms_CostUomId",
                        column: x => x.CostUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Uoms_ProductionUomId",
                        column: x => x.ProductionUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Uoms_PurchaseUomId",
                        column: x => x.PurchaseUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Uoms_SalesUomId",
                        column: x => x.SalesUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skus_Warehouses_DefaultWarehouseId",
                        column: x => x.DefaultWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CapacityCalendarEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftNumber = table.Column<int>(type: "integer", nullable: false),
                    ShiftCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AvailableHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Efficiency = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityCalendarEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapacityCalendarEntries_CapacityCalendars_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "CapacityCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentDowntimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DowntimeNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DowntimeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Cause = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReportedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentDowntimes_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionLine = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ScheduledStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ScheduledEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ClockIn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockOut = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockInLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClockOutLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LateFlag = table.Column<bool>(type: "boolean", nullable: false),
                    LateMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyLeaveFlag = table.Column<bool>(type: "boolean", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "integer", nullable: false),
                    ClockingAnomalyFlag = table.Column<bool>(type: "boolean", nullable: false),
                    AnomalyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ot1Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ot2Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ot3Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OtReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CostCenterName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CostCenterType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentCostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BudgetUsed = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostCenters_CostCenters_ParentCostCenterId",
                        column: x => x.ParentCostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostCenters_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CostCenters_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CostCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CertifyingAuthority = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCertifications_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewDeptId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NewPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldSalary = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    NewSalary = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TotalEntitled = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Used = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Remaining = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    TotalHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IncludesSpecialPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    ShiftConflictFlag = table.Column<bool>(type: "boolean", nullable: false),
                    AutoDeductPayFlag = table.Column<bool>(type: "boolean", nullable: false),
                    ImpactOtOrBonus = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OvertimeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpectedResolution = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAnomalies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payrolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InsuranceBase = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PensionBase = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NewLeavingFlag = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledDays = table.Column<int>(type: "integer", nullable: false),
                    ActualWorkingDays = table.Column<int>(type: "integer", nullable: false),
                    TotalLateMinutes = table.Column<int>(type: "integer", nullable: false),
                    TotalEarlyMinutes = table.Column<int>(type: "integer", nullable: false),
                    AnomalyDays = table.Column<int>(type: "integer", nullable: false),
                    TotalOt1Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TotalOt2Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TotalOt3Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ot1Pay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Ot2Pay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Ot3Pay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalLeaveHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    AnnualLeaveHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    PersonalLeaveHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    SickLeaveHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    FullAttendanceFlag = table.Column<bool>(type: "boolean", nullable: false),
                    LateDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EarlyDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AnomalyPenalty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PersonalLeaveDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SickLeaveDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OvertimePay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Bonus = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AttendanceBonus = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LaborInsuranceDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    HealthInsuranceDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PensionDeduction = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    WithholdingTax = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    SupplementalHealthInsurance = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    EmployerLi = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EmployerHi = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EmployerPf = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EmploymentInsurance = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    GrossPay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EmployerContributions = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NetPay = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CalculatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentBankAccount = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PayrollSlipNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CostAllocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payrolls_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewPeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceReviews_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PrDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BudgetAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WorkflowStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CurrentApprovalLevel = table.Column<string>(type: "text", nullable: true),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    TotalEstimatedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_Employees_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TradeTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerPoNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ManufacturingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreditChecked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BaseSubtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BaseTotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalespersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SalesChannel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AdvanceDepositAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    OrderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProductionGroupCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    HsCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Freight = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    ShippingMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Incoterms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Employees_SalespersonId",
                        column: x => x.SalespersonId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ScheduleData = table.Column<string>(type: "text", nullable: true),
                    ShiftType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TrainingProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrainingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrainingHours = table.Column<int>(type: "integer", nullable: false),
                    CertificationResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkExperiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Responsibilities = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperiences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    AsnId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IqcStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierInvoiceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PackingListNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    TotalTariff = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalFreight = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalLandedCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Asns_AsnId",
                        column: x => x.AsnId,
                        principalTable: "Asns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsnItems_Asns_AsnId",
                        column: x => x.AsnId,
                        principalTable: "Asns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsnItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompletionOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletionOrderItems_CompletionOrders_CompletionOrderId",
                        column: x => x.CompletionOrderId,
                        principalTable: "CompletionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletionOrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandForecastExts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ForecastQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Accuracy = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ActualQty = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandForecastExts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandForecastExts_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLots_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    OrderType = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrxId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QtyChange = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    QtyBefore = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    QtyAfter = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedDocType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedDocId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandlerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_BinLocations_BinLocationId",
                        column: x => x.BinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MpsLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MpsHeaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    BucketStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BucketEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BucketCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PlanQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ConsumedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AdjustedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OverrideFlag = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MpsLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MpsLines_MpsHeaders_MpsHeaderId",
                        column: x => x.MpsHeaderId,
                        principalTable: "MpsHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MpsLines_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MrpPlannedOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MrpRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrderQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceRequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConvertedDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConvertedDocumentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MrpPlannedOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MrpPlannedOrders_MrpRuns_MrpRunId",
                        column: x => x.MrpRunId,
                        principalTable: "MrpRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MrpPlannedOrders_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MrpRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MrpRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceDocumentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RequiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiredQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AvailableQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ShortageQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PlannedReceiptQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NetRequirement = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubstituteSkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MrpRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MrpRequirements_MrpRuns_MrpRunId",
                        column: x => x.MrpRunId,
                        principalTable: "MrpRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MrpRequirements_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InventoryUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConversionRate = table.Column<decimal>(type: "numeric(14,6)", nullable: false),
                    InventoryQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InventoryUnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    InventorySubtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Skus_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RccpItemResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsagePerUnit = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RccpItemResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RccpItemResources_RccpResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "RccpResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RccpItemResources_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RoutingName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routings_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SafetyStockLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyStockQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyStockLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyStockLevels_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceItems_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SerialNumbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SkuName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Warehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SerialNumbers_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubstituteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConditionExpression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstituteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubstituteItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstituteItems_SubstituteGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "SubstituteGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransferQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferOrderItems_TransferOrders_TransferOrderId",
                        column: x => x.TransferOrderId,
                        principalTable: "TransferOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UomConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Operator = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    InverseFactor = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UomConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UomConversions_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UomConversions_Uoms_FromUomId",
                        column: x => x.FromUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UomConversions_Uoms_ToUomId",
                        column: x => x.ToUomId,
                        principalTable: "Uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WoNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WoType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomId = table.Column<Guid>(type: "uuid", nullable: true),
                    BomVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MrpPlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MrpPlannedDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledResourceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsScheduleLocked = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleVersion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NeedsRescheduling = table.Column<bool>(type: "boolean", nullable: false),
                    RescheduleTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Skus_ProductSkuId",
                        column: x => x.ProductSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    GLAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Memo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_AccountingVouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "AccountingVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountingEntries_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActivityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StandardRate = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ActualRate = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityTypes_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationBasis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationRules_CostCenters_SourceCostCenterId",
                        column: x => x.SourceCostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRules_CostCenters_TargetCostCenterId",
                        column: x => x.TargetCostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostCenterPostings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenterPostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostCenterPostings_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostCenterPostings_CostElements_CostElementId",
                        column: x => x.CostElementId,
                        principalTable: "CostElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostElementAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostElementAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostElementAssignments_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostElementAssignments_CostElements_CostElementId",
                        column: x => x.CostElementId,
                        principalTable: "CostElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaborRate = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    OverheadRate = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    MachineRate = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenterCosts_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkCenterCosts_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationResult = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AdjustmentItems = table.Column<string>(type: "text", nullable: true),
                    AccountingPreview = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalFlow = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCorrections_PayrollAnomalies_AnomalyId",
                        column: x => x.AnomalyId,
                        principalTable: "PayrollAnomalies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CalculationBase = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    CalculationUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    TaxType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDetails_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionApprovals_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionApprovals_PurchaseRequisitions_PurchaseR~",
                        column: x => x.PurchaseRequisitionId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RequiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionItems_PurchaseRequisitions_PurchaseRequi~",
                        column: x => x.PurchaseRequisitionId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rfqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PrId = table.Column<Guid>(type: "uuid", nullable: true),
                    RfqDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rfqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rfqs_PurchaseRequisitions_PrId",
                        column: x => x.PrId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShippingMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SalesOrderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrders_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOrders_SalesOrders_SalesOrderId1",
                        column: x => x.SalesOrderId1,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    PoUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReceivedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    AcceptedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    RejectedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InventoryUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConversionRate = table.Column<decimal>(type: "numeric(14,6)", nullable: false),
                    InventoryQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IqcResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    ForeignCurrencyPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    LocalCurrencyPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ForeignCurrencyAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LocalCurrencyAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TariffAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    FreightAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalLandedCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_BinLocations_BinLocationId",
                        column: x => x.BinLocationId,
                        principalTable: "BinLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastConsumptionDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MpsLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SoLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastConsumptionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastConsumptionDetails_MpsLines_MpsLineId",
                        column: x => x.MpsLineId,
                        principalTable: "MpsLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutingOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferTime = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    WaitTime = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_Routings_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "Routings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DeliveredQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerItemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxRate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BomId = table.Column<Guid>(type: "uuid", nullable: true),
                    BomVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RoutingId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    NeedManufacturing = table.Column<bool>(type: "boolean", nullable: false),
                    NeedOutsourcing = table.Column<bool>(type: "boolean", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannedFinishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BackorderQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LineRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Boms_BomId",
                        column: x => x.BomId,
                        principalTable: "Boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Routings_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "Routings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Skus_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineOperationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftNumber = table.Column<int>(type: "integer", nullable: false),
                    PlannedRunTime = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ActualRunTime = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Downtime = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SetupTime = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WaitTime = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TheoreticalOutput = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualOutput = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    GoodOutput = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DefectOutput = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DowntimeReasonCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineOperationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineOperationRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineOperationRecords_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssueWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialIssues_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SetupHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    RunHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SequenceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTasks_ProductionSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "ProductionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleTasks_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleTasks_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScrapRecordExts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ScrapReasonCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DispositionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CostAmount = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ResponsibleDept = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapRecordExts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapRecordExts_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScrapRecordExts_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubstituteUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubstituteSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalQty = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    SubstituteQty = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    CostVariance = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstituteUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubstituteUsageLogs_Skus_OriginalSkuId",
                        column: x => x.OriginalSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstituteUsageLogs_Skus_SubstituteSkuId",
                        column: x => x.SubstituteSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstituteUsageLogs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WipReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompletedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DefectQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ActualManHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    ActualMachineHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaterialLotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EnergyConsumption = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    HasAnomaly = table.Column<bool>(type: "boolean", nullable: false),
                    AnomalyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AiDiagnosis = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WipReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WipReports_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_costs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalOutputQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaterialActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LaborActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MachineActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OverheadActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StandardUnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalStandardCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VarianceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_costs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_costs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_material_actuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_material_actuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_material_actuals_InventoryTransactions_Inventory~",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_order_material_actuals_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_material_actuals_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    BomStandardQty = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    RequiredQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    IssuedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ReturnedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ShortageQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterials_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterials_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CompletedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ScrapQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledWorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledSequence = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderOperations_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderOperations_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RfqItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Specifications = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfqItems_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RfqItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RfqSupplierResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RfqId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    Moq = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TradeTerms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfqSupplierResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfqSupplierResponses_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RfqSupplierResponses_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RfqSupplierResponses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeliveredQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItems_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderProductionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionDocType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductionDocId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionDocNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LinkedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderProductionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderProductionLinks_SalesOrderItems_SalesOrderItemId",
                        column: x => x.SalesOrderItemId,
                        principalTable: "SalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialIssueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiredQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    IssuedQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ShortageQty = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BinLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialIssueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialIssueItems_MaterialIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "MaterialIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_labor_actuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WipReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    ActualHours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_labor_actuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_labor_actuals_WipReports_WipReportId",
                        column: x => x.WipReportId,
                        principalTable: "WipReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_order_labor_actuals_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_machine_actuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WipReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationSeq = table.Column<int>(type: "integer", nullable: false),
                    ActualHours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MachineRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_machine_actuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_machine_actuals_WipReports_WipReportId",
                        column: x => x.WipReportId,
                        principalTable: "WipReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_order_machine_actuals_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_order_machine_actuals_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_CostCenterId",
                table: "AccountingEntries",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_GLAccountId",
                table: "AccountingEntries",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_VoucherId",
                table: "AccountingEntries",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingVouchers_ReferenceType_ReferenceId",
                table: "AccountingVouchers",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingVouchers_VoucherDate",
                table: "AccountingVouchers",
                column: "VoucherDate");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingVouchers_VoucherNumber",
                table: "AccountingVouchers",
                column: "VoucherNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayableItems_AccountsPayableId",
                table: "AccountsPayableItems",
                column: "AccountsPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountsPayables_ApNumber",
                table: "AccountsPayables",
                column: "ApNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypes_ActivityCode",
                table: "ActivityTypes",
                column: "ActivityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypes_CostCenterId",
                table: "ActivityTypes",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTasks_AgentExecutionId",
                table: "AgentTasks",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiStocktakeAnalyses_StocktakeOrderId",
                table: "AiStocktakeAnalyses",
                column: "StocktakeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AiStocktakeMissions_MissionNumber",
                table: "AiStocktakeMissions",
                column: "MissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiVisionCaptures_CaptureNumber",
                table: "AiVisionCaptures",
                column: "CaptureNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiVisionCaptures_SkuId",
                table: "AiVisionCaptures",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_SourceCostCenterId_TargetCostCenterId",
                table: "AllocationRules",
                columns: new[] { "SourceCostCenterId", "TargetCostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_TargetCostCenterId",
                table: "AllocationRules",
                column: "TargetCostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementReadLogs_AnnouncementId",
                table: "AnnouncementReadLogs",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuthorities_GradeId",
                table: "ApprovalAuthorities",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApsScheduleResults_OrderNo_ScheduleVersion",
                table: "ApsScheduleResults",
                columns: new[] { "OrderNo", "ScheduleVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_AsnItems_AsnId",
                table: "AsnItems",
                column: "AsnId");

            migrationBuilder.CreateIndex(
                name: "IX_AsnItems_SkuId",
                table: "AsnItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Asns_AsnNumber",
                table: "Asns",
                column: "AsnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asns_PurchaseOrderId",
                table: "Asns",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Asns_SupplierId",
                table: "Asns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_AsrsTasks_TaskNumber",
                table: "AsrsTasks",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_CategoryCode",
                table: "AssetCategories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetDepreciations_AssetId",
                table: "AssetDepreciations",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLocations_LocationCode",
                table: "AssetLocations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_AssetId",
                table: "AssetMaintenances",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetNumber",
                table: "Assets",
                column: "AssetNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CategoryId",
                table: "Assets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetStocktakeItems_AssetStocktakeId",
                table: "AssetStocktakeItems",
                column: "AssetStocktakeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_DepartmentId",
                table: "AttendanceRecords",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_Date",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Status",
                table: "AttendanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BinLocations_WarehouseId",
                table: "BinLocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_BomId",
                table: "BomItems",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_BomPaths_AncestorSkuId_DescendantSkuId_Distance",
                table: "BomPaths",
                columns: new[] { "AncestorSkuId", "DescendantSkuId", "Distance" });

            migrationBuilder.CreateIndex(
                name: "IX_Boms_BomCode",
                table: "Boms",
                column: "BomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetVersions_BudgetYearId",
                table: "BudgetVersions",
                column: "BudgetYearId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBuckets_CalendarId_BucketCode",
                table: "CalendarBuckets",
                columns: new[] { "CalendarId", "BucketCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarCategories_Code",
                table: "CalendarCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_CategoryCode",
                table: "CalendarEvents",
                column: "CategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_MeetingRoomId",
                table: "CalendarEvents",
                column: "MeetingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_SourceModule",
                table: "CalendarEvents",
                column: "SourceModule");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_StartDate_EndDate",
                table: "CalendarEvents",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminderLogs_CalendarEventId",
                table: "CalendarReminderLogs",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminderLogs_Status_ScheduledAt",
                table: "CalendarReminderLogs",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarShares_CalendarOwnerId",
                table: "CalendarShares",
                column: "CalendarOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityCalendarEntries_CalendarId",
                table: "CapacityCalendarEntries",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityCalendars_WorkCenterId",
                table: "CapacityCalendars",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityLoads_ScheduleId",
                table: "CapacityLoads",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityLoads_WorkCenterId",
                table: "CapacityLoads",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceiptAllocations_CashReceiptId",
                table: "CashReceiptAllocations",
                column: "CashReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_CustomerId",
                table: "CashReceipts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_ReceiptNumber",
                table: "CashReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyHolidays_Date",
                table: "CompanyHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionOrderItems_CompletionOrderId",
                table: "CompletionOrderItems",
                column: "CompletionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionOrderItems_SkuId",
                table: "CompletionOrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionOrders_CompletionNumber",
                table: "CompletionOrders",
                column: "CompletionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenterPostings_CostCenterId",
                table: "CostCenterPostings",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenterPostings_CostElementId",
                table: "CostCenterPostings",
                column: "CostElementId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_CostCenterCode",
                table: "CostCenters",
                column: "CostCenterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_DepartmentId",
                table: "CostCenters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_ManagerEmployeeId",
                table: "CostCenters",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_ParentCostCenterId",
                table: "CostCenters",
                column: "ParentCostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_WorkCenterId",
                table: "CostCenters",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CostElementAssignments_CostCenterId_CostElementId",
                table: "CostElementAssignments",
                columns: new[] { "CostCenterId", "CostElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostElementAssignments_CostElementId",
                table: "CostElementAssignments",
                column: "CostElementId");

            migrationBuilder.CreateIndex(
                name: "IX_CostElements_ElementCode",
                table: "CostElements",
                column: "ElementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrossDeptTaskReplies_TaskId",
                table: "CrossDeptTaskReplies",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CurrencyCode",
                table: "Currencies",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxId",
                table: "Customers",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItems_DeliveryOrderId",
                table: "DeliveryOrderItems",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItems_SkuId",
                table: "DeliveryOrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_DeliveryNumber",
                table: "DeliveryOrders",
                column: "DeliveryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_SalesOrderId",
                table: "DeliveryOrders",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_SalesOrderId1",
                table: "DeliveryOrders",
                column: "SalesOrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_DemandForecastExts_SkuId",
                table: "DemandForecastExts",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPages_DepartmentId",
                table: "DepartmentPages",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeptCode",
                table: "Departments",
                column: "DeptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDeptId",
                table: "Departments",
                column: "ParentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocNumber",
                table: "Documents",
                column: "DocNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EcnAffectedBoms_EcnId",
                table: "EcnAffectedBoms",
                column: "EcnId");

            migrationBuilder.CreateIndex(
                name: "IX_Ecns_EcnNumber",
                table: "Ecns",
                column: "EcnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ecns_EcrId",
                table: "Ecns",
                column: "EcrId");

            migrationBuilder.CreateIndex(
                name: "IX_Ecrs_EcrNumber",
                table: "Ecrs",
                column: "EcrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_EmployeeId",
                table: "EmployeeCertifications",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNo",
                table: "Employees",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_GradeId",
                table: "Employees",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobTitleId",
                table: "Employees",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NationalId",
                table: "Employees",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SupervisorId",
                table: "Employees",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentHistories_EmployeeId",
                table: "EmploymentHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDowntimes_EquipmentId",
                table: "EquipmentDowntimes",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_SupplierId",
                table: "Equipments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_WorkCenterId",
                table: "Equipments",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_EsgDashboardConfigs_ConfigKey",
                table: "EsgDashboardConfigs",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrency_ToCurrency_ValidFrom",
                table: "ExchangeRates",
                columns: new[] { "FromCurrency", "ToCurrency", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgetItems_ExpenseBudgetId",
                table: "ExpenseBudgetItems",
                column: "ExpenseBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastConsumptionDetails_MpsLineId",
                table: "ForecastConsumptionDetails",
                column: "MpsLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_AccountCode",
                table: "GLAccounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_ParentAccountId",
                table: "GLAccounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_BinLocationId",
                table: "GoodsReceiptItems",
                column: "BinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_GoodsReceiptId",
                table: "GoodsReceiptItems",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_SkuId",
                table: "GoodsReceiptItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_AsnId",
                table: "GoodsReceipts",
                column: "AsnId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_GrNumber",
                table: "GoodsReceipts",
                column: "GrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_SupplierId",
                table: "GoodsReceipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId",
                table: "GoodsReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Guarantees_GuaranteeNumber",
                table: "Guarantees",
                column: "GuaranteeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_SkuId_LotNumber",
                table: "InventoryLots",
                columns: new[] { "SkuId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_SkuId",
                table: "InventoryReservations",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BinLocationId",
                table: "InventoryTransactions",
                column: "BinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CreatedAt",
                table: "InventoryTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_SkuId_CreatedAt",
                table: "InventoryTransactions",
                columns: new[] { "SkuId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentValuations_InvestmentId",
                table: "InvestmentValuations",
                column: "InvestmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IpqcReportItems_IpqcReportId",
                table: "IpqcReportItems",
                column: "IpqcReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IpqcReports_ReportNumber",
                table: "IpqcReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IqcReportItems_IqcReportId",
                table: "IqcReportItems",
                column: "IqcReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_ReportNumber",
                table: "IqcReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IqcReports_SupplierId",
                table: "IqcReports",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_GradeId",
                table: "JobTitles",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiValues_KpiDefinitionId",
                table: "KpiValues",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborBudgetItems_LaborBudgetId",
                table: "LaborBudgetItems",
                column: "LaborBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId",
                table: "LeaveBalances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApplicationNo",
                table: "LeaveRequests",
                column: "ApplicationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_Status",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LineApprovalTokens_Token",
                table: "LineApprovalTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineUserMappings_LineUserId",
                table: "LineUserMappings",
                column: "LineUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanRepaymentSchedules_LoanId",
                table: "LoanRepaymentSchedules",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_LoanNumber",
                table: "Loans",
                column: "LoanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperationRecords_WorkCenterId",
                table: "MachineOperationRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperationRecords_WorkOrderId",
                table: "MachineOperationRecords",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MassProductionTransitions_ProductionBomId",
                table: "MassProductionTransitions",
                column: "ProductionBomId");

            migrationBuilder.CreateIndex(
                name: "IX_MassProductionTransitions_RndProjectId",
                table: "MassProductionTransitions",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCostForecastItems_MaterialCostForecastId",
                table: "MaterialCostForecastItems",
                column: "MaterialCostForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssueItems_IssueId",
                table: "MaterialIssueItems",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_IssueNumber",
                table: "MaterialIssues",
                column: "IssueNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_WorkOrderId",
                table: "MaterialIssues",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturnItems_ReturnId",
                table: "MaterialReturnItems",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReturns_ReturnNumber",
                table: "MaterialReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSupplementItems_SupplementId",
                table: "MaterialSupplementItems",
                column: "SupplementId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSupplements_SupplementNumber",
                table: "MaterialSupplements",
                column: "SupplementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingReservations_RoomId",
                table: "MeetingReservations",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPeriods_Year_Month",
                table: "MonthlyPeriods",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MpsHeaders_MpsNo",
                table: "MpsHeaders",
                column: "MpsNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MpsLines_MpsHeaderId",
                table: "MpsLines",
                column: "MpsHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_MpsLines_SkuId",
                table: "MpsLines",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_MrpPlannedOrders_MrpRunId",
                table: "MrpPlannedOrders",
                column: "MrpRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MrpPlannedOrders_SkuId",
                table: "MrpPlannedOrders",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_MrpRequirements_MrpRunId",
                table: "MrpRequirements",
                column: "MrpRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MrpRequirements_SkuId",
                table: "MrpRequirements",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformanceReports_NcrNumber",
                table: "NonConformanceReports",
                column: "NcrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operations_OperationCode",
                table: "Operations",
                column: "OperationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OqcReports_ReportNumber",
                table: "OqcReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_EmployeeId_Date",
                table: "OvertimeRequests",
                columns: new[] { "EmployeeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentBatches_BatchNumber",
                table: "PaymentBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_PaymentBatchId",
                table: "PaymentRecords",
                column: "PaymentBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAnomalies_EmployeeId",
                table: "PayrollAnomalies",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCorrections_AnomalyId",
                table: "PayrollCorrections",
                column: "AnomalyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDetails_PayrollId",
                table: "PayrollDetails",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_EmployeeId_PeriodStart_PeriodEnd",
                table: "Payrolls",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_Status",
                table: "Payrolls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_EmployeeId",
                table: "PerformanceReviews",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionCode",
                table: "Permissions",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortalMessages_ThreadId",
                table: "PortalMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_SkuId",
                table: "PurchaseOrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PoNumber",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionApprovals_ApproverId",
                table: "PurchaseRequisitionApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionApprovals_PurchaseRequisitionId",
                table: "PurchaseRequisitionApprovals",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionItems_PurchaseRequisitionId",
                table: "PurchaseRequisitionItems",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionItems_SkuId",
                table: "PurchaseRequisitionItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_DepartmentId",
                table: "PurchaseRequisitions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_PrNumber",
                table: "PurchaseRequisitions",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_RequesterId",
                table: "PurchaseRequisitions",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ProductId",
                table: "QuoteItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_QuoteId",
                table: "QuoteItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_SkuId",
                table: "QuoteItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteNumber",
                table: "Quotes",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RccpItemResources_ResourceId",
                table: "RccpItemResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RccpItemResources_SkuId_ResourceId",
                table: "RccpItemResources",
                columns: new[] { "SkuId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RccpResources_ResourceCode",
                table: "RccpResources",
                column: "ResourceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RccpResources_WorkCenterId",
                table: "RccpResources",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqItems_RfqId",
                table: "RfqItems",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqItems_SkuId",
                table: "RfqItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_PrId",
                table: "Rfqs",
                column: "PrId");

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_RfqNumber",
                table: "Rfqs",
                column: "RfqNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfqSupplierResponses_RfqId",
                table: "RfqSupplierResponses",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqSupplierResponses_SkuId",
                table: "RfqSupplierResponses",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqSupplierResponses_SupplierId",
                table: "RfqSupplierResponses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RndBudgets_RndProjectId",
                table: "RndBudgets",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndMilestones_RndProjectId",
                table: "RndMilestones",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RndProjects_ProjectCode",
                table: "RndProjects",
                column: "ProjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_OperationId",
                table: "RoutingOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_RoutingId",
                table: "RoutingOperations",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_WorkCenterId",
                table: "RoutingOperations",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Routings_ProductSkuId",
                table: "Routings",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Routings_RoutingCode",
                table: "Routings",
                column: "RoutingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyStockLevels_SkuId",
                table: "SafetyStockLevels",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesForecastItems_SalesForecastId",
                table: "SalesForecastItems",
                column: "SalesForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_SalesInvoiceId",
                table: "SalesInvoiceItems",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_SkuId",
                table: "SalesInvoiceItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CustomerId",
                table: "SalesInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InvoiceNumber",
                table: "SalesInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_BomId",
                table: "SalesOrderItems",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_ProductId",
                table: "SalesOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_RoutingId",
                table: "SalesOrderItems",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId_LineNumber",
                table: "SalesOrderItems",
                columns: new[] { "SalesOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SkuId",
                table: "SalesOrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_WarehouseId",
                table: "SalesOrderItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_WorkCenterId",
                table: "SalesOrderItems",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderProductionLinks_SalesOrderItemId",
                table: "SalesOrderProductionLinks",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_DepartmentId",
                table: "SalesOrders",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderNumber",
                table: "SalesOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalespersonId",
                table: "SalesOrders",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleRequests_RequestNumber",
                table: "SampleRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleConstraints_ScheduleId",
                table: "ScheduleConstraints",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTasks_ScheduleId",
                table: "ScheduleTasks",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTasks_WorkCenterId",
                table: "ScheduleTasks",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTasks_WorkOrderId",
                table: "ScheduleTasks",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapRecordExts_SkuId",
                table: "ScrapRecordExts",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapRecordExts_WorkOrderId",
                table: "ScrapRecordExts",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SerialNo",
                table: "SerialNumbers",
                column: "SerialNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_SkuId",
                table: "SerialNumbers",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSchedules_EmployeeId_YearMonth",
                table: "ShiftSchedules",
                columns: new[] { "EmployeeId", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skus_Barcode",
                table: "Skus",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_BaseUomId",
                table: "Skus",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_CostUomId",
                table: "Skus",
                column: "CostUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_DefaultBinLocationId",
                table: "Skus",
                column: "DefaultBinLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_DefaultSupplierId",
                table: "Skus",
                column: "DefaultSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_DefaultWarehouseId",
                table: "Skus",
                column: "DefaultWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_ProductionUomId",
                table: "Skus",
                column: "ProductionUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_PurchaseUomId",
                table: "Skus",
                column: "PurchaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_SalesUomId",
                table: "Skus",
                column: "SalesUomId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_SkuNumber",
                table: "Skus",
                column: "SkuNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeOrderItems_StocktakeOrderId",
                table: "StocktakeOrderItems",
                column: "StocktakeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeOrders_StocktakeNumber",
                table: "StocktakeOrders",
                column: "StocktakeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractItems_SubcontractOrderId",
                table: "SubcontractItems",
                column: "SubcontractOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractOrders_SubcontractNumber",
                table: "SubcontractOrders",
                column: "SubcontractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubstituteItems_GroupId",
                table: "SubstituteItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituteItems_SkuId",
                table: "SubstituteItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituteUsageLogs_OriginalSkuId",
                table: "SubstituteUsageLogs",
                column: "OriginalSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituteUsageLogs_SubstituteSkuId",
                table: "SubstituteUsageLogs",
                column: "SubstituteSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituteUsageLogs_WorkOrderId",
                table: "SubstituteUsageLogs",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAsnItems_AsnId",
                table: "SupplierAsnItems",
                column: "AsnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBankAccounts_SupplierId",
                table: "SupplierBankAccounts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapas_CapaNumber",
                table: "SupplierCapas",
                column: "CapaNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCapas_ComplaintId",
                table: "SupplierCapas",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierComplaints_ComplaintNumber",
                table: "SupplierComplaints",
                column: "ComplaintNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierComplaints_SupplierId",
                table: "SupplierComplaints",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_SupplierId",
                table: "SupplierContacts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEsgQuestionnaires_QuestionnaireNumber",
                table: "SupplierEsgQuestionnaires",
                column: "QuestionnaireNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEsgQuestionnaires_SupplierId",
                table: "SupplierEsgQuestionnaires",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotes_RfqId_SupplierId",
                table: "SupplierQuotes",
                columns: new[] { "RfqId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierUsers_SupplierId",
                table: "SupplierUsers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierUsers_Username",
                table: "SupplierUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_ConfigKey",
                table: "SystemConfigs",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_EmployeeId",
                table: "TrainingRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItems_SkuId",
                table: "TransferOrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItems_TransferOrderId",
                table: "TransferOrderItems",
                column: "TransferOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_TransferNumber",
                table: "TransferOrders",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryTransfers_TransferNumber",
                table: "TreasuryTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrialProductions_RndProjectId",
                table: "TrialProductions",
                column: "RndProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UomConversions_FromUomId_ToUomId",
                table: "UomConversions",
                columns: new[] { "FromUomId", "ToUomId" });

            migrationBuilder.CreateIndex(
                name: "IX_UomConversions_SkuId",
                table: "UomConversions",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_UomConversions_ToUomId",
                table: "UomConversions",
                column: "ToUomId");

            migrationBuilder.CreateIndex(
                name: "IX_UomGroupMembers_UomGroupId_UomId",
                table: "UomGroupMembers",
                columns: new[] { "UomGroupId", "UomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UomGroupMembers_UomId",
                table: "UomGroupMembers",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_UomGroups_GroupCode",
                table: "UomGroups",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uoms_UomCode",
                table: "Uoms",
                column: "UomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserThemePreferences_UserId",
                table: "UserThemePreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_WarehouseCode",
                table: "Warehouses",
                column: "WarehouseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WipReports_ReportNumber",
                table: "WipReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WipReports_WorkOrderId",
                table: "WipReports",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_costs_WorkOrderId",
                table: "work_order_costs",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_labor_actuals_WipReportId",
                table: "work_order_labor_actuals",
                column: "WipReportId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_labor_actuals_WorkOrderId",
                table: "work_order_labor_actuals",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_machine_actuals_WipReportId",
                table: "work_order_machine_actuals",
                column: "WipReportId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_machine_actuals_WorkCenterId",
                table: "work_order_machine_actuals",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_machine_actuals_WorkOrderId",
                table: "work_order_machine_actuals",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_material_actuals_InventoryTransactionId",
                table: "work_order_material_actuals",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_material_actuals_SkuId",
                table: "work_order_material_actuals",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_material_actuals_WorkOrderId",
                table: "work_order_material_actuals",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterCosts_CostCenterId",
                table: "WorkCenterCosts",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterCosts_WorkCenterId_EffectiveFrom",
                table: "WorkCenterCosts",
                columns: new[] { "WorkCenterId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_WorkCenterCode",
                table: "WorkCenters",
                column: "WorkCenterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_EmployeeId",
                table: "WorkExperiences",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterials_SkuId",
                table: "WorkOrderMaterials",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterials_WorkOrderId",
                table: "WorkOrderMaterials",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_OperationId",
                table: "WorkOrderOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_WorkCenterId",
                table: "WorkOrderOperations",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_WorkOrderId",
                table: "WorkOrderOperations",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductSkuId",
                table: "WorkOrders",
                column: "ProductSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WoNumber",
                table: "WorkOrders",
                column: "WoNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingEntries");

            migrationBuilder.DropTable(
                name: "AccountsPayableItems");

            migrationBuilder.DropTable(
                name: "ActivityTypes");

            migrationBuilder.DropTable(
                name: "AgentTasks");

            migrationBuilder.DropTable(
                name: "AiAnomalyDetections");

            migrationBuilder.DropTable(
                name: "AiFormGuidances");

            migrationBuilder.DropTable(
                name: "AiInsights");

            migrationBuilder.DropTable(
                name: "AiStocktakeAnalyses");

            migrationBuilder.DropTable(
                name: "AiStocktakeMissions");

            migrationBuilder.DropTable(
                name: "AiVisionCaptures");

            migrationBuilder.DropTable(
                name: "AllocationRules");

            migrationBuilder.DropTable(
                name: "AnnouncementReadLogs");

            migrationBuilder.DropTable(
                name: "ApprovalAuthorities");

            migrationBuilder.DropTable(
                name: "ApsScheduleResults");

            migrationBuilder.DropTable(
                name: "AsnItems");

            migrationBuilder.DropTable(
                name: "AsrsTasks");

            migrationBuilder.DropTable(
                name: "AssetDepreciations");

            migrationBuilder.DropTable(
                name: "AssetDisposals");

            migrationBuilder.DropTable(
                name: "AssetLocations");

            migrationBuilder.DropTable(
                name: "AssetMaintenances");

            migrationBuilder.DropTable(
                name: "AssetStocktakeItems");

            migrationBuilder.DropTable(
                name: "AssetTransfers");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AutoReplenishments");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "BarcodeRecords");

            migrationBuilder.DropTable(
                name: "BarcodeScanHistories");

            migrationBuilder.DropTable(
                name: "BomItems");

            migrationBuilder.DropTable(
                name: "BomPaths");

            migrationBuilder.DropTable(
                name: "BudgetVersions");

            migrationBuilder.DropTable(
                name: "CalendarBuckets");

            migrationBuilder.DropTable(
                name: "CalendarReminderLogs");

            migrationBuilder.DropTable(
                name: "CalendarShares");

            migrationBuilder.DropTable(
                name: "CapacityCalendarEntries");

            migrationBuilder.DropTable(
                name: "CapacityLoads");

            migrationBuilder.DropTable(
                name: "CapexBudgets");

            migrationBuilder.DropTable(
                name: "CarbonFootprintMappings");

            migrationBuilder.DropTable(
                name: "CashReceiptAllocations");

            migrationBuilder.DropTable(
                name: "CompanyHolidays");

            migrationBuilder.DropTable(
                name: "CompletionOrderItems");

            migrationBuilder.DropTable(
                name: "CostCenterPostings");

            migrationBuilder.DropTable(
                name: "CostElementAssignments");

            migrationBuilder.DropTable(
                name: "CrossDeptTaskReplies");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "CustomerContacts");

            migrationBuilder.DropTable(
                name: "DeliveryOrderItems");

            migrationBuilder.DropTable(
                name: "DemandForecastExts");

            migrationBuilder.DropTable(
                name: "DemandForecasts");

            migrationBuilder.DropTable(
                name: "DepartmentPages");

            migrationBuilder.DropTable(
                name: "DocumentPermissions");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "EcnAffectedBoms");

            migrationBuilder.DropTable(
                name: "EmployeeCertifications");

            migrationBuilder.DropTable(
                name: "EmploymentHistories");

            migrationBuilder.DropTable(
                name: "EquipmentDowntimes");

            migrationBuilder.DropTable(
                name: "EquipmentHistories");

            migrationBuilder.DropTable(
                name: "EsgDashboardConfigs");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ExpenseBudgetItems");

            migrationBuilder.DropTable(
                name: "ExpiryAlerts");

            migrationBuilder.DropTable(
                name: "ForecastConsumptionDetails");

            migrationBuilder.DropTable(
                name: "GhgEmissionRecords");

            migrationBuilder.DropTable(
                name: "GhgEmissionSources");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItems");

            migrationBuilder.DropTable(
                name: "Guarantees");

            migrationBuilder.DropTable(
                name: "InventoryLots");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropTable(
                name: "InvestmentValuations");

            migrationBuilder.DropTable(
                name: "IpqcReportItems");

            migrationBuilder.DropTable(
                name: "IqcReportItems");

            migrationBuilder.DropTable(
                name: "KpiValues");

            migrationBuilder.DropTable(
                name: "LaborBudgetItems");

            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LineApprovalTokens");

            migrationBuilder.DropTable(
                name: "LineConfigs");

            migrationBuilder.DropTable(
                name: "LineNotificationLogs");

            migrationBuilder.DropTable(
                name: "LineUserMappings");

            migrationBuilder.DropTable(
                name: "LoanRepaymentSchedules");

            migrationBuilder.DropTable(
                name: "MachineOperationRecords");

            migrationBuilder.DropTable(
                name: "MaintenancePlans");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "MassProductionTransitions");

            migrationBuilder.DropTable(
                name: "MaterialCostForecastItems");

            migrationBuilder.DropTable(
                name: "MaterialIssueItems");

            migrationBuilder.DropTable(
                name: "MaterialReturnItems");

            migrationBuilder.DropTable(
                name: "MaterialSupplementItems");

            migrationBuilder.DropTable(
                name: "MeetingReservations");

            migrationBuilder.DropTable(
                name: "MonthlyPeriods");

            migrationBuilder.DropTable(
                name: "MpsExceptions");

            migrationBuilder.DropTable(
                name: "MrpPlannedOrders");

            migrationBuilder.DropTable(
                name: "MrpRequirements");

            migrationBuilder.DropTable(
                name: "NonConformanceReports");

            migrationBuilder.DropTable(
                name: "NotificationQueues");

            migrationBuilder.DropTable(
                name: "OqcReports");

            migrationBuilder.DropTable(
                name: "OvertimeRequests");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "PayrollCorrections");

            migrationBuilder.DropTable(
                name: "PayrollDetails");

            migrationBuilder.DropTable(
                name: "PerformanceReviews");

            migrationBuilder.DropTable(
                name: "PortalMessages");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductSpecifications");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionApprovals");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionItems");

            migrationBuilder.DropTable(
                name: "QuoteItems");

            migrationBuilder.DropTable(
                name: "RccpItemResources");

            migrationBuilder.DropTable(
                name: "RfqItems");

            migrationBuilder.DropTable(
                name: "RfqSupplierResponses");

            migrationBuilder.DropTable(
                name: "RndBudgets");

            migrationBuilder.DropTable(
                name: "RndLogs");

            migrationBuilder.DropTable(
                name: "RndMilestones");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "RoutingOperations");

            migrationBuilder.DropTable(
                name: "SafetyStockLevels");

            migrationBuilder.DropTable(
                name: "SalesForecastItems");

            migrationBuilder.DropTable(
                name: "SalesInvoiceItems");

            migrationBuilder.DropTable(
                name: "SalesOrderProductionLinks");

            migrationBuilder.DropTable(
                name: "SampleRequests");

            migrationBuilder.DropTable(
                name: "ScheduleConstraints");

            migrationBuilder.DropTable(
                name: "ScheduleTasks");

            migrationBuilder.DropTable(
                name: "ScrapRecordExts");

            migrationBuilder.DropTable(
                name: "ScrapRecords");

            migrationBuilder.DropTable(
                name: "SerialNumbers");

            migrationBuilder.DropTable(
                name: "ShiftSchedules");

            migrationBuilder.DropTable(
                name: "StocktakeOrderItems");

            migrationBuilder.DropTable(
                name: "SubcontractItems");

            migrationBuilder.DropTable(
                name: "SubstituteItems");

            migrationBuilder.DropTable(
                name: "SubstituteUsageLogs");

            migrationBuilder.DropTable(
                name: "SupplierAsnItems");

            migrationBuilder.DropTable(
                name: "SupplierBankAccounts");

            migrationBuilder.DropTable(
                name: "SupplierCapas");

            migrationBuilder.DropTable(
                name: "SupplierContacts");

            migrationBuilder.DropTable(
                name: "SupplierDocuments");

            migrationBuilder.DropTable(
                name: "SupplierEsgQuestionnaires");

            migrationBuilder.DropTable(
                name: "SupplierPerformanceScores");

            migrationBuilder.DropTable(
                name: "SupplierQuotes");

            migrationBuilder.DropTable(
                name: "SupplierRatings");

            migrationBuilder.DropTable(
                name: "SupplierUsers");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "SystemNotifications");

            migrationBuilder.DropTable(
                name: "TrainingRecords");

            migrationBuilder.DropTable(
                name: "TransferOrderItems");

            migrationBuilder.DropTable(
                name: "TreasuryTransfers");

            migrationBuilder.DropTable(
                name: "TrialProductions");

            migrationBuilder.DropTable(
                name: "UomConversions");

            migrationBuilder.DropTable(
                name: "UomGroupMembers");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserThemePreferences");

            migrationBuilder.DropTable(
                name: "work_order_costs");

            migrationBuilder.DropTable(
                name: "work_order_labor_actuals");

            migrationBuilder.DropTable(
                name: "work_order_machine_actuals");

            migrationBuilder.DropTable(
                name: "work_order_material_actuals");

            migrationBuilder.DropTable(
                name: "WorkCenterCosts");

            migrationBuilder.DropTable(
                name: "WorkExperiences");

            migrationBuilder.DropTable(
                name: "WorkOrderMaterials");

            migrationBuilder.DropTable(
                name: "WorkOrderOperations");

            migrationBuilder.DropTable(
                name: "AccountingVouchers");

            migrationBuilder.DropTable(
                name: "GLAccounts");

            migrationBuilder.DropTable(
                name: "AccountsPayables");

            migrationBuilder.DropTable(
                name: "AgentExecutions");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "AssetStocktakes");

            migrationBuilder.DropTable(
                name: "BudgetYears");

            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "CapacityCalendars");

            migrationBuilder.DropTable(
                name: "CashReceipts");

            migrationBuilder.DropTable(
                name: "CompletionOrders");

            migrationBuilder.DropTable(
                name: "CostElements");

            migrationBuilder.DropTable(
                name: "CrossDeptTasks");

            migrationBuilder.DropTable(
                name: "DeliveryOrders");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Ecns");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "ExpenseBudgets");

            migrationBuilder.DropTable(
                name: "MpsLines");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropTable(
                name: "Investments");

            migrationBuilder.DropTable(
                name: "IpqcReports");

            migrationBuilder.DropTable(
                name: "IqcReports");

            migrationBuilder.DropTable(
                name: "KpiDefinitions");

            migrationBuilder.DropTable(
                name: "LaborBudgets");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "MaterialCostForecasts");

            migrationBuilder.DropTable(
                name: "MaterialIssues");

            migrationBuilder.DropTable(
                name: "MaterialReturns");

            migrationBuilder.DropTable(
                name: "MaterialSupplements");

            migrationBuilder.DropTable(
                name: "MrpRuns");

            migrationBuilder.DropTable(
                name: "PaymentBatches");

            migrationBuilder.DropTable(
                name: "PayrollAnomalies");

            migrationBuilder.DropTable(
                name: "Payrolls");

            migrationBuilder.DropTable(
                name: "PortalMessageThreads");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RccpResources");

            migrationBuilder.DropTable(
                name: "Rfqs");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SalesForecasts");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "ProductionSchedules");

            migrationBuilder.DropTable(
                name: "StocktakeOrders");

            migrationBuilder.DropTable(
                name: "SubcontractOrders");

            migrationBuilder.DropTable(
                name: "SubstituteGroups");

            migrationBuilder.DropTable(
                name: "SupplierAsns");

            migrationBuilder.DropTable(
                name: "SupplierComplaints");

            migrationBuilder.DropTable(
                name: "TransferOrders");

            migrationBuilder.DropTable(
                name: "RndProjects");

            migrationBuilder.DropTable(
                name: "UomGroups");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WipReports");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "AssetCategories");

            migrationBuilder.DropTable(
                name: "CalendarCategories");

            migrationBuilder.DropTable(
                name: "MeetingRooms");

            migrationBuilder.DropTable(
                name: "Ecrs");

            migrationBuilder.DropTable(
                name: "MpsHeaders");

            migrationBuilder.DropTable(
                name: "Asns");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions");

            migrationBuilder.DropTable(
                name: "Boms");

            migrationBuilder.DropTable(
                name: "Routings");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Skus");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "BinLocations");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Uoms");

            migrationBuilder.DropTable(
                name: "JobGrades");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
