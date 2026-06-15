using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using DotNetEnv;
using ErpBackend.Data;
using ErpBackend.Services;
using ErpBackend.Seeders;
using ErpBackend.Middleware;
using ErpBackend.Filters;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options => {
    options.Conventions.Add(new Microsoft.AspNetCore.Mvc.ApplicationModels.RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
})
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    })
    .AddMvcOptions(options => {
        options.Filters.Add<AuditLogFilter>();
        options.Filters.Add<ValidationFilterAttribute>();
    });
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AuditInterceptor
builder.Services.AddSingleton<AuditInterceptor>();

// Register DbContext with PostgreSQL
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("ErpGeneral")
    ?? throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is required.");
builder.Services.AddDbContext<ErpGeneralDbContext>((sp, options) =>
    options.UseNpgsql(dbConnectionString)
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<UomConversionService>();
builder.Services.AddScoped<ApprovalWorkflowService>();
builder.Services.AddScoped<ICostingService, CostingService>();
builder.Services.AddSingleton<AgentMetricsService>();
builder.Services.AddHostedService<CalendarReminderService>();
builder.Services.AddScoped<ISubstituteService, SubstituteService>();
builder.Services.AddScoped<IMrpEngine, MrpEngine>();
builder.Services.AddScoped<IApsScheduler, ApsScheduler>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IAiSchedulingService, AiSchedulingService>();
builder.Services.AddScoped<ICarbonAccountingService, CarbonAccountingService>();
builder.Services.AddScoped<IAssetDepreciationService, AssetDepreciationService>();
builder.Services.AddScoped<IAIPlanningService, AIPlanningService>();
builder.Services.AddScoped<IToolFactory, ToolFactory>();
builder.Services.AddScoped<IErpIntegrationService, ErpIntegrationService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IProductionAlertService, ProductionAlertService>();
builder.Services.AddScoped<IProductionEventHub, ProductionEventHub>();
builder.Services.AddScoped<IMpsService, MpsService>();
builder.Services.AddScoped<IMrpProcessLinkageService, MrpProcessLinkageService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IStateMachineService, StateMachineService>();
builder.Services.AddScoped<IBudgetControlService, BudgetControlService>();
builder.Services.AddScoped<IInventoryReservationService, InventoryReservationService>();
builder.Services.AddScoped<ILotManagementService, LotManagementService>();
builder.Services.AddScoped<ISupplierRatingService, SupplierRatingService>();
builder.Services.AddHostedService<ExchangeRateSyncService>();
builder.Services.AddHostedService<SupplierRatingSchedulerService>();
builder.Services.AddHostedService<AssetDepreciationSchedulerService>();

var llmConfig = new LlmConfig
{
    Provider = builder.Configuration["LLM:Provider"] ?? Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "openai",
    ApiKey = builder.Configuration["LLM:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "",
    BaseUrl = builder.Configuration["LLM:BaseUrl"] ?? Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://api.openai.com/v1",
    DefaultModel = builder.Configuration["LLM:DefaultModel"] ?? Environment.GetEnvironmentVariable("LLM_DEFAULT_MODEL") ?? "gpt-4",
    MaxTokens = int.Parse(builder.Configuration["LLM:MaxTokens"] ?? Environment.GetEnvironmentVariable("LLM_MAX_TOKENS") ?? "2000"),
    Temperature = double.Parse(builder.Configuration["LLM:Temperature"] ?? "0.7"),
    TimeoutSeconds = int.Parse(builder.Configuration["LLM:TimeoutSeconds"] ?? "60")
};

if (string.IsNullOrEmpty(llmConfig.ApiKey))
{
    Console.WriteLine("WARNING: LLM ApiKey is not configured.");
    Console.WriteLine("Please set in appsettings.json or use environment variables:");
    Console.WriteLine("  - OPENAI_API_KEY (for OpenAI)");
    Console.WriteLine("  - ANTHROPIC_API_KEY (for Claude)");
}
builder.Services.AddSingleton(llmConfig);
builder.Services.AddScoped<ILlmService>(sp => LlmFactory.Create(llmConfig, sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<ILoggerFactory>()));

builder.Services.AddHttpClient();

// Cache Service - In-Memory (Redis optional via REDIS_CONNECTION_STRING)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

// JWT Authentication - 使用環境變數，確保生產環境安全
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required. Please set it in production environment.");

// Validate JWT key length (must be at least 32 characters)
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT_SECRET_KEY must be at least 32 characters for security.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseGlobalExceptionHandler();
app.UseErrorHandling();
app.UseAuthentication();
app.UseAuthorization();

// Prometheus metrics
app.UseHttpMetrics();
app.UseMiddleware<MetricsMiddleware>();
app.UseRequestLogging();
app.MapMetrics();

app.MapControllers();

app.MapGet("/", () => new { status = "ok", service = "ERP Backend", timestamp = DateTime.UtcNow });

app.MapGet("/health", async (ErpGeneralDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.Json(new { status = "unhealthy", database = "disconnected", timestamp = DateTime.UtcNow }, statusCode: 503);
    }
});

app.MapGet("/health/ready", async (ErpGeneralDbContext db) =>
{
    var checks = new Dictionary<string, string>();
    try { await db.Database.CanConnectAsync(); checks["database"] = "ok"; }
    catch { checks["database"] = "error"; }
    
    var isReady = checks.Values.All(v => v == "ok");
    return isReady 
        ? Results.Ok(new { status = "ready", checks, timestamp = DateTime.UtcNow })
        : Results.Json(new { status = "notready", checks, timestamp = DateTime.UtcNow }, statusCode: 503);
});

// Auto-create database on startup (development only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ErpGeneralDbContext>();
    try
    {
        // Database will be created via EF Core Migrations
        // No runtime schema changes needed - use migrations instead
Console.WriteLine("Database ErpGeneral ready via PostgreSQL.");
        
        // Seed default warehouse if empty (done via migrations)
        var seededUoms = await UomSeedData.SeedAsync(db);
        if (seededUoms > 0)
        {
            Console.WriteLine($"Seeded {seededUoms} UOM master records.");
        }

        // Seed Calendar Categories
        if (!db.CalendarCategories.Any())
        {
            var categories = new[]
            {
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "meeting", Name = "會議", Color = "#3B82F6", Icon = "users", SortOrder = 1 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "milestone", Name = "里程碑", Color = "#10B981", Icon = "flag", SortOrder = 2 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "downtime", Name = "停機保養", Color = "#EF4444", Icon = "wrench", SortOrder = 3 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "training", Name = "教育訓練", Color = "#F59E0B", Icon = "book-open", SortOrder = 4 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "leave", Name = "請假", Color = "#6B7280", Icon = "calendar", SortOrder = 5 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "visit", Name = "客戶拜訪", Color = "#8B5CF6", Icon = "map-pin", SortOrder = 6 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "project", Name = "專案", Color = "#6366F1", Icon = "folder", SortOrder = 7 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "holiday", Name = "假日", Color = "#F97316", Icon = "sun", SortOrder = 8 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "task", Name = "任務", Color = "#14B8A6", Icon = "check", SortOrder = 9 },
                new ErpBackend.Models.Collaboration.CalendarCategory { Code = "workorder", Name = "工單", Color = "#A0522D", Icon = "factory", SortOrder = 10 },
            };
            db.CalendarCategories.AddRange(categories);
            db.SaveChanges();

            var holidays = new[]
            {
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "元旦", Date = new DateTime(2026, 1, 1), HolidayType = "national" },
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "除夕", Date = new DateTime(2026, 1, 29), HolidayType = "national" },
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "和平紀念日", Date = new DateTime(2026, 2, 28), HolidayType = "national" },
            };
            db.CompanyHolidays.AddRange(holidays);
            db.SaveChanges();
        }

        if (!db.MeetingRooms.Any())
        {
            var rooms = new[]
            {
                new ErpBackend.Models.Collaboration.MeetingRoom { RoomName = "會議室 A", Location = "1F", Capacity = 8 },
                new ErpBackend.Models.Collaboration.MeetingRoom { RoomName = "會議室 B", Location = "2F", Capacity = 16 },
            };
            db.MeetingRooms.AddRange(rooms);
            db.SaveChanges();
        }

        // Seed default admin user - 使用環境變數控制初始化密碼
        if (!db.Users.Any())
        {
            var adminRole = new ErpBackend.Models.System.Role { RoleName = "Admin", Description = "系統管理員", IsSystem = true };
            db.Roles.Add(adminRole);
            
            // 從環境變數讀取管理員密碼，優先順序: Process > User
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(adminPassword))
                adminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(adminPassword))
            {
                adminPassword = Guid.NewGuid().ToString("N")[..12];
                Console.WriteLine($"WARNING: No ADMIN_INITIAL_PASSWORD set. Generated random password: {adminPassword}");
                Console.WriteLine("Please change password after first login or set ADMIN_INITIAL_PASSWORD environment variable.");
            }
            
            var adminUser = new ErpBackend.Models.System.User 
            { 
                Username = "admin", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword), 
                Email = "admin@erp.local",
                MustChangePassword = true // 強制首次登入必須修改密碼
            };
            db.Users.Add(adminUser);
            db.UserRoles.Add(new ErpBackend.Models.System.UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

            var dept1 = new ErpBackend.Models.HR.Department { DeptCode = "CEO", DeptName = "總經理辦公室" };
            var dept4 = new ErpBackend.Models.HR.Department { DeptCode = "MFG", DeptName = "生產製造部", DeptType = "production" };
            db.Departments.AddRange(dept1, dept4);

            var wh2 = new ErpBackend.Models.Inventory.Warehouse { WarehouseCode = "FW", WarehouseName = "成品倉", Type = "finished_goods" };
            db.Warehouses.Add(wh2);

            var fg001 = new ErpBackend.Models.Inventory.Sku { SkuNumber = "FG-001", SkuName = "精密馬達 A 型", Unit = "SET", StandardCost = 2800, Qty = 120, DefaultWarehouseId = wh2.Id, SourceType = "self_made" };
            db.Skus.Add(fg001);
            db.SaveChanges();
        }

        // Seed MES data
        if (!db.Routings.Any())
        {
            var wc1 = db.WorkCenters.IgnoreQueryFilters().FirstOrDefault(w => w.WorkCenterCode == "WC-CNC")
                ?? new ErpBackend.Models.MES.WorkCenter { WorkCenterCode = "WC-CNC", WorkCenterName = "CNC 加工中心", Capacity = 24 };
            var wc2 = db.WorkCenters.IgnoreQueryFilters().FirstOrDefault(w => w.WorkCenterCode == "WC-ASM")
                ?? new ErpBackend.Models.MES.WorkCenter { WorkCenterCode = "WC-ASM", WorkCenterName = "自動化組裝線", Capacity = 100 };
            if (wc1.Id == Guid.Empty) db.WorkCenters.Add(wc1);
            if (wc2.Id == Guid.Empty) db.WorkCenters.Add(wc2);

            var op1 = db.Operations.IgnoreQueryFilters().FirstOrDefault(o => o.OperationCode == "OP-MAC");
            if (op1 == null)
            {
                op1 = new ErpBackend.Models.MES.Operation { OperationCode = "OP-MAC", OperationName = "精密加工", CostPerHour = 800 };
                db.Operations.Add(op1);
            }

            var op2 = db.Operations.IgnoreQueryFilters().FirstOrDefault(o => o.OperationCode == "OP-ASM");
            if (op2 == null)
            {
                op2 = new ErpBackend.Models.MES.Operation { OperationCode = "OP-ASM", OperationName = "組裝作業", CostPerHour = 500 };
                db.Operations.Add(op2);
            }

            db.SaveChanges();

            var sku = db.Skus.FirstOrDefault(s => s.SkuNumber == "FG-001");
            if (sku != null)
            {
                var r1 = new ErpBackend.Models.MES.Routing { RoutingCode = "R-FG001-STD", RoutingName = "標準工法", ProductSkuId = sku.Id, IsActive = true, Version = "1.0" };
                db.Routings.Add(r1);
                db.SaveChanges();
                db.RoutingOperations.Add(new ErpBackend.Models.MES.RoutingOperation { RoutingId = r1.Id, OperationSeq = 10, OperationId = op1.Id, WorkCenterId = wc1.Id });
                db.RoutingOperations.Add(new ErpBackend.Models.MES.RoutingOperation { RoutingId = r1.Id, OperationSeq = 20, OperationId = op2.Id, WorkCenterId = wc2.Id });
                db.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database seeding failed: {ex.Message}");
    }
}

app.Run();
public class KebabCaseParameterTransformer : Microsoft.AspNetCore.Routing.IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null) return null;
        var str = value.ToString();
        if (string.IsNullOrEmpty(str)) return null;

        // Special cases or just Regex for kebab-case
        return System.Text.RegularExpressions.Regex.Replace(str, "([a-z])([A-Z])", "$1-$2").ToLower();
    }
}
