using Microsoft.EntityFrameworkCore;
using ErpBackend.Models.System;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.RD;
using ErpBackend.Models.Asset;
using ErpBackend.Models.Finance;
using ErpBackend.Services;
using ErpBackend.Models.Budget;
using ErpBackend.Models.QA;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Digital;
using ErpBackend.Models.Collaboration;

namespace ErpBackend.Data;

public class ErpGeneralDbContext : DbContext
{
    public ErpGeneralDbContext(DbContextOptions<ErpGeneralDbContext> options) : base(options) { }

    // ========== System ==========
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemJob> SystemJobs => Set<SystemJob>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<NotificationQueue> NotificationQueues => Set<NotificationQueue>();
    public DbSet<UserThemePreference> UserThemePreferences => Set<UserThemePreference>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<AgentExecution> AgentExecutions => Set<AgentExecution>();
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();

    // ========== HR ==========
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<JobGrade> JobGrades => Set<JobGrade>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<ApprovalAuthority> ApprovalAuthorities => Set<ApprovalAuthority>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollAnomaly> PayrollAnomalies => Set<PayrollAnomaly>();
    public DbSet<PayrollCorrection> PayrollCorrections => Set<PayrollCorrection>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();

    // ========== Inventory ==========
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseType> WarehouseTypes => Set<WarehouseType>();
    public DbSet<BinLocation> BinLocations => Set<BinLocation>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<UomGroup> UomGroups => Set<UomGroup>();
    public DbSet<UomGroupMember> UomGroupMembers => Set<UomGroupMember>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<ErpBackend.Models.Inventory.InventoryReservation> InventoryReservations => Set<ErpBackend.Models.Inventory.InventoryReservation>();
    public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
    public DbSet<MaterialIssueItem> MaterialIssueItems => Set<MaterialIssueItem>();
    public DbSet<MaterialReturn> MaterialReturns => Set<MaterialReturn>();
    public DbSet<MaterialReturnItem> MaterialReturnItems => Set<MaterialReturnItem>();
    public DbSet<MaterialSupplement> MaterialSupplements => Set<MaterialSupplement>();
    public DbSet<MaterialSupplementItem> MaterialSupplementItems => Set<MaterialSupplementItem>();
    public DbSet<SubcontractOrder> SubcontractOrders => Set<SubcontractOrder>();
    public DbSet<SubcontractItem> SubcontractItems => Set<SubcontractItem>();
    public DbSet<InventoryTransfer> InventoryTransfers => Set<InventoryTransfer>();
    public DbSet<InventoryTransferItem> InventoryTransferItems => Set<InventoryTransferItem>();
    public DbSet<CompletionOrder> CompletionOrders => Set<CompletionOrder>();
    public DbSet<CompletionOrderItem> CompletionOrderItems => Set<CompletionOrderItem>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
    public DbSet<AutoReplenishment> AutoReplenishments => Set<AutoReplenishment>();
    public DbSet<ExpiryAlert> ExpiryAlerts => Set<ExpiryAlert>();
    public DbSet<StocktakeOrder> StocktakeOrders => Set<StocktakeOrder>();
    public DbSet<StocktakeOrderItem> StocktakeOrderItems => Set<StocktakeOrderItem>();
    public DbSet<AsrsTask> AsrsTasks => Set<AsrsTask>();
    
    // ========== Inventory - Substitute ==========
    public DbSet<SubstituteGroup> SubstituteGroups => Set<SubstituteGroup>();
    public DbSet<SubstituteItem> SubstituteItems => Set<SubstituteItem>();
    public DbSet<SubstituteUsageLog> SubstituteUsageLogs => Set<SubstituteUsageLog>();
    
    // ========== MES - MRP ==========
    public DbSet<MrpRun> MrpRuns => Set<MrpRun>();
    public DbSet<MrpRequirement> MrpRequirements => Set<MrpRequirement>();
    public DbSet<MrpPlannedOrder> MrpPlannedOrders => Set<MrpPlannedOrder>();
    public DbSet<SafetyStockLevel> SafetyStockLevels => Set<SafetyStockLevel>();
    public DbSet<DemandForecastExt> DemandForecastExts => Set<DemandForecastExt>();
    
    // ========== MES - APS ==========
    public DbSet<CapacityCalendar> CapacityCalendars => Set<CapacityCalendar>();
    public DbSet<CapacityCalendarEntry> CapacityCalendarEntries => Set<CapacityCalendarEntry>();
    public DbSet<ProductionSchedule> ProductionSchedules => Set<ProductionSchedule>();
    public DbSet<ScheduleTask> ScheduleTasks => Set<ScheduleTask>();
    public DbSet<CapacityLoad> CapacityLoads => Set<CapacityLoad>();
    public DbSet<ScheduleConstraint> ScheduleConstraints => Set<ScheduleConstraint>();
    
    // ========== MES - KPI ==========
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiValue> KpiValues => Set<KpiValue>();
    public DbSet<MachineOperationRecord> MachineOperationRecords => Set<MachineOperationRecord>();
    public DbSet<ScrapRecordExt> ScrapRecordExts => Set<ScrapRecordExt>();

    // ========== MES ==========
    public DbSet<WorkCenter> WorkCenters => Set<WorkCenter>();
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<Routing> Routings => Set<Routing>();
    public DbSet<RoutingOperation> RoutingOperations => Set<RoutingOperation>();
    public DbSet<Bom> Boms => Set<Bom>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<BomPath> BomPaths => Set<BomPath>();
    public DbSet<Ecr> Ecrs => Set<Ecr>();
    public DbSet<Ecn> Ecns => Set<Ecn>();
    public DbSet<EcnAffectedBom> EcnAffectedBoms => Set<EcnAffectedBom>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderMaterial> WorkOrderMaterials => Set<WorkOrderMaterial>();
    public DbSet<WorkOrderOperation> WorkOrderOperations { get; set; } = null!;
    public DbSet<WipReport> WipReports { get; set; } = null!;
    public DbSet<ScrapRecord> ScrapRecords { get; set; } = null!;
    public DbSet<ApsScheduleResult> ApsScheduleResults { get; set; } = null!;
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<EquipmentDowntime> EquipmentDowntimes => Set<EquipmentDowntime>();
    public DbSet<EquipmentHistory> EquipmentHistories => Set<EquipmentHistory>();
    public DbSet<MpsHeader> MpsHeaders => Set<MpsHeader>();
    public DbSet<MpsLine> MpsLines => Set<MpsLine>();
    public DbSet<ForecastConsumptionDetail> ForecastConsumptionDetails => Set<ForecastConsumptionDetail>();
    public DbSet<CalendarBucket> CalendarBuckets => Set<CalendarBucket>();
    public DbSet<MpsException> MpsExceptions => Set<MpsException>();
    public DbSet<CurrencyMaster> Currencies => Set<CurrencyMaster>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<RccpResource> RccpResources => Set<RccpResource>();
    public DbSet<RccpItemResource> RccpItemResources => Set<RccpItemResource>();
    public DbSet<BarcodeRecord> BarcodeRecords => Set<BarcodeRecord>();
    public DbSet<BarcodeScanHistory> BarcodeScanHistories => Set<BarcodeScanHistory>();

    // ========== Sales ==========
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<SalesOrderProductionLink> SalesOrderProductionLinks => Set<SalesOrderProductionLink>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderItem> DeliveryOrderItems => Set<DeliveryOrderItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<CashReceipt> CashReceipts => Set<CashReceipt>();
    public DbSet<CashReceiptAllocation> CashReceiptAllocations => Set<CashReceiptAllocation>();
    public DbSet<MonthlyPeriod> MonthlyPeriods => Set<MonthlyPeriod>();

    // ========== Procurement ==========
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierContact> SupplierContacts => Set<SupplierContact>();
    public DbSet<SupplierBankAccount> SupplierBankAccounts => Set<SupplierBankAccount>();
    public DbSet<SupplierRating> SupplierRatings => Set<SupplierRating>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionItem> PurchaseRequisitionItems => Set<PurchaseRequisitionItem>();
    public DbSet<PurchaseRequisitionApproval> PurchaseRequisitionApprovals => Set<PurchaseRequisitionApproval>();
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<RfqSupplierResponse> RfqSupplierResponses => Set<RfqSupplierResponse>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Asn> Asns => Set<Asn>();
    public DbSet<AsnItem> AsnItems => Set<AsnItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<AccountsPayable> AccountsPayables => Set<AccountsPayable>();
    public DbSet<AccountsPayableItem> AccountsPayableItems => Set<AccountsPayableItem>();
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

     // ========== R&D ==========
     public DbSet<RndProject> RndProjects => Set<RndProject>();
     public DbSet<RndMilestone> RndMilestones => Set<RndMilestone>();
     public DbSet<RndBudget> RndBudgets => Set<RndBudget>();
     public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
     public DbSet<RndLog> RndLogs => Set<RndLog>();
     public DbSet<TrialProduction> TrialProductions => Set<TrialProduction>();
     public DbSet<MassProductionTransition> MassProductionTransitions => Set<MassProductionTransition>();
     public DbSet<Document> Documents => Set<Document>();
     public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
     public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
     public DbSet<SampleRequest> SampleRequests => Set<SampleRequest>();

    // ========== Asset ==========
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetLocation> AssetLocations => Set<AssetLocation>();
    public DbSet<Models.Asset.Asset> Assets => Set<Models.Asset.Asset>();
    public DbSet<AssetDepreciation> AssetDepreciations => Set<AssetDepreciation>();
    public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();
    public DbSet<AssetMaintenance> AssetMaintenances => Set<AssetMaintenance>();
    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();
    public DbSet<AssetStocktake> AssetStocktakes => Set<AssetStocktake>();
    public DbSet<AssetStocktakeItem> AssetStocktakeItems => Set<AssetStocktakeItem>();

    // ========== Finance ==========
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanRepaymentSchedule> LoanRepaymentSchedules => Set<LoanRepaymentSchedule>();
    public DbSet<TreasuryTransfer> TreasuryTransfers => Set<TreasuryTransfer>();
    public DbSet<Guarantee> Guarantees => Set<Guarantee>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentValuation> InvestmentValuations => Set<InvestmentValuation>();

    // ========== Accounting ==========
    public DbSet<GLAccount> GLAccounts => Set<GLAccount>();
    public DbSet<AccountingVoucher> AccountingVouchers => Set<AccountingVoucher>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();

    // ========== Cost Center ==========
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<CostElement> CostElements => Set<CostElement>();
    public DbSet<CostElementAssignment> CostElementAssignments => Set<CostElementAssignment>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<AllocationRule> AllocationRules => Set<AllocationRule>();
    public DbSet<CostCenterPosting> CostCenterPostings => Set<CostCenterPosting>();
    public DbSet<WorkCenterCost> WorkCenterCosts => Set<WorkCenterCost>();

    // ========== Actual Costing ==========
    public DbSet<WorkOrderMaterialActual> WorkOrderMaterialActuals => Set<WorkOrderMaterialActual>();
    public DbSet<WorkOrderLaborActual> WorkOrderLaborActuals => Set<WorkOrderLaborActual>();
    public DbSet<WorkOrderMachineActual> WorkOrderMachineActuals => Set<WorkOrderMachineActual>();
    public DbSet<WorkOrderCost> WorkOrderCosts => Set<WorkOrderCost>();

    // ========== Budget ==========
    public DbSet<BudgetYear> BudgetYears => Set<BudgetYear>();
    public DbSet<BudgetVersion> BudgetVersions => Set<BudgetVersion>();
    public DbSet<SalesForecast> SalesForecasts => Set<SalesForecast>();
    public DbSet<SalesForecastItem> SalesForecastItems => Set<SalesForecastItem>();
    public DbSet<MaterialCostForecast> MaterialCostForecasts => Set<MaterialCostForecast>();
    public DbSet<MaterialCostForecastItem> MaterialCostForecastItems => Set<MaterialCostForecastItem>();
    public DbSet<LaborBudget> LaborBudgets => Set<LaborBudget>();
    public DbSet<LaborBudgetItem> LaborBudgetItems => Set<LaborBudgetItem>();
    public DbSet<ExpenseBudget> ExpenseBudgets => Set<ExpenseBudget>();
    public DbSet<ExpenseBudgetItem> ExpenseBudgetItems => Set<ExpenseBudgetItem>();
    public DbSet<CapexBudget> CapexBudgets => Set<CapexBudget>();

    // ========== QA ==========
    public DbSet<IqcReport> IqcReports => Set<IqcReport>();
    public DbSet<IqcReportItem> IqcReportItems => Set<IqcReportItem>();
    public DbSet<IpqcReport> IpqcReports => Set<IpqcReport>();
    public DbSet<QaReviewLog> QaReviewLogs => Set<QaReviewLog>();
    public DbSet<IpqcReportItem> IpqcReportItems => Set<IpqcReportItem>();
    public DbSet<OqcReport> OqcReports => Set<OqcReport>();
    public DbSet<NonConformanceReport> NonConformanceReports => Set<NonConformanceReport>();
    public DbSet<SupplierComplaint> SupplierComplaints => Set<SupplierComplaint>();
    public DbSet<SupplierCapa> SupplierCapas => Set<SupplierCapa>();

    // ========== Portal ==========
    public DbSet<SupplierUser> SupplierUsers => Set<SupplierUser>();
    public DbSet<SupplierDocument> SupplierDocuments => Set<SupplierDocument>();
    public DbSet<SupplierQuote> SupplierQuotes => Set<SupplierQuote>();
    public DbSet<SupplierPerformanceScore> SupplierPerformanceScores => Set<SupplierPerformanceScore>();
    public DbSet<SupplierAsn> SupplierAsns => Set<SupplierAsn>();
    public DbSet<SupplierAsnItem> SupplierAsnItems => Set<SupplierAsnItem>();
    public DbSet<PortalMessageThread> PortalMessageThreads => Set<PortalMessageThread>();
    public DbSet<PortalMessage> PortalMessages => Set<PortalMessage>();
    public DbSet<SupplierEsgQuestionnaire> SupplierEsgQuestionnaires => Set<SupplierEsgQuestionnaire>();

    // ========== Digital ==========
    public DbSet<GhgEmissionSource> GhgEmissionSources => Set<GhgEmissionSource>();
    public DbSet<GhgEmissionRecord> GhgEmissionRecords => Set<GhgEmissionRecord>();
    public DbSet<CarbonFootprintMapping> CarbonFootprintMappings => Set<CarbonFootprintMapping>();
    public DbSet<AiInsight> AiInsights => Set<AiInsight>();
    public DbSet<AiAnomalyDetection> AiAnomalyDetections => Set<AiAnomalyDetection>();
    public DbSet<AiFormGuidance> AiFormGuidances => Set<AiFormGuidance>();
    public DbSet<LineConfig> LineConfigs => Set<LineConfig>();
    public DbSet<LineUserMapping> LineUserMappings => Set<LineUserMapping>();
    public DbSet<LineNotificationLog> LineNotificationLogs => Set<LineNotificationLog>();
    public DbSet<LineApprovalToken> LineApprovalTokens => Set<LineApprovalToken>();
    public DbSet<AiVisionCapture> AiVisionCaptures => Set<AiVisionCapture>();
    public DbSet<AiStocktakeAnalysis> AiStocktakeAnalyses => Set<AiStocktakeAnalysis>();
    public DbSet<AiStocktakeMission> AiStocktakeMissions => Set<AiStocktakeMission>();
    public DbSet<EsgDashboardConfig> EsgDashboardConfigs => Set<EsgDashboardConfig>();

    // ========== Collaboration ==========
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementReadLog> AnnouncementReadLogs => Set<AnnouncementReadLog>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarCategory> CalendarCategories => Set<CalendarCategory>();
    public DbSet<CalendarShare> CalendarShares => Set<CalendarShare>();
    public DbSet<CompanyHoliday> CompanyHolidays => Set<CompanyHoliday>();
    public DbSet<CalendarReminderLog> CalendarReminderLogs => Set<CalendarReminderLog>();
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();
    public DbSet<MeetingReservation> MeetingReservations => Set<MeetingReservation>();
    public DbSet<CrossDeptTask> CrossDeptTasks => Set<CrossDeptTask>();
    public DbSet<CrossDeptTaskReply> CrossDeptTaskReplies => Set<CrossDeptTaskReply>();
    public DbSet<DepartmentPage> DepartmentPages => Set<DepartmentPage>();

    // 系統消息
    public DbSet<SystemNotification> SystemNotifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== Global Audit Shadow Properties ==========
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType == null) continue;

            if (entityType.ClrType.GetProperty("CreatedBy") == null)
            {
                modelBuilder.Entity(entityType.ClrType).Property<Guid?>("CreatedBy");
            }
            if (entityType.ClrType.GetProperty("ModifiedBy") == null)
            {
                modelBuilder.Entity(entityType.ClrType).Property<Guid?>("ModifiedBy");
            }
            if (entityType.ClrType.GetProperty("CreatedAt") == null)
            {
                var isPostgres = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
                modelBuilder.Entity(entityType.ClrType).Property<DateTime>("CreatedAt")
                    .HasDefaultValueSql(isPostgres ? "now()" : "GETUTCDATE()");
            }
            if (entityType.ClrType.GetProperty("ModifiedAt") == null)
            {
                modelBuilder.Entity(entityType.ClrType).Property<DateTime?>("ModifiedAt");
            }
            // Optional: If you want to force all tables to have a Status field
            if (entityType.ClrType.GetProperty("Status") == null)
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>("Status").HasMaxLength(20).HasDefaultValue("Active");
            }
        }

        // ========== System: Precision & Indexes ==========
        modelBuilder.Entity<AgentExecution>().Property(a => a.Duration).HasColumnType("decimal(18,4)");
        modelBuilder.Entity<AgentExecution>().Property(a => a.SuccessRate).HasColumnType("decimal(18,4)");
        modelBuilder.Entity<AgentTask>().Property(a => a.Duration).HasColumnType("decimal(18,4)");

        // ========== System: Indexes ==========
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionCode).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        modelBuilder.Entity<UserRole>().HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
        modelBuilder.Entity<SystemConfig>().HasIndex(s => s.ConfigKey).IsUnique();

        // ========== HR: Indexes ==========
        modelBuilder.Entity<Department>().HasIndex(d => d.DeptCode).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeNo).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(e => e.NationalId);
        modelBuilder.Entity<AttendanceRecord>().HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
        modelBuilder.Entity<AttendanceRecord>().HasIndex(a => a.Status);
        modelBuilder.Entity<LeaveRequest>().HasIndex(l => new { l.EmployeeId, l.Status });
        modelBuilder.Entity<LeaveRequest>().HasIndex(l => l.ApplicationNo).IsUnique();
        modelBuilder.Entity<OvertimeRequest>().HasIndex(o => new { o.EmployeeId, o.Date });
        modelBuilder.Entity<ShiftSchedule>().HasIndex(s => new { s.EmployeeId, s.YearMonth }).IsUnique();
        modelBuilder.Entity<Payroll>().HasIndex(p => new { p.EmployeeId, p.PeriodStart, p.PeriodEnd });
        modelBuilder.Entity<Payroll>().HasIndex(p => p.Status);
        modelBuilder.Entity<PayrollDetail>().HasOne(pd => pd.Payroll).WithMany(p => p.Details).HasForeignKey(pd => pd.PayrollId);
        modelBuilder.Entity<PayrollAnomaly>().HasOne(pa => pa.Employee).WithMany().HasForeignKey(pa => pa.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollCorrection>().HasOne(pc => pc.Anomaly).WithMany().HasForeignKey(pc => pc.AnomalyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>().HasMany(e => e.Certifications).WithOne(c => c.Employee).HasForeignKey(c => c.EmployeeId);
        modelBuilder.Entity<Employee>().HasMany(e => e.WorkExperiences).WithOne(w => w.Employee).HasForeignKey(w => w.EmployeeId);

        // ========== Inventory: Indexes & Relations ==========
        modelBuilder.Entity<Warehouse>().HasIndex(w => w.WarehouseCode).IsUnique();

        // UOM relations
        modelBuilder.Entity<Uom>().HasIndex(u => u.UomCode).IsUnique();
        modelBuilder.Entity<UomConversion>()
            .HasOne(c => c.FromUom)
            .WithMany(u => u.FromConversions)
            .HasForeignKey(c => c.FromUomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UomConversion>()
            .HasOne(c => c.ToUom)
            .WithMany(u => u.ToConversions)
            .HasForeignKey(c => c.ToUomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UomConversion>().HasIndex(c => new { c.FromUomId, c.ToUomId });
        modelBuilder.Entity<UomGroup>().HasIndex(g => g.GroupCode).IsUnique();
        modelBuilder.Entity<UomGroupMember>().HasIndex(m => new { m.UomGroupId, m.UomId }).IsUnique();

        // Sku relations
        modelBuilder.Entity<Sku>().HasIndex(s => s.SkuNumber).IsUnique();
        modelBuilder.Entity<Sku>().HasIndex(s => s.Barcode);
        modelBuilder.Entity<InventoryLot>().HasIndex(il => new { il.SkuId, il.LotNumber }).IsUnique();
        modelBuilder.Entity<SerialNumber>().HasIndex(sn => sn.SerialNo).IsUnique();
        modelBuilder.Entity<InventoryTransaction>().HasIndex(it => it.CreatedAt);
        modelBuilder.Entity<InventoryTransaction>().HasIndex(it => new { it.SkuId, it.CreatedAt });
        modelBuilder.Entity<MaterialIssue>().HasIndex(mi => mi.IssueNumber).IsUnique();
        modelBuilder.Entity<MaterialIssue>().HasOne<ErpBackend.Models.MES.WorkOrder>().WithMany().HasForeignKey(mi => mi.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialIssueItem>().HasOne(mii => mii.Issue).WithMany(mi => mi.Items).HasForeignKey(mii => mii.IssueId);
        modelBuilder.Entity<MaterialReturn>().HasIndex(mr => mr.ReturnNumber).IsUnique();
        modelBuilder.Entity<MaterialReturnItem>().HasOne(mri => mri.Return).WithMany(mr => mr.Items).HasForeignKey(mri => mri.ReturnId);
        modelBuilder.Entity<MaterialSupplement>().HasIndex(ms => ms.SupplementNumber).IsUnique();
        modelBuilder.Entity<MaterialSupplementItem>().HasOne(msi => msi.Supplement).WithMany(ms => ms.Items).HasForeignKey(msi => msi.SupplementId);
        modelBuilder.Entity<SubcontractOrder>().HasIndex(so => so.SubcontractNumber).IsUnique();
        modelBuilder.Entity<SubcontractItem>().HasOne(si => si.SubcontractOrder).WithMany(so => so.Items).HasForeignKey(si => si.SubcontractOrderId);
        modelBuilder.Entity<InventoryTransfer>().HasIndex(to => to.TransferNumber).IsUnique();
        modelBuilder.Entity<InventoryTransferItem>().HasOne(t => t.InventoryTransfer).WithMany(to => to.Items).HasForeignKey(t => t.InventoryTransferId);
        modelBuilder.Entity<CompletionOrder>().HasIndex(co => co.CompletionNumber).IsUnique();
        modelBuilder.Entity<CompletionOrderItem>().HasOne(coi => coi.CompletionOrder).WithMany(co => co.Items).HasForeignKey(coi => coi.CompletionOrderId);
        modelBuilder.Entity<StocktakeOrder>().HasIndex(so => so.StocktakeNumber).IsUnique();
        modelBuilder.Entity<StocktakeOrderItem>().HasOne(soi => soi.StocktakeOrder).WithMany(so => so.Items).HasForeignKey(soi => soi.StocktakeOrderId);
        modelBuilder.Entity<AsrsTask>().HasIndex(at => at.TaskNumber).IsUnique();

        // ========== Substitute Usage Logs ==========
        modelBuilder.Entity<SubstituteUsageLog>()
            .HasOne(sul => sul.OriginalSku)
            .WithMany(s => s.OriginalSubstituteUsageLogs)
            .HasForeignKey(sul => sul.OriginalSkuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubstituteUsageLog>()
            .HasOne(sul => sul.SubstituteSku)
            .WithMany(s => s.SubstituteUsageLogs)
            .HasForeignKey(sul => sul.SubstituteSkuId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== Substitute Items ==========
        modelBuilder.Entity<SubstituteItem>()
            .HasOne(si => si.Sku)
            .WithMany(s => s.SubstituteItems)
            .HasForeignKey(si => si.SkuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubstituteItem>()
            .HasOne(si => si.Group)
            .WithMany(g => g.Items)
            .HasForeignKey(si => si.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore unused navigation property
        modelBuilder.Entity<Sku>().Ignore(s => s.UsedAsSubstitute);

        // ========== MES: Indexes & Relations ==========
        modelBuilder.Entity<WorkCenter>().HasIndex(wc => wc.WorkCenterCode).IsUnique();
        modelBuilder.Entity<Operation>().HasIndex(o => o.OperationCode).IsUnique();
        modelBuilder.Entity<Routing>().HasIndex(r => r.RoutingCode).IsUnique();
        modelBuilder.Entity<Routing>().HasOne(r => r.ProductSku).WithMany().HasForeignKey(r => r.ProductSkuId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RoutingOperation>().HasOne(ro => ro.Routing).WithMany(r => r.Operations).HasForeignKey(ro => ro.RoutingId);
        modelBuilder.Entity<RoutingOperation>().HasOne(ro => ro.Operation).WithMany().HasForeignKey(ro => ro.OperationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RoutingOperation>().HasOne(ro => ro.WorkCenter).WithMany().HasForeignKey(ro => ro.WorkCenterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Bom>().HasIndex(b => b.BomCode).IsUnique();
        modelBuilder.Entity<BomItem>().HasOne(bi => bi.Bom).WithMany(b => b.Items).HasForeignKey(bi => bi.BomId);
        modelBuilder.Entity<BomPath>().HasIndex(bp => new { bp.AncestorSkuId, bp.DescendantSkuId, bp.Distance });
        modelBuilder.Entity<Ecr>().HasIndex(e => e.EcrNumber).IsUnique();
        modelBuilder.Entity<Ecn>().HasIndex(e => e.EcnNumber).IsUnique();
        modelBuilder.Entity<Ecn>().HasOne(e => e.Ecr).WithMany().HasForeignKey(e => e.EcrId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EcnAffectedBom>().HasOne(eab => eab.Ecn).WithMany(e => e.AffectedBoms).HasForeignKey(eab => eab.EcnId);
        modelBuilder.Entity<WorkOrder>().HasIndex(wo => wo.WoNumber).IsUnique();
        modelBuilder.Entity<WorkOrderMaterial>().HasOne(wom => wom.WorkOrder).WithMany(wo => wo.Materials).HasForeignKey(wom => wom.WorkOrderId);
        modelBuilder.Entity<WorkOrderOperation>().HasOne(woo => woo.WorkOrder).WithMany(wo => wo.Operations).HasForeignKey(woo => woo.WorkOrderId);
        modelBuilder.Entity<WipReport>().HasIndex(wr => wr.ReportNumber).IsUnique();
        modelBuilder.Entity<Equipment>().HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Equipment>().HasOne(e => e.WorkCenter).WithMany().HasForeignKey(e => e.WorkCenterId).OnDelete(DeleteBehavior.SetNull);

        // ========== Sales: Indexes & Relations ==========
        modelBuilder.Entity<Customer>().HasIndex(c => c.CustomerCode).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(c => c.TaxId);
        modelBuilder.Entity<CustomerContact>().HasOne(cc => cc.Customer).WithMany(c => c.Contacts).HasForeignKey(cc => cc.CustomerId);
        modelBuilder.Entity<Product>().HasIndex(p => p.ProductCode).IsUnique();
        modelBuilder.Entity<Quote>().HasIndex(q => q.QuoteNumber).IsUnique();
        modelBuilder.Entity<Quote>().HasOne(q => q.Customer).WithMany(c => c.Quotes).HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QuoteItem>().HasOne(qi => qi.Quote).WithMany(q => q.Items).HasForeignKey(qi => qi.QuoteId);
        modelBuilder.Entity<SalesOrder>().HasIndex(so => so.OrderNumber).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasOne(so => so.Customer).WithMany(c => c.SalesOrders).HasForeignKey(so => so.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrder>().HasOne(so => so.Salesperson).WithMany().HasForeignKey(so => so.SalespersonId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrder>().HasOne(so => so.Department).WithMany().HasForeignKey(so => so.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderItem>().HasOne(soi => soi.SalesOrder).WithMany(so => so.Items).HasForeignKey(soi => soi.SalesOrderId);
        modelBuilder.Entity<SalesOrderItem>().HasIndex(soi => new { soi.SalesOrderId, soi.LineNumber }).IsUnique();
        modelBuilder.Entity<SalesOrderItem>().HasOne(soi => soi.Warehouse).WithMany().HasForeignKey(soi => soi.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderItem>().HasOne(soi => soi.Bom).WithMany().HasForeignKey(soi => soi.BomId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderItem>().HasOne(soi => soi.Routing).WithMany().HasForeignKey(soi => soi.RoutingId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderItem>().HasOne(soi => soi.WorkCenter).WithMany().HasForeignKey(soi => soi.WorkCenterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderProductionLink>().HasIndex(sopl => sopl.SalesOrderItemId);
        modelBuilder.Entity<SalesOrderProductionLink>().HasOne(sopl => sopl.SalesOrderItem).WithMany(soi => soi.ProductionLinks).HasForeignKey(sopl => sopl.SalesOrderItemId);
        modelBuilder.Entity<DeliveryOrder>().HasIndex(d => d.DeliveryNumber).IsUnique();
        modelBuilder.Entity<DeliveryOrder>().HasOne<ErpBackend.Models.Sales.SalesOrder>().WithMany().HasForeignKey(d => d.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrderItem>().HasOne(doi => doi.DeliveryOrder).WithMany(d => d.Items).HasForeignKey(doi => doi.DeliveryOrderId);
        modelBuilder.Entity<SalesInvoice>().HasIndex(si => si.InvoiceNumber).IsUnique();
        modelBuilder.Entity<SalesInvoiceItem>().HasOne(sii => sii.SalesInvoice).WithMany(si => si.Items).HasForeignKey(sii => sii.SalesInvoiceId);
        modelBuilder.Entity<CashReceipt>().HasIndex(cr => cr.ReceiptNumber).IsUnique();
        modelBuilder.Entity<CashReceiptAllocation>().HasOne(cra => cra.CashReceipt).WithMany(cr => cr.Allocations).HasForeignKey(cra => cra.CashReceiptId);
        modelBuilder.Entity<MonthlyPeriod>().HasIndex(mp => new { mp.Year, mp.Month }).IsUnique();

        // ========== Procurement: Indexes & Relations ==========
        modelBuilder.Entity<Supplier>().HasIndex(s => s.SupplierCode).IsUnique();
        modelBuilder.Entity<SupplierContact>().HasOne(sc => sc.Supplier).WithMany(s => s.Contacts).HasForeignKey(sc => sc.SupplierId);
        modelBuilder.Entity<SupplierBankAccount>().HasOne(sba => sba.Supplier).WithMany(s => s.BankAccounts).HasForeignKey(sba => sba.SupplierId);
        modelBuilder.Entity<PurchaseRequisition>().HasIndex(pr => pr.PrNumber).IsUnique();
        modelBuilder.Entity<PurchaseRequisitionItem>().HasOne(pri => pri.PurchaseRequisition).WithMany(pr => pr.Items).HasForeignKey(pri => pri.PurchaseRequisitionId);
        modelBuilder.Entity<Rfq>().HasIndex(r => r.RfqNumber).IsUnique();
        modelBuilder.Entity<RfqItem>().HasOne(ri => ri.Rfq).WithMany(r => r.Items).HasForeignKey(ri => ri.RfqId);
        modelBuilder.Entity<RfqSupplierResponse>().HasOne(rsr => rsr.Rfq).WithMany(r => r.SupplierResponses).HasForeignKey(rsr => rsr.RfqId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasIndex(po => po.PoNumber).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasOne(po => po.Supplier).WithMany().HasForeignKey(po => po.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderItem>().HasOne(poi => poi.PurchaseOrder).WithMany(po => po.Items).HasForeignKey(poi => poi.PurchaseOrderId);
        modelBuilder.Entity<Asn>().HasIndex(a => a.AsnNumber).IsUnique();
        modelBuilder.Entity<AsnItem>().HasOne(ai => ai.Asn).WithMany(a => a.Items).HasForeignKey(ai => ai.AsnId);
        modelBuilder.Entity<GoodsReceipt>().HasIndex(gr => gr.GrNumber).IsUnique();
        modelBuilder.Entity<GoodsReceiptItem>().HasOne(gri => gri.GoodsReceipt).WithMany(gr => gr.Items).HasForeignKey(gri => gri.GoodsReceiptId);
        modelBuilder.Entity<AccountsPayable>().HasIndex(ap => ap.ApNumber).IsUnique();
        modelBuilder.Entity<AccountsPayableItem>().HasOne(api => api.AccountsPayable).WithMany(ap => ap.Items).HasForeignKey(api => api.AccountsPayableId);
        modelBuilder.Entity<PaymentBatch>().HasIndex(pb => pb.BatchNumber).IsUnique();
        modelBuilder.Entity<PaymentRecord>().HasOne(pr => pr.PaymentBatch).WithMany(pb => pb.Records).HasForeignKey(pr => pr.PaymentBatchId);

        // ========== R&D: Indexes ==========
        modelBuilder.Entity<RndProject>().HasIndex(rp => rp.ProjectCode).IsUnique();
        modelBuilder.Entity<RndMilestone>().HasOne(rm => rm.RndProject).WithMany(rp => rp.Milestones).HasForeignKey(rm => rm.RndProjectId);
        modelBuilder.Entity<RndBudget>().HasOne(rb => rb.RndProject).WithMany(rp => rp.Budgets).HasForeignKey(rb => rb.RndProjectId);
        modelBuilder.Entity<Document>().HasIndex(d => d.DocNumber).IsUnique();
        modelBuilder.Entity<DocumentVersion>().HasOne(dv => dv.Document).WithMany(d => d.Versions).HasForeignKey(dv => dv.DocumentId);
        modelBuilder.Entity<SampleRequest>().HasIndex(sr => sr.RequestNumber).IsUnique();

        // ========== Asset: Indexes ==========
        modelBuilder.Entity<AssetCategory>().HasIndex(ac => ac.CategoryCode).IsUnique();
        modelBuilder.Entity<AssetLocation>().HasIndex(al => al.LocationCode).IsUnique();
        modelBuilder.Entity<Models.Asset.Asset>().HasIndex(a => a.AssetNumber).IsUnique();
        modelBuilder.Entity<AssetDepreciation>().HasOne(ad => ad.Asset).WithMany(a => a.Depreciations).HasForeignKey(ad => ad.AssetId);
        modelBuilder.Entity<AssetMaintenance>().HasOne(am => am.Asset).WithMany(a => a.Maintenances).HasForeignKey(am => am.AssetId);
        modelBuilder.Entity<AssetStocktakeItem>().HasOne(asi => asi.AssetStocktake).WithMany(ast => ast.Items).HasForeignKey(asi => asi.AssetStocktakeId);

        // ========== Finance: Indexes ==========
        modelBuilder.Entity<Loan>().HasIndex(l => l.LoanNumber).IsUnique();
        modelBuilder.Entity<LoanRepaymentSchedule>().HasOne(lrs => lrs.Loan).WithMany(l => l.RepaymentSchedules).HasForeignKey(lrs => lrs.LoanId);
        modelBuilder.Entity<TreasuryTransfer>().HasIndex(tt => tt.TransferNumber).IsUnique();
        modelBuilder.Entity<Guarantee>().HasIndex(g => g.GuaranteeNumber).IsUnique();
        modelBuilder.Entity<InvestmentValuation>().HasOne(iv => iv.Investment).WithMany(i => i.Valuations).HasForeignKey(iv => iv.InvestmentId);

        // ========== Cost Center: Indexes & Relations ==========
        modelBuilder.Entity<CostCenter>().HasIndex(cc => cc.CostCenterCode).IsUnique();
        modelBuilder.Entity<CostCenter>().HasOne(cc => cc.ParentCostCenter).WithMany(cc => cc.ChildCostCenters).HasForeignKey(cc => cc.ParentCostCenterId);
        modelBuilder.Entity<CostCenter>().HasOne(cc => cc.Department).WithMany().HasForeignKey(cc => cc.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CostCenter>().HasOne(cc => cc.WorkCenter).WithMany().HasForeignKey(cc => cc.WorkCenterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CostCenter>().HasOne(cc => cc.ManagerEmployee).WithMany().HasForeignKey(cc => cc.ManagerEmployeeId).OnDelete(DeleteBehavior.SetNull);
        
        // ========== MPS: Indexes & Relations ==========
        modelBuilder.Entity<MpsHeader>().HasIndex(m => m.MpsNo).IsUnique();
        modelBuilder.Entity<MpsLine>().HasOne(l => l.Header).WithMany(h => h.Lines).HasForeignKey(l => l.MpsHeaderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ForecastConsumptionDetail>().HasOne(d => d.MpsLine).WithMany(l => l.ConsumptionDetails).HasForeignKey(d => d.MpsLineId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CalendarBucket>().HasIndex(b => new { b.CalendarId, b.BucketCode }).IsUnique();

        // Multi-currency & RCCP
        modelBuilder.Entity<CurrencyMaster>().HasIndex(c => c.CurrencyCode).IsUnique();
        modelBuilder.Entity<ExchangeRate>().HasIndex(e => new { e.FromCurrency, e.ToCurrency, e.ValidFrom });
        modelBuilder.Entity<RccpResource>().HasIndex(r => r.ResourceCode).IsUnique();
        modelBuilder.Entity<RccpItemResource>().HasIndex(ri => new { ri.SkuId, ri.ResourceId }).IsUnique();
        modelBuilder.Entity<RccpItemResource>().HasOne(ri => ri.Sku).WithMany().HasForeignKey(ri => ri.SkuId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RccpItemResource>().HasOne(ri => ri.Resource).WithMany().HasForeignKey(ri => ri.ResourceId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CostElement>().HasIndex(ce => ce.ElementCode).IsUnique();
        modelBuilder.Entity<CostElementAssignment>().HasIndex(cea => new { cea.CostCenterId, cea.CostElementId }).IsUnique();
        modelBuilder.Entity<CostElementAssignment>().HasOne(cea => cea.CostCenter).WithMany(cc => cc.CostElementAssignments).HasForeignKey(cea => cea.CostCenterId);
        modelBuilder.Entity<CostElementAssignment>().HasOne(cea => cea.CostElement).WithMany(ce => ce.Assignments).HasForeignKey(cea => cea.CostElementId);

        modelBuilder.Entity<ActivityType>().HasIndex(at => at.ActivityCode).IsUnique();
        modelBuilder.Entity<ActivityType>().HasOne(at => at.CostCenter).WithMany(cc => cc.ActivityTypes).HasForeignKey(at => at.CostCenterId);

        modelBuilder.Entity<AllocationRule>().HasIndex(ar => new { ar.SourceCostCenterId, ar.TargetCostCenterId });
        modelBuilder.Entity<AllocationRule>().HasOne(ar => ar.SourceCostCenter).WithMany(cc => cc.SourceAllocationRules).HasForeignKey(ar => ar.SourceCostCenterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AllocationRule>().HasOne(ar => ar.TargetCostCenter).WithMany(cc => cc.TargetAllocationRules).HasForeignKey(ar => ar.TargetCostCenterId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CostCenterPosting>().HasOne(cp => cp.CostCenter).WithMany(cc => cc.Postings).HasForeignKey(cp => cp.CostCenterId);
        modelBuilder.Entity<CostCenterPosting>().HasOne(cp => cp.CostElement).WithMany(ce => ce.Postings).HasForeignKey(cp => cp.CostElementId);

        modelBuilder.Entity<WorkCenterCost>().HasIndex(wc => new { wc.WorkCenterId, wc.EffectiveFrom }).IsUnique();
        modelBuilder.Entity<WorkCenterCost>().HasOne(wc => wc.WorkCenter).WithMany().HasForeignKey(wc => wc.WorkCenterId);
        modelBuilder.Entity<WorkCenterCost>().HasOne(wc => wc.CostCenter).WithMany().HasForeignKey(wc => wc.CostCenterId);

        // ========== Accounting: Indexes & Relations ==========
        modelBuilder.Entity<GLAccount>().HasIndex(g => g.AccountCode).IsUnique();
        modelBuilder.Entity<AccountingVoucher>().HasIndex(v => v.VoucherNumber).IsUnique();
        modelBuilder.Entity<AccountingVoucher>().HasIndex(v => v.VoucherDate);
        modelBuilder.Entity<AccountingVoucher>().HasIndex(v => new { v.ReferenceType, v.ReferenceId });
        modelBuilder.Entity<AccountingEntry>().HasOne(ae => ae.Voucher).WithMany(v => v.Entries).HasForeignKey(ae => ae.VoucherId);

        // ========== Budget: Relations ==========
        modelBuilder.Entity<BudgetVersion>().HasOne(bv => bv.BudgetYear).WithMany(by => by.Versions).HasForeignKey(bv => bv.BudgetYearId);
        modelBuilder.Entity<SalesForecastItem>().HasOne(sfi => sfi.SalesForecast).WithMany(sf => sf.Items).HasForeignKey(sfi => sfi.SalesForecastId);
        modelBuilder.Entity<MaterialCostForecastItem>().HasOne(mcfi => mcfi.MaterialCostForecast).WithMany(mcf => mcf.Items).HasForeignKey(mcfi => mcfi.MaterialCostForecastId);
        modelBuilder.Entity<LaborBudgetItem>().HasOne(lbi => lbi.LaborBudget).WithMany(lb => lb.Items).HasForeignKey(lbi => lbi.LaborBudgetId);
        modelBuilder.Entity<ExpenseBudgetItem>().HasOne(ebi => ebi.ExpenseBudget).WithMany(eb => eb.Items).HasForeignKey(ebi => ebi.ExpenseBudgetId);

        // ========== QA: Indexes ==========
        modelBuilder.Entity<IqcReport>().HasIndex(ir => ir.ReportNumber).IsUnique();
        modelBuilder.Entity<IqcReportItem>().HasOne(iri => iri.IqcReport).WithMany(ir => ir.Items).HasForeignKey(iri => iri.IqcReportId);
        modelBuilder.Entity<IpqcReport>().HasIndex(ir => ir.ReportNumber).IsUnique();
        modelBuilder.Entity<IpqcReportItem>().HasOne(ipqi => ipqi.IpqcReport).WithMany(ipqr => ipqr.Items).HasForeignKey(ipqi => ipqi.IpqcReportId);
        modelBuilder.Entity<OqcReport>().HasIndex(oqr => oqr.ReportNumber).IsUnique();
        modelBuilder.Entity<NonConformanceReport>().HasIndex(ncr => ncr.NcrNumber).IsUnique();
        modelBuilder.Entity<SupplierComplaint>().HasIndex(sc => sc.ComplaintNumber).IsUnique();
        modelBuilder.Entity<SupplierCapa>().HasIndex(sc => sc.CapaNumber).IsUnique();
        modelBuilder.Entity<SupplierCapa>().HasOne(sc => sc.Complaint).WithOne(c => c.Capa).HasForeignKey<SupplierCapa>(sc => sc.ComplaintId);
        modelBuilder.Entity<QaReviewLog>().HasIndex(log => new { log.TargetType, log.TargetId, log.CreatedAt });

        // ========== Portal: Indexes ==========
        modelBuilder.Entity<SupplierUser>().HasIndex(su => su.Username).IsUnique();
        modelBuilder.Entity<SupplierQuote>().HasIndex(sq => new { sq.RfqId, sq.SupplierId });
        modelBuilder.Entity<PortalMessage>().HasOne(pm => pm.Thread).WithMany(t => t.Messages).HasForeignKey(pm => pm.ThreadId);
        modelBuilder.Entity<SupplierEsgQuestionnaire>().HasIndex(seq => seq.QuestionnaireNumber).IsUnique();
        modelBuilder.Entity<SupplierEsgQuestionnaire>().HasIndex(seq => seq.SupplierId);

        // ========== Digital: Indexes ==========
        modelBuilder.Entity<LineUserMapping>().HasIndex(lum => lum.LineUserId).IsUnique();
        modelBuilder.Entity<LineApprovalToken>().HasIndex(lat => lat.Token).IsUnique();
        modelBuilder.Entity<AiVisionCapture>().HasIndex(avc => avc.CaptureNumber).IsUnique();
        modelBuilder.Entity<AiVisionCapture>().HasIndex(avc => avc.SkuId);
        modelBuilder.Entity<AiStocktakeAnalysis>().HasIndex(asa => asa.StocktakeOrderId);
        modelBuilder.Entity<AiStocktakeMission>().HasIndex(asm => asm.MissionNumber).IsUnique();
        modelBuilder.Entity<EsgDashboardConfig>().HasIndex(edc => edc.ConfigKey).IsUnique();

        // ========== Collaboration: Relations ==========
        modelBuilder.Entity<AnnouncementReadLog>().HasOne(arl => arl.Announcement).WithMany(a => a.ReadLogs).HasForeignKey(arl => arl.AnnouncementId);
        modelBuilder.Entity<MeetingReservation>().HasOne(mr => mr.Room).WithMany().HasForeignKey(mr => mr.RoomId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CrossDeptTaskReply>().HasOne(cdtr => cdtr.Task).WithMany(t => t.Replies).HasForeignKey(cdtr => cdtr.TaskId);
        modelBuilder.Entity<DepartmentPage>().HasIndex(dp => dp.DepartmentId);
        modelBuilder.Entity<CalendarCategory>().HasIndex(cc => cc.Code).IsUnique();
        modelBuilder.Entity<CalendarEvent>().HasOne(ce => ce.Category).WithMany().HasForeignKey(ce => ce.CategoryCode).HasPrincipalKey(cc => cc.Code);
        modelBuilder.Entity<CalendarEvent>().HasIndex(ce => new { ce.StartDate, ce.EndDate });
        modelBuilder.Entity<CalendarEvent>().HasIndex(ce => ce.SourceModule);
        modelBuilder.Entity<CalendarShare>().HasIndex(cs => cs.CalendarOwnerId);
        modelBuilder.Entity<CompanyHoliday>().HasIndex(ch => ch.Date);
        modelBuilder.Entity<CalendarReminderLog>().HasIndex(crl => new { crl.Status, crl.ScheduledAt });

        // ========== Delete Behavior: Prevent cascade cycles ==========
        // Set default delete behavior to Restrict for FKs that could cause cycles
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (relationship.DeleteBehavior == DeleteBehavior.Cascade)
            {
                // Keep cascade only for parent-child (master-detail) relationships
                // Set Restrict for others to prevent multiple cascade paths
                var principalTable = relationship.PrincipalEntityType.Name;
                var dependentTable = relationship.DeclaringEntityType.Name;

                // Keep cascade for detail lines
                if (dependentTable.EndsWith("Item") || dependentTable.EndsWith("Detail") ||
                    dependentTable.EndsWith("Reply") || dependentTable.EndsWith("Version"))
                {
                    continue; // Keep cascade
                }

                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
        modelBuilder.Entity<ApsScheduleResult>(entity =>
        {
            entity.ToTable("ApsScheduleResults");
            entity.HasIndex(e => new { e.OrderNo, e.ScheduleVersion });
        });
    }
}
