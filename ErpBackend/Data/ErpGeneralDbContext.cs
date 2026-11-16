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
    public DbSet<DisclosureLog> DisclosureLogs => Set<DisclosureLog>();
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
    public DbSet<PayrollPolicyVersion> PayrollPolicyVersions => Set<PayrollPolicyVersion>();
    public DbSet<PayrollPolicyAuditEvent> PayrollPolicyAuditEvents => Set<PayrollPolicyAuditEvent>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollAnomaly> PayrollAnomalies => Set<PayrollAnomaly>();
    public DbSet<PayrollCorrection> PayrollCorrections => Set<PayrollCorrection>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();

    // ========== Inventory ==========
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<BinLocation> BinLocations => Set<BinLocation>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<UomGroup> UomGroups => Set<UomGroup>();
    public DbSet<UomGroupMember> UomGroupMembers => Set<UomGroupMember>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<ProductionReceipt> ProductionReceipts => Set<ProductionReceipt>();
    public DbSet<ProductionReceiptItem> ProductionReceiptItems => Set<ProductionReceiptItem>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
    public DbSet<MaterialIssueItem> MaterialIssueItems => Set<MaterialIssueItem>();
    public DbSet<MaterialIssueItemAllocation> MaterialIssueItemAllocations => Set<MaterialIssueItemAllocation>();
    public DbSet<MaterialReturn> MaterialReturns => Set<MaterialReturn>();
    public DbSet<MaterialReturnItem> MaterialReturnItems => Set<MaterialReturnItem>();
    public DbSet<MaterialSupplement> MaterialSupplements => Set<MaterialSupplement>();
    public DbSet<MaterialSupplementItem> MaterialSupplementItems => Set<MaterialSupplementItem>();
    public DbSet<SubcontractOrder> SubcontractOrders => Set<SubcontractOrder>();
    public DbSet<SubcontractItem> SubcontractItems => Set<SubcontractItem>();
    public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
    public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();
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
    public DbSet<WipReportEquipmentHour> WipReportEquipmentHours => Set<WipReportEquipmentHour>();
    public DbSet<WipReportParticipant> WipReportParticipants => Set<WipReportParticipant>();
    public DbSet<WipReportAuditEvent> WipReportAuditEvents => Set<WipReportAuditEvent>();
    public DbSet<EquipmentUsageRecord> EquipmentUsageRecords => Set<EquipmentUsageRecord>();
    public DbSet<ScrapRecord> ScrapRecords { get; set; } = null!;
    public DbSet<FqcScrapApprovalEvent> FqcScrapApprovalEvents => Set<FqcScrapApprovalEvent>();
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
    public DbSet<RfqSupplierInvitation> RfqSupplierInvitations => Set<RfqSupplierInvitation>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseOrderSupplierResponse> PurchaseOrderSupplierResponses => Set<PurchaseOrderSupplierResponse>();
    public DbSet<PurchaseOrderSupplierResponseItem> PurchaseOrderSupplierResponseItems => Set<PurchaseOrderSupplierResponseItem>();
    public DbSet<Asn> Asns => Set<Asn>();
    public DbSet<AsnItem> AsnItems => Set<AsnItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<AccountsPayable> AccountsPayables => Set<AccountsPayable>();
    public DbSet<AccountsPayableItem> AccountsPayableItems => Set<AccountsPayableItem>();
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<SupplierApSubmission> SupplierApSubmissions => Set<SupplierApSubmission>();
    public DbSet<SupplierApSubmissionRevision> SupplierApSubmissionRevisions => Set<SupplierApSubmissionRevision>();
    public DbSet<SupplierApSubmissionRevisionLine> SupplierApSubmissionRevisionLines => Set<SupplierApSubmissionRevisionLine>();
    public DbSet<SupplierApSubmissionAttachment> SupplierApSubmissionAttachments => Set<SupplierApSubmissionAttachment>();
    public DbSet<SupplierApSubmissionReview> SupplierApSubmissionReviews => Set<SupplierApSubmissionReview>();

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
    public DbSet<GLAccountMapping> GLAccountMappings => Set<GLAccountMapping>();
    public DbSet<AccountingVoucher> AccountingVouchers => Set<AccountingVoucher>();
    public DbSet<SalesCreditNote> SalesCreditNotes => Set<SalesCreditNote>();
    public DbSet<SalesCreditNoteApproval> SalesCreditNoteApprovals => Set<SalesCreditNoteApproval>();
    public DbSet<SupplierDebitNote> SupplierDebitNotes => Set<SupplierDebitNote>();
    public DbSet<SupplierDebitNoteApproval> SupplierDebitNoteApprovals => Set<SupplierDebitNoteApproval>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    public override int SaveChanges()
    {
        ValidateAccountingChanges();
        return base.SaveChanges();
    }

    private void ValidateSupplierDebitNoteMutations()
    {
        foreach (var entry in ChangeTracker.Entries<SupplierDebitNote>()
                     .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.OriginalValues.GetValue<string>(nameof(SupplierDebitNote.Status))
                .Equals("Posted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("已過帳採購借項單不可修改或刪除");
        }

        foreach (var entry in ChangeTracker.Entries<SupplierDebitNote>()
                     .Where(entry => entry.State == EntityState.Added &&
                                     entry.Entity.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("採購借項單必須經過簽核與過帳流程");

        if (ChangeTracker.Entries<SupplierDebitNoteApproval>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("採購借項簽核紀錄不可修改或刪除");

        foreach (var entry in ChangeTracker.Entries<SupplierDebitNoteApproval>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            var note = SupplierDebitNotes.Local.FirstOrDefault(item => item.Id == entry.Entity.SupplierDebitNoteId);
            var isPosted = note is not null
                ? note.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)
                : SupplierDebitNotes.Any(item =>
                    item.Id == entry.Entity.SupplierDebitNoteId &&
                    item.Status.ToUpper() == "POSTED");
            if (isPosted)
                throw new InvalidOperationException("已過帳採購借項單不可新增簽核紀錄");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateAccountingChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await ValidateAccountingChangesAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateAccountingChanges()
    {
        ChangeTracker.DetectChanges();
        ValidatePostedVoucherMutations();
        ValidateSalesCreditNoteMutations();
        ValidateSupplierDebitNoteMutations();
        ValidateWorkOrderCostVarianceMutations();
        ValidateClosedPeriodMutations();
        ValidatePeriodRanges();

        foreach (var voucherEntry in ChangeTracker.Entries<AccountingVoucher>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified &&
                                     entry.Entity.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)))
        {
            AccountingVoucherRules.ValidateForPosting(voucherEntry.Entity);
            var postingDate = DateOnly.FromDateTime(voucherEntry.Entity.VoucherDate);
            if (IsClosedAccountingPeriod(postingDate))
                throw new InvalidOperationException("傳票日期所屬會計期間已關帳，禁止過帳");
        }
    }

    private async Task ValidateAccountingChangesAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        ValidatePostedVoucherMutations();
        ValidateSalesCreditNoteMutations();
        ValidateSupplierDebitNoteMutations();
        ValidateWorkOrderCostVarianceMutations();
        ValidateClosedPeriodMutations();
        await ValidatePeriodRangesAsync(cancellationToken);

        foreach (var voucherEntry in ChangeTracker.Entries<AccountingVoucher>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified &&
                                     entry.Entity.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)))
        {
            AccountingVoucherRules.ValidateForPosting(voucherEntry.Entity);
            var postingDate = DateOnly.FromDateTime(voucherEntry.Entity.VoucherDate);
            if (AccountingPeriods.Local.Any(period =>
                    period.IsClosed &&
                    period.StartDate <= postingDate &&
                    period.EndDate >= postingDate) ||
                await AccountingPeriods.AnyAsync(
                    period => period.IsClosed &&
                              period.StartDate <= postingDate &&
                              period.EndDate >= postingDate,
                    cancellationToken))
                throw new InvalidOperationException("傳票日期所屬會計期間已關帳，禁止過帳");
        }
    }

    private void ValidateWorkOrderCostVarianceMutations()
    {
        foreach (var entry in ChangeTracker.Entries<WorkOrderCostVarianceAllocation>()
                     .Where(entry => entry.State == EntityState.Added &&
                                     !entry.Entity.Status.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("工單成本差異分攤必須經過簽核流程");

        foreach (var entry in ChangeTracker.Entries<WorkOrderCostVarianceAllocation>()
                     .Where(entry => entry.State == EntityState.Deleted ||
                                     entry.State == EntityState.Modified &&
                                     !entry.OriginalValues.GetValue<string>(
                                         nameof(WorkOrderCostVarianceAllocation.Status))
                                     .Equals("PendingApproval", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("已簽核或刪除的工單成本差異分攤不可修改或刪除");

        if (ChangeTracker.Entries<WorkOrderCostVarianceAllocationApproval>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("工單成本差異簽核紀錄不可修改或刪除");
    }

    private bool IsClosedAccountingPeriod(DateOnly postingDate) =>
        AccountingPeriods.Local.Any(period =>
            period.IsClosed &&
            period.StartDate <= postingDate &&
            period.EndDate >= postingDate) ||
        AccountingPeriods.Any(period =>
            period.IsClosed &&
            period.StartDate <= postingDate &&
            period.EndDate >= postingDate);

    private void ValidateClosedPeriodMutations()
    {
        foreach (var entry in ChangeTracker.Entries<AccountingPeriod>()
                     .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.OriginalValues.GetValue<bool>(nameof(AccountingPeriod.IsClosed)))
                throw new InvalidOperationException("已關帳會計期間不可修改或刪除");
        }
    }

    private void ValidatePeriodRanges()
    {
        foreach (var entry in ChangeTracker.Entries<AccountingPeriod>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity.StartDate > entry.Entity.EndDate)
                throw new InvalidOperationException("會計期間起日不可晚於迄日");
            if (HasOverlappingPeriod(entry.Entity))
                throw new InvalidOperationException("會計期間日期不可與既有期間重疊");
        }
    }

    private async Task ValidatePeriodRangesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<AccountingPeriod>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity.StartDate > entry.Entity.EndDate)
                throw new InvalidOperationException("會計期間起日不可晚於迄日");
            if (AccountingPeriods.Local.Any(period =>
                    period.Id != entry.Entity.Id &&
                    Entry(period).State != EntityState.Deleted &&
                    period.StartDate <= entry.Entity.EndDate &&
                    period.EndDate >= entry.Entity.StartDate) ||
                await AccountingPeriods.AnyAsync(
                    period => period.Id != entry.Entity.Id &&
                              period.StartDate <= entry.Entity.EndDate &&
                              period.EndDate >= entry.Entity.StartDate,
                    cancellationToken))
                throw new InvalidOperationException("會計期間日期不可與既有期間重疊");
        }
    }

    private bool HasOverlappingPeriod(AccountingPeriod period) =>
        AccountingPeriods.Local.Any(candidate =>
            candidate.Id != period.Id &&
            Entry(candidate).State != EntityState.Deleted &&
            candidate.StartDate <= period.EndDate &&
            candidate.EndDate >= period.StartDate) ||
        AccountingPeriods.Any(candidate =>
            candidate.Id != period.Id &&
            candidate.StartDate <= period.EndDate &&
            candidate.EndDate >= period.StartDate);

    private void ValidatePostedVoucherMutations()
    {
        var changedVouchers = ChangeTracker.Entries<AccountingVoucher>()
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted);
        foreach (var entry in changedVouchers)
        {
            if (entry.OriginalValues.GetValue<string>(nameof(AccountingVoucher.Status))
                .Equals("Posted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("已過帳傳票不可直接修改或刪除，請建立沖銷傳票");
        }

        var changedLines = ChangeTracker.Entries<AccountingEntry>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        foreach (var entry in changedLines)
        {
            var voucherIds = new HashSet<Guid>();
            if (entry.State != EntityState.Added)
                voucherIds.Add(entry.OriginalValues.GetValue<Guid>(nameof(AccountingEntry.VoucherId)));
            if (entry.State != EntityState.Deleted)
                voucherIds.Add(entry.Entity.VoucherId);

            foreach (var voucherId in voucherIds)
            {
                var trackedVoucher = AccountingVouchers.Local
                    .FirstOrDefault(voucher => voucher.Id == voucherId);
                if (trackedVoucher is not null)
                {
                    var state = Entry(trackedVoucher).State;
                    if (state == EntityState.Added)
                        continue;
                    if (state == EntityState.Deleted ||
                        (state != EntityState.Detached &&
                         trackedVoucher.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("已過帳傳票分錄不可直接修改或刪除，請建立沖銷傳票");
                    continue;
                }

                if (AccountingVouchers.Any(voucher =>
                        voucher.Id == voucherId &&
                        voucher.Status.ToUpper() == "POSTED"))
                    throw new InvalidOperationException("已過帳傳票分錄不可直接修改或刪除，請建立沖銷傳票");
            }
        }
    }

    private void ValidateSalesCreditNoteMutations()
    {
        foreach (var entry in ChangeTracker.Entries<SalesCreditNote>()
                     .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entry.OriginalValues.GetValue<string>(nameof(SalesCreditNote.Status))
                .Equals("Posted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("已過帳銷貨折讓單不可修改或刪除");
        }

        foreach (var entry in ChangeTracker.Entries<SalesCreditNote>()
                     .Where(entry => entry.State == EntityState.Added &&
                                     entry.Entity.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("銷貨折讓單必須經過簽核與過帳流程");

        if (ChangeTracker.Entries<SalesCreditNoteApproval>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("銷貨折讓簽核紀錄不可修改或刪除");

        foreach (var entry in ChangeTracker.Entries<SalesCreditNoteApproval>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            var note = SalesCreditNotes.Local.FirstOrDefault(item => item.Id == entry.Entity.SalesCreditNoteId);
            var isPosted = note is not null
                ? note.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase)
                : SalesCreditNotes.Any(item =>
                    item.Id == entry.Entity.SalesCreditNoteId &&
                    item.Status.ToUpper() == "POSTED");
            if (isPosted)
                throw new InvalidOperationException("已過帳銷貨折讓單不可新增簽核紀錄");
        }
    }

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
    public DbSet<WorkOrderCostVarianceAllocation> WorkOrderCostVarianceAllocations =>
        Set<WorkOrderCostVarianceAllocation>();
    public DbSet<WorkOrderCostVarianceAllocationApproval> WorkOrderCostVarianceAllocationApprovals =>
        Set<WorkOrderCostVarianceAllocationApproval>();

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
    public DbSet<IpqcReportItem> IpqcReportItems => Set<IpqcReportItem>();
    public DbSet<OqcReport> OqcReports => Set<OqcReport>();
    public DbSet<InspectionTemplate> InspectionTemplates => Set<InspectionTemplate>();
    public DbSet<InspectionTemplateSku> InspectionTemplateSkus => Set<InspectionTemplateSku>();
    public DbSet<InspectionTemplateItem> InspectionTemplateItems => Set<InspectionTemplateItem>();
    public DbSet<ProcessInspectionReport> ProcessInspectionReports => Set<ProcessInspectionReport>();
    public DbSet<ProcessInspectionReportItem> ProcessInspectionReportItems => Set<ProcessInspectionReportItem>();
    public DbSet<FqcInspectionLot> FqcInspectionLots => Set<FqcInspectionLot>();
    public DbSet<FqcInspectionFinding> FqcInspectionFindings => Set<FqcInspectionFinding>();
    public DbSet<NonConformanceReport> NonConformanceReports => Set<NonConformanceReport>();
    public DbSet<SupplierComplaint> SupplierComplaints => Set<SupplierComplaint>();
    public DbSet<SupplierCapa> SupplierCapas => Set<SupplierCapa>();
    public DbSet<SupplierCapaResponseRevision> SupplierCapaResponseRevisions => Set<SupplierCapaResponseRevision>();
    public DbSet<SupplierCapaReview> SupplierCapaReviews => Set<SupplierCapaReview>();
    public DbSet<SupplierCapaAttachment> SupplierCapaAttachments => Set<SupplierCapaAttachment>();

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
    public DbSet<AnnouncementRecipient> AnnouncementRecipients => Set<AnnouncementRecipient>();
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
                modelBuilder.Entity(entityType.ClrType).Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
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
        modelBuilder.Entity<User>().Property(u => u.SecurityVersion).HasDefaultValue(0L);
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionCode).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        modelBuilder.Entity<UserRole>().HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
        modelBuilder.Entity<DisclosureLog>().HasIndex(log => log.TraceId);
        modelBuilder.Entity<DisclosureLog>().HasIndex(log => new { log.ActorUserId, log.CreatedAt });
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
        modelBuilder.Entity<PayrollPolicyVersion>().HasIndex(p => p.VersionCode).IsUnique();
        modelBuilder.Entity<PayrollPolicyVersion>().HasIndex(p => new { p.Status, p.EffectiveFrom });
        modelBuilder.Entity<PayrollPolicyAuditEvent>()
            .HasOne(e => e.PolicyVersion)
            .WithMany(p => p.AuditEvents)
            .HasForeignKey(e => e.PayrollPolicyVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollPolicyAuditEvent>().HasIndex(e => new { e.PayrollPolicyVersionId, e.OccurredAt });
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
        modelBuilder.Entity<InventoryLot>()
            .HasIndex(il => new { il.SkuId, il.LotNumber, il.WarehouseId, il.BinLocationId })
            .IsUnique();
        modelBuilder.Entity<SerialNumber>().HasIndex(sn => sn.SerialNo).IsUnique();
        modelBuilder.Entity<InventoryTransaction>().HasIndex(it => it.CreatedAt);
        modelBuilder.Entity<InventoryTransaction>().HasIndex(it => new { it.SkuId, it.CreatedAt });
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne<InventoryTransaction>()
            .WithMany()
            .HasForeignKey(it => it.ReversalOfTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(it => it.ReversalOfTransactionId)
            .IsUnique()
            .HasFilter("\"ReversalOfTransactionId\" IS NOT NULL");
        modelBuilder.Entity<MaterialIssue>().HasIndex(mi => mi.IssueNumber).IsUnique();
        modelBuilder.Entity<MaterialIssue>().HasOne<ErpBackend.Models.MES.WorkOrder>().WithMany().HasForeignKey(mi => mi.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialIssueItem>().HasOne(mii => mii.Issue).WithMany(mi => mi.Items).HasForeignKey(mii => mii.IssueId);
        modelBuilder.Entity<MaterialIssueItem>().HasOne(mii => mii.Sku).WithMany().HasForeignKey(mii => mii.SkuId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialIssue>().HasOne(mi => mi.WorkOrder).WithMany().HasForeignKey(mi => mi.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialIssue>().HasOne(mi => mi.IssueWarehouse).WithMany().HasForeignKey(mi => mi.IssueWarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialReturn>().HasIndex(mr => mr.ReturnNumber).IsUnique();
        modelBuilder.Entity<MaterialReturnItem>().HasOne(mri => mri.Return).WithMany(mr => mr.Items).HasForeignKey(mri => mri.ReturnId);
        modelBuilder.Entity<MaterialSupplement>().HasIndex(ms => ms.SupplementNumber).IsUnique();
        modelBuilder.Entity<MaterialSupplementItem>().HasOne(msi => msi.Supplement).WithMany(ms => ms.Items).HasForeignKey(msi => msi.SupplementId);
        modelBuilder.Entity<SubcontractOrder>().HasIndex(so => so.SubcontractNumber).IsUnique();
        modelBuilder.Entity<SubcontractItem>().HasOne(si => si.SubcontractOrder).WithMany(so => so.Items).HasForeignKey(si => si.SubcontractOrderId);
        modelBuilder.Entity<TransferOrder>().HasIndex(to => to.TransferNumber).IsUnique();
        modelBuilder.Entity<TransferOrderItem>().HasOne(t => t.TransferOrder).WithMany(to => to.Items).HasForeignKey(t => t.TransferOrderId);
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
        modelBuilder.Entity<WipReportEquipmentHour>()
            .HasIndex(hour => new { hour.WipReportId, hour.EquipmentId })
            .IsUnique();
        modelBuilder.Entity<WipReportEquipmentHour>()
            .HasOne(hour => hour.WipReport)
            .WithMany(report => report.EquipmentHours)
            .HasForeignKey(hour => hour.WipReportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WipReportEquipmentHour>()
            .HasOne(hour => hour.Equipment)
            .WithMany()
            .HasForeignKey(hour => hour.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WipReportParticipant>()
            .HasIndex(participant => new { participant.WipReportId, participant.EmployeeId })
            .IsUnique();
        modelBuilder.Entity<WipReportParticipant>()
            .HasOne(participant => participant.WipReport)
            .WithMany(report => report.Participants)
            .HasForeignKey(participant => participant.WipReportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WipReportParticipant>()
            .HasOne(participant => participant.Employee)
            .WithMany()
            .HasForeignKey(participant => participant.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WipReportAuditEvent>()
            .HasOne(auditEvent => auditEvent.WipReport)
            .WithMany(report => report.AuditEvents)
            .HasForeignKey(auditEvent => auditEvent.WipReportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EquipmentUsageRecord>()
            .HasOne(usage => usage.WorkOrder)
            .WithMany()
            .HasForeignKey(usage => usage.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EquipmentUsageRecord>()
            .HasOne(usage => usage.Equipment)
            .WithMany()
            .HasForeignKey(usage => usage.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EquipmentUsageRecord>()
            .HasOne(usage => usage.RecordedByEmployee)
            .WithMany()
            .HasForeignKey(usage => usage.RecordedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EquipmentUsageRecord>()
            .HasIndex(usage => new { usage.WorkOrderId, usage.OperationSeq, usage.EquipmentId, usage.StartTime });
        modelBuilder.Entity<WorkOrderMachineActual>()
            .HasIndex(actual => actual.EquipmentUsageRecordId)
            .IsUnique();
        modelBuilder.Entity<WorkOrderMachineActual>()
            .HasOne<EquipmentUsageRecord>()
            .WithMany()
            .HasForeignKey(actual => actual.EquipmentUsageRecordId)
            .OnDelete(DeleteBehavior.Restrict);
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
        modelBuilder.Entity<BankAccount>().HasOne(account => account.GLAccount).WithMany().HasForeignKey(account => account.GLAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderProductionLink>().HasIndex(sopl => sopl.SalesOrderItemId);
        modelBuilder.Entity<SalesOrderProductionLink>().HasOne(sopl => sopl.SalesOrderItem).WithMany(soi => soi.ProductionLinks).HasForeignKey(sopl => sopl.SalesOrderItemId);
        modelBuilder.Entity<DeliveryOrder>().HasIndex(d => d.DeliveryNumber).IsUnique();
        modelBuilder.Entity<DeliveryOrder>().HasOne<ErpBackend.Models.Sales.SalesOrder>().WithMany().HasForeignKey(d => d.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrderItem>().HasOne(doi => doi.DeliveryOrder).WithMany(d => d.Items).HasForeignKey(doi => doi.DeliveryOrderId);
        modelBuilder.Entity<DeliveryOrderItem>().HasOne(item => item.Warehouse).WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrderItem>().HasOne(item => item.BinLocation).WithMany().HasForeignKey(item => item.BinLocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesInvoice>().HasIndex(si => si.InvoiceNumber).IsUnique();
        modelBuilder.Entity<SalesInvoiceItem>().HasOne(sii => sii.SalesInvoice).WithMany(si => si.Items).HasForeignKey(sii => sii.SalesInvoiceId);
        modelBuilder.Entity<CashReceipt>().HasIndex(cr => cr.ReceiptNumber).IsUnique();
        modelBuilder.Entity<CashReceipt>().Property(cr => cr.Currency).HasDefaultValue("TWD");
        modelBuilder.Entity<CashReceiptAllocation>().HasOne(cra => cra.CashReceipt).WithMany(cr => cr.Allocations).HasForeignKey(cra => cra.CashReceiptId);
        modelBuilder.Entity<CashReceipt>().HasOne(receipt => receipt.BankAccount).WithMany().HasForeignKey(receipt => receipt.BankAccountId).OnDelete(DeleteBehavior.Restrict);
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
        modelBuilder.Entity<RfqSupplierInvitation>().HasIndex(invitation => new { invitation.RfqId, invitation.SupplierId }).IsUnique();
        modelBuilder.Entity<RfqSupplierInvitation>().HasOne(invitation => invitation.Rfq).WithMany(rfq => rfq.SupplierInvitations).HasForeignKey(invitation => invitation.RfqId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RfqSupplierInvitation>().HasOne(invitation => invitation.Supplier).WithMany().HasForeignKey(invitation => invitation.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>().HasIndex(po => po.PoNumber).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasOne(po => po.Supplier).WithMany().HasForeignKey(po => po.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderItem>().HasOne(poi => poi.PurchaseOrder).WithMany(po => po.Items).HasForeignKey(poi => poi.PurchaseOrderId);
        modelBuilder.Entity<PurchaseOrderSupplierResponse>().HasIndex(response => new { response.PurchaseOrderId, response.SubmittedAt });
        modelBuilder.Entity<PurchaseOrderSupplierResponse>().HasOne(response => response.PurchaseOrder).WithMany(po => po.SupplierResponses).HasForeignKey(response => response.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderSupplierResponse>().HasOne(response => response.Supplier).WithMany().HasForeignKey(response => response.SupplierId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderSupplierResponseItem>().HasOne(item => item.SupplierResponse).WithMany(response => response.Items).HasForeignKey(item => item.SupplierResponseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PurchaseOrderSupplierResponseItem>().HasOne(item => item.PurchaseOrderItem).WithMany().HasForeignKey(item => item.PurchaseOrderItemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Asn>().HasIndex(a => a.AsnNumber).IsUnique();
        modelBuilder.Entity<AsnItem>().HasOne(ai => ai.Asn).WithMany(a => a.Items).HasForeignKey(ai => ai.AsnId);
        modelBuilder.Entity<GoodsReceipt>().HasIndex(gr => gr.GrNumber).IsUnique();
        modelBuilder.Entity<GoodsReceiptItem>().HasOne(gri => gri.GoodsReceipt).WithMany(gr => gr.Items).HasForeignKey(gri => gri.GoodsReceiptId);
        modelBuilder.Entity<AccountsPayable>().HasIndex(ap => ap.ApNumber).IsUnique();
        modelBuilder.Entity<AccountsPayableItem>().HasOne(api => api.AccountsPayable).WithMany(ap => ap.Items).HasForeignKey(api => api.AccountsPayableId);
        modelBuilder.Entity<PaymentBatch>().HasIndex(pb => pb.BatchNumber).IsUnique();
        modelBuilder.Entity<PaymentRecord>().HasOne(pr => pr.PaymentBatch).WithMany(pb => pb.Records).HasForeignKey(pr => pr.PaymentBatchId);
        modelBuilder.Entity<SupplierApSubmission>().HasIndex(submission => submission.SubmissionNumber).IsUnique();
        modelBuilder.Entity<SupplierApSubmission>().HasIndex(submission =>
            new { submission.SupplierId, submission.IdempotencyKey }).IsUnique();
        modelBuilder.Entity<SupplierApSubmission>()
            .HasIndex(submission => new { submission.SupplierId, submission.SupplierInvoiceNumberKey })
            .IsUnique()
            .HasFilter("\"SupplierInvoiceNumberKey\" IS NOT NULL");
        modelBuilder.Entity<SupplierApSubmission>()
            .HasIndex(submission => new { submission.SupplierId, submission.Status, submission.CreatedAt });
        modelBuilder.Entity<SupplierApSubmission>()
            .HasIndex(submission => submission.GeneratedAccountsPayableId)
            .IsUnique()
            .HasFilter("\"GeneratedAccountsPayableId\" IS NOT NULL");
        modelBuilder.Entity<SupplierApSubmission>()
            .Property(submission => submission.Version)
            .IsConcurrencyToken();
        modelBuilder.Entity<SupplierApSubmission>()
            .HasOne(submission => submission.Supplier)
            .WithMany()
            .HasForeignKey(submission => submission.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmission>()
            .HasOne(submission => submission.SubmittedBySupplierUser)
            .WithMany()
            .HasForeignKey(submission => submission.SubmittedBySupplierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmission>()
            .HasOne(submission => submission.GeneratedAccountsPayable)
            .WithMany()
            .HasForeignKey(submission => submission.GeneratedAccountsPayableId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevision>()
            .HasIndex(revision => new { revision.SubmissionId, revision.RevisionNumber })
            .IsUnique();
        modelBuilder.Entity<SupplierApSubmissionRevision>()
            .HasOne(revision => revision.Submission)
            .WithMany(submission => submission.Revisions)
            .HasForeignKey(revision => revision.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevision>()
            .HasOne(revision => revision.PurchaseOrder)
            .WithMany()
            .HasForeignKey(revision => revision.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevision>()
            .HasOne(revision => revision.GoodsReceipt)
            .WithMany()
            .HasForeignKey(revision => revision.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevision>()
            .HasOne(revision => revision.SubmittedBySupplierUser)
            .WithMany()
            .HasForeignKey(revision => revision.SubmittedBySupplierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevisionLine>()
            .HasIndex(line => new { line.RevisionId, line.GoodsReceiptItemId })
            .IsUnique();
        modelBuilder.Entity<SupplierApSubmissionRevisionLine>()
            .HasOne(line => line.Revision)
            .WithMany(revision => revision.Lines)
            .HasForeignKey(line => line.RevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevisionLine>()
            .HasOne(line => line.GoodsReceiptItem)
            .WithMany()
            .HasForeignKey(line => line.GoodsReceiptItemId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionRevisionLine>()
            .HasOne(line => line.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(line => line.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionAttachment>()
            .HasIndex(attachment => new { attachment.SubmissionId, attachment.RevokedAt });
        modelBuilder.Entity<SupplierApSubmissionAttachment>()
            .HasIndex(attachment => new { attachment.RevisionId, attachment.ScanStatus });
        modelBuilder.Entity<SupplierApSubmissionAttachment>()
            .HasOne(attachment => attachment.Submission)
            .WithMany(submission => submission.Attachments)
            .HasForeignKey(attachment => attachment.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionAttachment>()
            .HasOne(attachment => attachment.Revision)
            .WithMany()
            .HasForeignKey(attachment => attachment.RevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionAttachment>()
            .HasOne(attachment => attachment.UploadedBySupplierUser)
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedBySupplierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionReview>()
            .HasIndex(review => new { review.SubmissionId, review.CreatedAt });
        modelBuilder.Entity<SupplierApSubmissionReview>()
            .HasOne(review => review.Submission)
            .WithMany(submission => submission.Reviews)
            .HasForeignKey(review => review.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionReview>()
            .HasOne(review => review.Revision)
            .WithMany()
            .HasForeignKey(review => review.RevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionReview>()
            .HasOne(review => review.Reviewer)
            .WithMany()
            .HasForeignKey(review => review.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierApSubmissionReview>()
            .HasOne(review => review.AccountsPayable)
            .WithMany()
            .HasForeignKey(review => review.AccountsPayableId)
            .OnDelete(DeleteBehavior.Restrict);

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
        modelBuilder.Entity<CostCenter>().ToTable(table => table.HasCheckConstraint(
            "CK_CostCenters_AreaSquareMeters_NonNegative",
            "\"AreaSquareMeters\" IS NULL OR \"AreaSquareMeters\" >= 0"));
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
        modelBuilder.Entity<SalesCreditNote>().HasIndex(note => note.CreditNoteNumber).IsUnique();
        modelBuilder.Entity<SalesCreditNote>().HasIndex(note => note.IdempotencyKey).IsUnique();
        modelBuilder.Entity<SalesCreditNote>()
            .HasOne(note => note.SalesInvoice)
            .WithMany()
            .HasForeignKey(note => note.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesCreditNote>()
            .HasOne(note => note.DeliveryOrder)
            .WithMany()
            .HasForeignKey(note => note.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesCreditNoteApproval>()
            .HasIndex(approval => new { approval.SalesCreditNoteId, approval.ApprovalRole })
            .IsUnique();
        modelBuilder.Entity<SalesCreditNoteApproval>()
            .HasIndex(approval => new { approval.SalesCreditNoteId, approval.ApproverId })
            .IsUnique();
        modelBuilder.Entity<SalesCreditNoteApproval>()
            .HasOne(approval => approval.SalesCreditNote)
            .WithMany(note => note.Approvals)
            .HasForeignKey(approval => approval.SalesCreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupplierDebitNote>().HasIndex(note => note.DebitNoteNumber).IsUnique();
        modelBuilder.Entity<SupplierDebitNote>().HasIndex(note => note.IdempotencyKey).IsUnique();
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.AccountsPayable)
            .WithMany()
            .HasForeignKey(note => note.AccountsPayableId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.GoodsReceipt)
            .WithMany()
            .HasForeignKey(note => note.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.GoodsReceiptItem)
            .WithMany()
            .HasForeignKey(note => note.GoodsReceiptItemId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.IqcReport)
            .WithMany()
            .HasForeignKey(note => note.IqcReportId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.Supplier)
            .WithMany()
            .HasForeignKey(note => note.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNote>()
            .HasOne(note => note.Sku)
            .WithMany()
            .HasForeignKey(note => note.SkuId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierDebitNoteApproval>()
            .HasIndex(approval => new { approval.SupplierDebitNoteId, approval.ApprovalRole })
            .IsUnique();
        modelBuilder.Entity<SupplierDebitNoteApproval>()
            .HasIndex(approval => new { approval.SupplierDebitNoteId, approval.ApproverId })
            .IsUnique();
        modelBuilder.Entity<SupplierDebitNoteApproval>()
            .HasOne(approval => approval.SupplierDebitNote)
            .WithMany(note => note.Approvals)
            .HasForeignKey(approval => approval.SupplierDebitNoteId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkOrderCostVarianceAllocation>()
            .HasIndex(allocation => new { allocation.WorkOrderId, allocation.Status });
        modelBuilder.Entity<WorkOrderCostVarianceAllocation>()
            .HasIndex(allocation => new { allocation.SourceType, allocation.SourceId });
        modelBuilder.Entity<WorkOrderCostVarianceAllocation>()
            .HasOne(allocation => allocation.WorkOrder)
            .WithMany()
            .HasForeignKey(allocation => allocation.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkOrderCostVarianceAllocationApproval>()
            .HasIndex(approval => new { approval.AllocationId, approval.ApprovalRole })
            .IsUnique();
        modelBuilder.Entity<WorkOrderCostVarianceAllocationApproval>()
            .HasIndex(approval => new { approval.AllocationId, approval.ApproverId })
            .IsUnique();
        modelBuilder.Entity<WorkOrderCostVarianceAllocationApproval>()
            .HasOne(approval => approval.Allocation)
            .WithMany(allocation => allocation.Approvals)
            .HasForeignKey(approval => approval.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(transaction => new { transaction.RelatedDocType, transaction.RelatedDocId })
            .IsUnique()
            .HasFilter("\"RelatedDocType\" = 'SupplierDebitNote'");
        modelBuilder.Entity<AccountingVoucher>().HasIndex(v => v.VoucherDate);
        modelBuilder.Entity<AccountingVoucher>().HasIndex(v => new { v.ReferenceType, v.ReferenceId });
        modelBuilder.Entity<AccountingVoucher>()
            .HasIndex(v => new { v.ReferenceType, v.ReferenceId, v.PostingPurpose })
            .IsUnique()
            .HasFilter("\"ReferenceType\" IS NOT NULL AND \"ReferenceId\" IS NOT NULL AND \"PostingPurpose\" IS NOT NULL");
        modelBuilder.Entity<AccountingVoucher>()
            .HasIndex(voucher => voucher.ReversalOfVoucherId)
            .IsUnique()
            .HasFilter("\"ReversalOfVoucherId\" IS NOT NULL");
        modelBuilder.Entity<AccountingVoucher>()
            .HasOne(voucher => voucher.ReversalOfVoucher)
            .WithOne(voucher => voucher.ReversedByVoucher)
            .HasForeignKey<AccountingVoucher>(voucher => voucher.ReversalOfVoucherId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GLAccountMapping>()
            .HasIndex(mapping => mapping.MappingKey)
            .IsUnique();
        modelBuilder.Entity<GLAccountMapping>()
            .HasOne(mapping => mapping.GLAccount)
            .WithMany()
            .HasForeignKey(mapping => mapping.GLAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccountingEntry>().HasOne(ae => ae.Voucher).WithMany(v => v.Entries).HasForeignKey(ae => ae.VoucherId);
        modelBuilder.Entity<AccountingEntry>().HasOne(ae => ae.WorkOrder).WithMany().HasForeignKey(ae => ae.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccountingPeriod>().HasIndex(period => period.PeriodCode).IsUnique();

        // ========== Budget: Relations ==========
        modelBuilder.Entity<BudgetVersion>().HasOne(bv => bv.BudgetYear).WithMany(by => by.Versions).HasForeignKey(bv => bv.BudgetYearId);
        modelBuilder.Entity<SalesForecastItem>().HasOne(sfi => sfi.SalesForecast).WithMany(sf => sf.Items).HasForeignKey(sfi => sfi.SalesForecastId);
        modelBuilder.Entity<MaterialCostForecastItem>().HasOne(mcfi => mcfi.MaterialCostForecast).WithMany(mcf => mcf.Items).HasForeignKey(mcfi => mcfi.MaterialCostForecastId);
        modelBuilder.Entity<LaborBudgetItem>().HasOne(lbi => lbi.LaborBudget).WithMany(lb => lb.Items).HasForeignKey(lbi => lbi.LaborBudgetId);
        modelBuilder.Entity<ExpenseBudgetItem>().HasOne(ebi => ebi.ExpenseBudget).WithMany(eb => eb.Items).HasForeignKey(ebi => ebi.ExpenseBudgetId);

        // ========== QA: Indexes ==========
        modelBuilder.Entity<IqcReport>().HasIndex(ir => ir.ReportNumber).IsUnique();
        modelBuilder.Entity<IqcReport>()
            .HasIndex(ir => ir.GoodsReceiptItemId)
            .IsUnique()
            .HasFilter("\"GoodsReceiptItemId\" IS NOT NULL");
        modelBuilder.Entity<IqcReport>()
            .HasOne(ir => ir.GoodsReceipt)
            .WithMany()
            .HasForeignKey(ir => ir.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IqcReport>()
            .HasOne(ir => ir.GoodsReceiptItem)
            .WithMany()
            .HasForeignKey(ir => ir.GoodsReceiptItemId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IqcReport>()
            .HasOne(ir => ir.Sku)
            .WithMany()
            .HasForeignKey(ir => ir.SkuId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IqcReport>()
            .HasOne(ir => ir.ReleaseBinLocation)
            .WithMany()
            .HasForeignKey(ir => ir.ReleaseBinLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IqcReportItem>().HasOne(iri => iri.IqcReport).WithMany(ir => ir.Items).HasForeignKey(iri => iri.IqcReportId);
        modelBuilder.Entity<IpqcReport>().HasIndex(ir => ir.ReportNumber).IsUnique();
        modelBuilder.Entity<IpqcReportItem>().HasOne(ipqi => ipqi.IpqcReport).WithMany(ipqr => ipqr.Items).HasForeignKey(ipqi => ipqi.IpqcReportId);
        modelBuilder.Entity<OqcReport>().HasIndex(oqr => oqr.ReportNumber).IsUnique();
        modelBuilder.Entity<InspectionTemplate>().HasIndex(t => new { t.TemplateCode, t.Version }).IsUnique();
        modelBuilder.Entity<InspectionTemplate>().HasOne(t => t.Operation).WithMany().HasForeignKey(t => t.OperationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InspectionTemplateSku>().HasKey(ts => new { ts.TemplateId, ts.ProductSkuId });
        modelBuilder.Entity<InspectionTemplateSku>().HasOne(ts => ts.Template).WithMany(t => t.ProductSkus)
            .HasForeignKey(ts => ts.TemplateId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InspectionTemplateSku>().HasOne(ts => ts.ProductSku).WithMany()
            .HasForeignKey(ts => ts.ProductSkuId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InspectionTemplateItem>().HasOne(i => i.Template).WithMany(t => t.Items).HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProcessInspectionReport>().HasIndex(r => r.ReportNumber).IsUnique();
        modelBuilder.Entity<ProcessInspectionReport>().HasIndex(r => new { r.WorkOrderId, r.OperationSeq, r.LotNumber });
        modelBuilder.Entity<FqcInspectionLot>().HasIndex(lot => lot.InspectionNumber).IsUnique();
        modelBuilder.Entity<FqcInspectionLot>().HasIndex(lot => new { lot.WorkOrderId, lot.LotNumber }).IsUnique();
        modelBuilder.Entity<FqcInspectionLot>().HasIndex(lot => lot.ReworkSourceInspectionLotId).IsUnique();
        modelBuilder.Entity<FqcInspectionLot>()
            .HasOne(lot => lot.ReworkSourceInspectionLot)
            .WithOne(lot => lot.ReworkInspectionLot)
            .HasForeignKey<FqcInspectionLot>(lot => lot.ReworkSourceInspectionLotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionReceipt>().HasIndex(receipt => receipt.ReceiptNumber).IsUnique();
        modelBuilder.Entity<ProductionReceiptItem>().HasIndex(item => new { item.ProductionReceiptId, item.FqcInspectionLotId }).IsUnique();
        modelBuilder.Entity<ScrapRecord>().HasIndex(item => item.FqcInspectionLotId).IsUnique();
        modelBuilder.Entity<FqcScrapApprovalEvent>().HasIndex(item => new { item.ScrapRecordId, item.OccurredAt });
        modelBuilder.Entity<FqcScrapApprovalEvent>()
            .HasOne(item => item.ScrapRecord)
            .WithMany(record => record.ApprovalEvents)
            .HasForeignKey(item => item.ScrapRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductionReceiptItem>().HasOne(item => item.FqcInspectionLot).WithMany(lot => lot.ReceiptItems).HasForeignKey(item => item.FqcInspectionLotId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FqcInspectionFinding>().HasOne(finding => finding.FqcInspectionLot).WithMany(lot => lot.Findings).HasForeignKey(finding => finding.FqcInspectionLotId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProcessInspectionReport>().HasOne(r => r.WorkOrder).WithMany().HasForeignKey(r => r.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProcessInspectionReport>().HasOne(r => r.Template).WithMany().HasForeignKey(r => r.TemplateId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProcessInspectionReport>().HasOne(r => r.ProductSku).WithMany().HasForeignKey(r => r.ProductSkuId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProcessInspectionReportItem>().HasOne(i => i.Report).WithMany(r => r.Items).HasForeignKey(i => i.ReportId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProcessInspectionReportItem>().HasOne(i => i.TemplateItem).WithMany().HasForeignKey(i => i.TemplateItemId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<NonConformanceReport>().HasIndex(ncr => ncr.NcrNumber).IsUnique();
        modelBuilder.Entity<SupplierComplaint>().HasIndex(sc => sc.ComplaintNumber).IsUnique();
        modelBuilder.Entity<SupplierCapa>().HasIndex(sc => sc.CapaNumber).IsUnique();
        modelBuilder.Entity<SupplierCapa>().HasOne(sc => sc.Complaint).WithOne(c => c.Capa).HasForeignKey<SupplierCapa>(sc => sc.ComplaintId);
        modelBuilder.Entity<SupplierCapaResponseRevision>().HasIndex(revision => new { revision.SupplierCapaId, revision.RevisionNumber }).IsUnique();
        modelBuilder.Entity<SupplierCapaResponseRevision>()
            .HasOne(revision => revision.SupplierCapa)
            .WithMany(capa => capa.ResponseRevisions)
            .HasForeignKey(revision => revision.SupplierCapaId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCapaReview>().HasIndex(review =>
            new { review.SupplierCapaId, review.SupplierCapaResponseRevisionId, review.CreatedAt });
        modelBuilder.Entity<SupplierCapaReview>()
            .HasOne(review => review.SupplierCapa)
            .WithMany(capa => capa.Reviews)
            .HasForeignKey(review => review.SupplierCapaId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCapaReview>()
            .HasOne(review => review.ResponseRevision)
            .WithMany()
            .HasForeignKey(review => review.SupplierCapaResponseRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCapaAttachment>().HasIndex(attachment =>
            new { attachment.SupplierCapaId, attachment.RevokedAt });
        modelBuilder.Entity<SupplierCapaAttachment>()
            .HasOne(attachment => attachment.SupplierCapa)
            .WithMany(capa => capa.Attachments)
            .HasForeignKey(attachment => attachment.SupplierCapaId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierCapaAttachment>()
            .HasOne(attachment => attachment.ResponseRevision)
            .WithMany()
            .HasForeignKey(attachment => attachment.SupplierCapaResponseRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== Portal: Indexes ==========
        modelBuilder.Entity<SupplierUser>().HasIndex(su => su.Username).IsUnique();
        modelBuilder.Entity<SupplierUser>().HasIndex(su => su.IdentityProviderSubject).IsUnique();
        modelBuilder.Entity<SupplierUser>().HasIndex(su => su.InvitationCorrelationId).IsUnique();
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
        modelBuilder.Entity<AnnouncementRecipient>()
            .HasKey(recipient => new { recipient.AnnouncementId, recipient.EmployeeId });
        modelBuilder.Entity<AnnouncementRecipient>()
            .HasOne(recipient => recipient.Announcement)
            .WithMany(announcement => announcement.Recipients)
            .HasForeignKey(recipient => recipient.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AnnouncementRecipient>()
            .HasOne(recipient => recipient.Employee)
            .WithMany()
            .HasForeignKey(recipient => recipient.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
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
                    dependentTable.EndsWith("Reply") || dependentTable.EndsWith("Version") ||
                    relationship.DeclaringEntityType.ClrType == typeof(InspectionTemplateSku))
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
