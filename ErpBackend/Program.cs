using System.Runtime.CompilerServices;
using System.Data.Common;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using DotNetEnv;
using ErpBackend.Configs;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Infrastructure;
using ErpBackend.Services;
using ErpBackend.Middleware;
using ErpBackend.Filters;
using ErpBackend.Security.SupplierPortal;
using Npgsql;

[assembly: InternalsVisibleTo("ErpBackend.IntegrationTests")]

Env.Load();

var builder = WebApplication.CreateBuilder(args);

if (args.Length > 0 && args[0] == "recover-admin")
{
    Environment.ExitCode = await AdminRecoveryCommand.RunAsync(args[1..], builder.Configuration);
    return;
}

// Configure background service exception behavior to not stop host
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

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
        options.Filters.Add<PermissionAuthorizeFilter>();
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
var maxDbPoolSize = int.TryParse(Environment.GetEnvironmentVariable("DB_MAX_POOL_SIZE"), out var configuredMaxPoolSize)
    ? configuredMaxPoolSize
    : 40;
if (maxDbPoolSize is < 1 or > 50)
    throw new InvalidOperationException("DB_MAX_POOL_SIZE must be between 1 and 50 to preserve PostgreSQL connection capacity.");
var npgsqlConnectionString = new NpgsqlConnectionStringBuilder(dbConnectionString)
{
    MaxPoolSize = maxDbPoolSize,
    ApplicationName = "ProMES ERP Backend"
}.ConnectionString;
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<DbConnectionPoolMetricsInterceptor>();
DbConnectionPoolMetricsInterceptor.SetPoolLimit(maxDbPoolSize);
builder.Services.AddDbContext<ErpGeneralDbContext>((sp, options) =>
    options.UseNpgsql(npgsqlConnectionString)
           .AddInterceptors(
               sp.GetRequiredService<AuditInterceptor>(),
               sp.GetRequiredService<DbConnectionPoolMetricsInterceptor>()));

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISkuService, SkuService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
builder.Services.AddScoped<ICompletionOrderService, CompletionOrderService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<ISupplierService, ProcurementService>();
builder.Services.AddScoped<IPurchaseOrderService, ProcurementService>();
builder.Services.AddScoped<IPurchaseRequisitionService, ProcurementService>();
builder.Services.AddScoped<IGoodsReceiptService, ProcurementService>();
builder.Services.AddScoped<IGoodsReceiptReversalService, GoodsReceiptReversalService>();
builder.Services.Configure<AccountingPolicyOptions>(builder.Configuration.GetSection("AccountingPolicy"));
builder.Services.AddScoped<ISalesCreditNoteService, SalesCreditNoteService>();
builder.Services.AddScoped<ISupplierDebitNoteService, SupplierDebitNoteService>();
builder.Services.AddScoped<IAgentDisclosureAuditService, AgentDisclosureAuditService>();
builder.Services.Configure<AgentWipAccessOptions>(builder.Configuration.GetSection("AgentWip"));
builder.Services.AddScoped<IAgentWipQueryService, AgentWipQueryService>();
builder.Services.Configure<AgentLlmEgressOptions>(
    builder.Configuration.GetSection(AgentLlmEgressOptions.SectionName));
builder.Services.AddScoped<IAgentLlmEgressPolicyService, AgentLlmEgressPolicyService>();
builder.Services.AddScoped<PermissionAuthorizeFilter>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUomConversionService, UomConversionService>();
builder.Services.AddScoped<ApprovalWorkflowService>();
builder.Services.AddScoped<ICostingService, CostingService>();
builder.Services.AddScoped<ICostCenterService, CostCenterService>();
builder.Services.AddScoped<IManufacturingCostAllocationService, ManufacturingCostAllocationService>();
builder.Services.AddSingleton<AgentMetricsService>();
builder.Services.AddHostedService<CalendarReminderService>();
builder.Services.AddScoped<ISubstituteService, SubstituteService>();
builder.Services.AddScoped<IMrpEngine, MrpEngine>();
builder.Services.AddScoped<IMrpDataReader, MrpDataReader>();
builder.Services.AddScoped<IPlannedOrderEngine, PlannedOrderEngine>();
builder.Services.AddScoped<IMrpNettingEngine, MrpNettingEngine>();
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<IWorkCenterService, WorkCenterService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ICurrencyMasterService, CurrencyMasterService>();
builder.Services.AddScoped<IJobTitleService, JobTitleService>();
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<ICostElementService, CostElementService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IJobGradeService, JobGradeService>();
builder.Services.AddScoped<ISupplierQuoteService, SupplierQuoteService>();
builder.Services.AddScoped<IEcnService, EcnService>();
builder.Services.AddScoped<IPlanningService, PlanningService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRfqService, RfqService>();
builder.Services.AddScoped<IAccountsPayableService, AccountsPayableService>();
builder.Services.AddScoped<IAttendanceRecordService, AttendanceRecordService>();
builder.Services.AddScoped<IMaterialIssueService, MaterialIssueService>();
builder.Services.AddScoped<IWorkOrderCostingService, WorkOrderCostingService>();
builder.Services.AddScoped<IWorkOrderCostVarianceService, WorkOrderCostVarianceService>();
builder.Services.AddScoped<IWipReportService, WipReportService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IDeliveryOrderService, DeliveryOrderService>();
builder.Services.AddScoped<ICashReceiptService, CashReceiptService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IApsScheduler, ApsScheduler>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IIqcReportService, IqcReportService>();
builder.Services.AddScoped<IProcessInspectionService, ProcessInspectionService>();
builder.Services.AddScoped<IFqcWorkflowService, FqcWorkflowService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ICollaborationService, CollaborationService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUomService, UomService>();
builder.Services.AddScoped<IAiSchedulingService, AiSchedulingService>();
builder.Services.AddScoped<ICarbonAccountingService, CarbonAccountingService>();
builder.Services.AddScoped<IAssetService, AssetService>();
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
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IAccountingVoucherService, AccountingVoucherService>();
builder.Services.AddScoped<IAccountingReportingService, AccountingReportingService>();
builder.Services.AddScoped<IStateMachineService, StateMachineService>();
builder.Services.AddScoped<IBudgetControlService, BudgetControlService>();
builder.Services.AddScoped<IInventoryReservationService, InventoryReservationService>();
builder.Services.AddScoped<ILotManagementService, LotManagementService>();
builder.Services.AddScoped<ISupplierRatingService, SupplierRatingService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPayrollPolicyService, PayrollPolicyService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IApsIntegrationService, ApsIntegrationService>();
builder.Services.AddScoped<IRccpResourceService, RccpResourceService>();
builder.Services.AddScoped<ISupplierPortalIdentityService, SupplierPortalIdentityService>();
builder.Services.AddScoped<ISupplierPortalInvitationService, SupplierPortalInvitationService>();
builder.Services.AddScoped<ISupplierPortalAsnService, SupplierPortalAsnService>();
builder.Services.AddScoped<ISupplierPortalCollaborationService, SupplierPortalCollaborationService>();
builder.Services.AddScoped<ISupplierPortalCapaService, SupplierPortalCapaService>();
builder.Services.AddScoped<ISupplierCapaReviewService, SupplierCapaReviewService>();
builder.Services.AddScoped<ICapaAttachmentService, CapaAttachmentService>();
builder.Services.AddScoped<ISupplierApSubmissionService, SupplierApSubmissionService>();
builder.Services.AddScoped<ISupplierApSubmissionAttachmentService, SupplierApSubmissionAttachmentService>();
builder.Services.Configure<CapaAttachmentOptions>(
    builder.Configuration.GetSection(CapaAttachmentOptions.SectionName));
builder.Services.AddSingleton<ICapaPrivateObjectStorage, MinioCapaObjectStorage>();
builder.Services.AddScoped<ICapaMalwareScanner, ClamAvCapaMalwareScanner>();
builder.Services.AddScoped<SupplierPortalIdentityContext>();
builder.Services.AddScoped<IAuthorizationHandler, SupplierPortalAccessHandler>();
builder.Services.AddHttpClient<IKeycloakSupplierIdentityClient, KeycloakSupplierIdentityClient>();
builder.Services.AddHostedService<ExchangeRateSyncService>();
builder.Services.AddHostedService<SupplierRatingSchedulerService>();
builder.Services.AddHostedService<AssetDepreciationSchedulerService>();

var llmConfig = new LlmConfig
{
    Provider = ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:Provider"], "LLM_PROVIDER", "openai"),
    ApiKey = ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:ApiKey"], "OPENAI_API_KEY", Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? ""),
    BaseUrl = ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:BaseUrl"], "LLM_BASE_URL", "https://api.openai.com/v1"),
    DefaultModel = ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:DefaultModel"], "LLM_DEFAULT_MODEL", "gpt-4"),
    MaxTokens = int.Parse(ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:MaxTokens"], "LLM_MAX_TOKENS", "2000")),
    Temperature = double.Parse(ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:Temperature"], null, "0.7")),
    TimeoutSeconds = int.Parse(ConfigHelper.ResolvePlaceholder(builder.Configuration["LLM:TimeoutSeconds"], null, "60"))
};

if (string.IsNullOrEmpty(llmConfig.ApiKey))
{
    Console.WriteLine("WARNING: LLM ApiKey is not configured.");
    Console.WriteLine("Please set in appsettings.json or use environment variables:");
    Console.WriteLine("  - OPENAI_API_KEY (for OpenAI)");
    Console.WriteLine("  - ANTHROPIC_API_KEY (for Claude)");
}
builder.Services.AddSingleton(llmConfig);
builder.Services.AddHttpClient("LLM")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<ILlmService>(sp =>
    new PolicyGuardedLlmService(
        LlmFactory.Create(
            llmConfig,
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("LLM"),
            sp.GetRequiredService<ILoggerFactory>()),
        sp.GetRequiredService<IAgentLlmEgressPolicyService>(),
        llmConfig));

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
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var securityVersionValue = context.Principal?.FindFirstValue("security_version");
            if (!Guid.TryParse(userIdValue, out var userId)
                || !long.TryParse(
                    securityVersionValue,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var securityVersion))
            {
                context.Fail("Token account security state is invalid.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ErpGeneralDbContext>();
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtAccountStateValidation");
            try
            {
                var account = await (
                    from user in db.Users.AsNoTracking()
                    join employee in db.Employees.AsNoTracking()
                        on user.EmployeeId equals employee.Id into employeeMatches
                    from employee in employeeMatches.DefaultIfEmpty()
                    where user.Id == userId
                    select new
                    {
                        user.IsActive,
                        user.SecurityVersion,
                        user.EmployeeId,
                        EmployeeStatus = employee == null ? null : employee.Status,
                        user.MustChangePassword
                    })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (account == null || !account.IsActive || account.SecurityVersion != securityVersion)
                {
                    context.Fail("Token account security state has changed.");
                    return;
                }

                var tokenRoles = context.Principal?.Claims
                    .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
                    .Select(claim => claim.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var databaseRoles = await db.UserRoles
                    .AsNoTracking()
                    .Where(userRole => userRole.UserId == userId && userRole.Role.IsActive)
                    .Select(userRole => userRole.Role.RoleName)
                    .Distinct()
                    .ToListAsync(context.HttpContext.RequestAborted);
                var employeeIdValue = context.Principal?.FindFirstValue("employee_id");
                var tokenHasEmployee = Guid.TryParse(employeeIdValue, out var employeeId);
                var hasActiveAdminRole = databaseRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                var employeeLinkMatches = account.EmployeeId is Guid accountEmployeeId
                    ? tokenHasEmployee && accountEmployeeId == employeeId
                    : !tokenHasEmployee && hasActiveAdminRole;
                if (tokenRoles == null
                    || !tokenRoles.SetEquals(databaseRoles))
                {
                    context.Fail("Token role bindings do not match current account authorization.");
                    return;
                }
                if (!employeeLinkMatches)
                {
                    context.Fail("Token employee link does not match current account authorization.");
                    return;
                }

                var path = context.HttpContext.Request.Path;
                var isEssRead = HttpMethods.IsGet(context.HttpContext.Request.Method)
                    && path.StartsWithSegments("/api/erp/ess", StringComparison.OrdinalIgnoreCase);
                var isPasswordChange = HttpMethods.IsPost(context.HttpContext.Request.Method)
                    && string.Equals(path.Value, "/api/erp/auth/change-password", StringComparison.OrdinalIgnoreCase);
                var employeeStatus = account.EmployeeStatus?.Trim().ToLowerInvariant();
                if (employeeStatus == "leave")
                {
                    if (!isEssRead)
                        context.Fail("Employees on leave may only access read-only ESS endpoints.");
                }
                else if (employeeStatus != "active" && !(employeeStatus == null && hasActiveAdminRole))
                {
                    context.Fail("Employee status does not allow system access.");
                }

                if (account.MustChangePassword && !isPasswordChange)
                    context.Fail("Password change is required before accessing the system.");
            }
            catch (DbException ex)
            {
                logger.LogError(ex, "Could not validate JWT employee and account state against the database.");
                context.Fail("Account state could not be verified.");
            }
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is DbException)
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAccountStateValidation");
                logger.LogError(context.Exception, "Could not validate JWT account state against the database.");
            }

            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            if (!context.Request.Path.StartsWithSegments(
                    "/api/erp/agent-wip",
                    StringComparison.OrdinalIgnoreCase))
                return;

            var disclosureAudit = context.HttpContext.RequestServices
                .GetRequiredService<IAgentDisclosureAuditService>();
            await disclosureAudit.RecordAsync(
                new AgentDisclosureAuditEntryDto(
                    null,
                    context.HttpContext.TraceIdentifier,
                    "permission_blocked",
                    "unauthenticated_or_invalid_token",
                    null,
                    [],
                    context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    context.HttpContext.Request.Headers.UserAgent.ToString()),
                context.HttpContext.RequestAborted);
        }
    };
});

builder.Services.AddAuthentication()
    .AddJwtBearer(SupplierPortalAuthentication.Scheme, options =>
    {
        var authority = builder.Configuration["SupplierIdentity:Authority"];
        var audience = builder.Configuration["SupplierIdentity:Audience"];

        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = !(builder.Environment.IsDevelopment()
            && builder.Configuration.GetValue<bool>("SupplierIdentity:AllowHttpMetadata"));
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                if (string.IsNullOrWhiteSpace(configuration["SupplierIdentity:Authority"])
                    || string.IsNullOrWhiteSpace(configuration["SupplierIdentity:Audience"]))
                {
                    context.Fail("Supplier identity provider is not configured.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var authenticatedPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = authenticatedPolicy;
    options.FallbackPolicy = authenticatedPolicy;
    options.AddPolicy("AuthenticatedAccount", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(SupplierPortalAuthentication.Policy, policy =>
    {
        policy.AddAuthenticationSchemes(SupplierPortalAuthentication.Scheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SupplierPortalAccessRequirement());
    });
    options.AddPolicy(SupplierPortalAuthentication.InvitationCompletionPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(SupplierPortalAuthentication.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("email_verified", "true");
    });
});

var additionalFrontendOrigins = builder.Configuration
    .GetSection("Cors:AdditionalOrigins")
    .Get<string[]>() ?? [];
var frontendOrigins = new[]
    {
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:5175",
        "http://localhost:5176"
    }
    .Concat(additionalFrontendOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("ETag")
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
app.UseApiVersioning();
app.UseAuthentication();
app.UseAuthorization();

// Prometheus metrics
app.UseHttpMetrics();
app.UseMiddleware<MetricsMiddleware>();
app.UseRequestLogging();
app.MapMetrics().AllowAnonymous();

app.MapControllers();

app.MapGet("/", () => new { status = "ok", service = "ERP Backend", timestamp = DateTime.UtcNow })
    .AllowAnonymous();

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
}).AllowAnonymous();

app.MapGet("/health/ready", async (ErpGeneralDbContext db) =>
{
    var checks = new Dictionary<string, string>();
    try { await db.Database.CanConnectAsync(); checks["database"] = "ok"; }
    catch { checks["database"] = "error"; }
    
    var isReady = checks.Values.All(v => v == "ok");
    return isReady 
        ? Results.Ok(new { status = "ready", checks, timestamp = DateTime.UtcNow })
        : Results.Json(new { status = "notready", checks, timestamp = DateTime.UtcNow }, statusCode: 503);
}).AllowAnonymous();

// Auto-create database on startup (development only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ErpGeneralDbContext>();
    try
    {
        // Database will be created via EF Core Migrations
        // No runtime schema changes needed - use migrations instead
Console.WriteLine("Database ErpGeneral ready via PostgreSQL.");
        await PermissionCatalog.SeedAsync(db);
await RolePermissionCatalog.SeedAsync(db);
        
        // Seed default warehouse if empty (done via migrations)
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
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "元旦", Date = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc), HolidayType = "national" },
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "除夕", Date = DateTime.SpecifyKind(new DateTime(2026, 1, 29), DateTimeKind.Utc), HolidayType = "national" },
                new ErpBackend.Models.Collaboration.CompanyHoliday { Name = "和平紀念日", Date = DateTime.SpecifyKind(new DateTime(2026, 2, 28), DateTimeKind.Utc), HolidayType = "national" },
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

        var financeManagerTitle = db.JobTitles.FirstOrDefault(title =>
            title.TitleCode == "J13" || title.TitleName == "財務主管");
        if (financeManagerTitle == null)
        {
            db.JobTitles.Add(new ErpBackend.Models.HR.JobTitle
            {
                TitleCode = "J13",
                TitleName = "財務主管",
                ApplicableDeptType = "admin",
                IsSupervisor = true,
                IsActive = true,
                Description = "負責財務部門主管職務"
            });
        }
        else
        {
            financeManagerTitle.TitleCode = "J13";
            financeManagerTitle.TitleName = "財務主管";
            financeManagerTitle.ApplicableDeptType = "admin";
            financeManagerTitle.IsSupervisor = true;
            financeManagerTitle.IsActive = true;
        }
        db.SaveChanges();

        // Seed default admin user - 使用環境變數控制初始化密碼
        if (!db.Users.Any())
        {
            var adminRole = db.Roles.SingleOrDefault(role => role.RoleName == "Admin");
            if (adminRole == null)
            {
                adminRole = new ErpBackend.Models.System.Role
                {
                    RoleName = "Admin",
                    Description = "系統管理員",
                    IsSystem = true,
                    IsActive = true
                };
                db.Roles.Add(adminRole);
            }
            else
            {
                adminRole.IsActive = true;
            }
            
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
            var wc1 = new ErpBackend.Models.MES.WorkCenter { WorkCenterCode = "WC-CNC", WorkCenterName = "CNC 加工中心", Capacity = 24 };
            var wc2 = new ErpBackend.Models.MES.WorkCenter { WorkCenterCode = "WC-ASM", WorkCenterName = "自動化組裝線", Capacity = 100 };
            db.WorkCenters.AddRange(wc1, wc2);

            var op1 = new ErpBackend.Models.MES.Operation { OperationCode = "OP-MAC", OperationName = "精密加工", CostPerHour = 800 };
            var op2 = new ErpBackend.Models.MES.Operation { OperationCode = "OP-ASM", OperationName = "組裝作業", CostPerHour = 500 };
            db.Operations.AddRange(op1, op2);
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
