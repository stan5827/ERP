CREATE TABLE [AccountingVouchers] (
    [Id] uniqueidentifier NOT NULL,
    [VoucherNumber] nvarchar(30) NOT NULL,
    [VoucherDate] datetime2 NOT NULL,
    [VoucherType] nvarchar(20) NOT NULL,
    [Description] nvarchar(200) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [PostedBy] uniqueidentifier NULL,
    [PostedAt] datetime2 NULL,
    [ReferenceType] nvarchar(450) NULL,
    [ReferenceId] uniqueidentifier NULL,
    CONSTRAINT [PK_AccountingVouchers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AccountsPayables] (
    [Id] uniqueidentifier NOT NULL,
    [ApNumber] nvarchar(30) NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [GoodsReceiptId] uniqueidentifier NULL,
    [SupplierInvoiceNumber] nvarchar(50) NULL,
    [SupplierInvoiceDate] datetime2 NULL,
    [InvoiceAmount] decimal(14,2) NOT NULL,
    [TaxAmount] decimal(14,2) NOT NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [DueDate] datetime2 NULL,
    [PaymentTerms] nvarchar(50) NULL,
    [LastAgingDate] datetime2 NULL,
    [VerificationStatus] nvarchar(20) NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [CostCenter] nvarchar(50) NULL,
    [PaymentStatus] nvarchar(20) NOT NULL,
    [ThreeWayMatchResult] nvarchar(20) NOT NULL,
    [AllowanceAmount] decimal(14,2) NULL,
    [DeductionReason] nvarchar(500) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountsPayables] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AgentExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [ExecutionId] nvarchar(50) NOT NULL,
    [PlanId] nvarchar(50) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [SuccessRate] decimal(18,4) NOT NULL,
    [Duration] decimal(18,4) NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [Context] nvarchar(max) NULL,
    [Result] nvarchar(max) NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AgentExecutions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiAnomalyDetections] (
    [Id] uniqueidentifier NOT NULL,
    [TargetType] nvarchar(50) NOT NULL,
    [TargetId] uniqueidentifier NULL,
    [AnomalyLabel] nvarchar(100) NULL,
    [AiExplanation] nvarchar(max) NULL,
    [ConfidenceScore] decimal(5,2) NULL,
    [HumanReviewResult] nvarchar(30) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiAnomalyDetections] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiFormGuidances] (
    [Id] uniqueidentifier NOT NULL,
    [Scenario] nvarchar(100) NULL,
    [UserInput] nvarchar(max) NULL,
    [MappedFields] nvarchar(max) NULL,
    [ConfidenceScore] decimal(5,2) NULL,
    [UserAccepted] bit NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiFormGuidances] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiInsights] (
    [Id] uniqueidentifier NOT NULL,
    [InsightType] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NULL,
    [Summary] nvarchar(max) NULL,
    [DataSources] nvarchar(max) NULL,
    [ConfidenceScore] decimal(5,2) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiInsights] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiStocktakeAnalyses] (
    [Id] uniqueidentifier NOT NULL,
    [StocktakeOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [BinLocationCode] nvarchar(50) NULL,
    [SystemQty] decimal(14,2) NOT NULL,
    [AiCapturedQty] decimal(14,2) NOT NULL,
    [Difference] decimal(14,2) NOT NULL,
    [DifferenceAmount] decimal(14,2) NULL,
    [AiReasoning] nvarchar(200) NULL,
    [AnomalyTag] nvarchar(20) NULL,
    [AiConfidence] decimal(5,2) NULL,
    [ReviewStatus] nvarchar(20) NOT NULL,
    [ReviewedBy] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiStocktakeAnalyses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiStocktakeMissions] (
    [Id] uniqueidentifier NOT NULL,
    [MissionNumber] nvarchar(30) NOT NULL,
    [MissionType] nvarchar(50) NOT NULL,
    [WarehouseId] uniqueidentifier NULL,
    [TargetZone] nvarchar(50) NULL,
    [DeviceType] nvarchar(50) NULL,
    [PathCode] nvarchar(500) NULL,
    [ScheduledAt] datetime2 NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Frequency] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiStocktakeMissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AiVisionCaptures] (
    [Id] uniqueidentifier NOT NULL,
    [CaptureNumber] nvarchar(30) NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [WarehouseCode] nvarchar(50) NULL,
    [BinLocationCode] nvarchar(50) NULL,
    [RecognizedSkuNumber] nvarchar(50) NULL,
    [AiCountedQty] decimal(14,2) NOT NULL,
    [ConfidenceScore] decimal(5,2) NOT NULL,
    [ScreenshotPath] nvarchar(500) NULL,
    [DeviceId] nvarchar(50) NULL,
    [CaptureType] nvarchar(50) NULL,
    [CapturedAt] datetime2 NOT NULL,
    [IsReviewed] bit NOT NULL,
    [ReviewedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiVisionCaptures] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Announcements] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Content] nvarchar(max) NULL,
    [PublishDeptId] uniqueidentifier NULL,
    [PublishBy] uniqueidentifier NULL,
    [PublishDate] datetime2 NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [TargetAudience] nvarchar(50) NOT NULL,
    [IsPinned] bit NOT NULL,
    [IsUrgent] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Asns] (
    [Id] uniqueidentifier NOT NULL,
    [AsnNumber] nvarchar(30) NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [ShipDate] datetime2 NOT NULL,
    [ExpectedArrival] datetime2 NULL,
    [Carrier] nvarchar(100) NULL,
    [TrackingNumber] nvarchar(100) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Asns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AsrsTasks] (
    [Id] uniqueidentifier NOT NULL,
    [TaskNumber] nvarchar(30) NOT NULL,
    [TaskType] nvarchar(30) NOT NULL,
    [SourceLocation] nvarchar(100) NULL,
    [TargetLocation] nvarchar(100) NULL,
    [Lpn] nvarchar(50) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ErrorCode] nvarchar(50) NULL,
    [ErpSentAt] datetime2 NULL,
    [AsrsCompletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AsrsTasks] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AssetCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryCode] nvarchar(20) NOT NULL,
    [CategoryName] nvarchar(100) NOT NULL,
    [DepreciationMethod] nvarchar(30) NOT NULL,
    [DefaultUsefulLife] int NOT NULL,
    [DefaultResidualRate] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_AssetCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AssetDisposals] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [DisposalReason] nvarchar(50) NOT NULL,
    [AccumulatedDepreciation] decimal(14,2) NOT NULL,
    [BookValue] decimal(14,2) NOT NULL,
    [DisposalIncome] decimal(14,2) NOT NULL,
    [DisposalGainLoss] decimal(14,2) NOT NULL,
    [DisposalDate] datetime2 NOT NULL,
    [EnvironmentalCertNo] nvarchar(100) NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssetDisposals] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AssetLocations] (
    [Id] uniqueidentifier NOT NULL,
    [LocationCode] nvarchar(20) NOT NULL,
    [LocationName] nvarchar(100) NOT NULL,
    [Address] nvarchar(300) NULL,
    CONSTRAINT [PK_AssetLocations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AssetStocktakes] (
    [Id] uniqueidentifier NOT NULL,
    [StocktakeNumber] nvarchar(30) NOT NULL,
    [StocktakeDate] datetime2 NOT NULL,
    [StocktakeType] nvarchar(30) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_AssetStocktakes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AssetTransfers] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [TransferType] nvarchar(30) NOT NULL,
    [FromDeptId] uniqueidentifier NULL,
    [ToDeptId] uniqueidentifier NULL,
    [FromLocationId] uniqueidentifier NULL,
    [ToLocationId] uniqueidentifier NULL,
    [FromCustodianId] uniqueidentifier NULL,
    [ToCustodianId] uniqueidentifier NULL,
    [TransferDate] datetime2 NOT NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssetTransfers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AutoReplenishments] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [CurrentQty] decimal(14,2) NOT NULL,
    [Rop] decimal(14,2) NOT NULL,
    [SuggestedQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RelatedPrId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AutoReplenishments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BankAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [BankCode] nvarchar(10) NULL,
    [BankName] nvarchar(100) NULL,
    [BranchName] nvarchar(50) NULL,
    [AccountNumber] nvarchar(30) NULL,
    [AccountName] nvarchar(100) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BarcodeRecords] (
    [Id] uniqueidentifier NOT NULL,
    [Barcode] nvarchar(50) NOT NULL,
    [BarcodeType] nvarchar(20) NOT NULL,
    [RefId] uniqueidentifier NULL,
    [RefType] nvarchar(50) NULL,
    [RefName] nvarchar(100) NULL,
    [LocationCode] nvarchar(30) NULL,
    [Qty] decimal(14,2) NULL,
    [Status] nvarchar(20) NOT NULL,
    [LastScanTime] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BarcodeRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BarcodeScanHistories] (
    [Id] uniqueidentifier NOT NULL,
    [Barcode] nvarchar(50) NOT NULL,
    [BarcodeType] nvarchar(20) NOT NULL,
    [Action] nvarchar(20) NOT NULL,
    [Result] nvarchar(20) NOT NULL,
    [OperatorId] uniqueidentifier NULL,
    [OperatorName] nvarchar(100) NULL,
    [Details] nvarchar(500) NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_BarcodeScanHistories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BomPaths] (
    [Id] uniqueidentifier NOT NULL,
    [AncestorSkuId] uniqueidentifier NOT NULL,
    [DescendantSkuId] uniqueidentifier NOT NULL,
    [Distance] int NOT NULL,
    [PathId] nvarchar(50) NULL,
    CONSTRAINT [PK_BomPaths] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Boms] (
    [Id] uniqueidentifier NOT NULL,
    [BomCode] nvarchar(30) NOT NULL,
    [ProductSkuId] uniqueidentifier NOT NULL,
    [Version] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Boms] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BudgetYears] (
    [Id] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BudgetYears] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CalendarCategories] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [Color] nvarchar(10) NOT NULL,
    [Icon] nvarchar(50) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CalendarCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_CalendarCategories_Code] UNIQUE ([Code])
);
GO


CREATE TABLE [CalendarShares] (
    [Id] uniqueidentifier NOT NULL,
    [CalendarOwnerId] uniqueidentifier NOT NULL,
    [SharedWithEmployeeId] uniqueidentifier NULL,
    [SharedWithDeptId] uniqueidentifier NULL,
    [SharedWithRoleId] uniqueidentifier NULL,
    [Permission] nvarchar(20) NOT NULL,
    [CanSeeDetails] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CalendarShares] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CapexBudgets] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetVersionId] uniqueidentifier NOT NULL,
    [ProjectName] nvarchar(200) NOT NULL,
    [PlannedPurchaseDate] datetime2 NULL,
    [EstimatedAmount] decimal(18,2) NOT NULL,
    [UsefulLife] int NOT NULL,
    [FundingSource] nvarchar(50) NULL,
    [EstimatedMonthlyDepreciation] decimal(14,2) NOT NULL,
    [PlannedCommissionDate] datetime2 NULL,
    [Roi] decimal(8,2) NULL,
    [PaybackPeriodYears] decimal(5,1) NULL,
    CONSTRAINT [PK_CapexBudgets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CarbonFootprintMappings] (
    [Id] uniqueidentifier NOT NULL,
    [GhgEmissionRecordId] uniqueidentifier NOT NULL,
    [SourceDocType] nvarchar(50) NULL,
    [SourceDocId] uniqueidentifier NULL,
    [ActivityData] decimal(18,4) NOT NULL,
    [EmissionFactor] decimal(12,6) NOT NULL,
    [CalculatedCo2e] decimal(18,4) NOT NULL,
    [MappingStatus] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_CarbonFootprintMappings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CashReceipts] (
    [Id] uniqueidentifier NOT NULL,
    [ReceiptNumber] nvarchar(30) NOT NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [PaymentMethod] nvarchar(30) NOT NULL,
    [ReceivedAmount] decimal(14,2) NOT NULL,
    [DiscountAmount] decimal(14,2) NOT NULL,
    [BankAccountId] uniqueidentifier NULL,
    [ReferenceNumber] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [HandlingFee] decimal(14,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CashReceipts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CompanyHolidays] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Date] datetime2 NOT NULL,
    [HolidayType] nvarchar(20) NOT NULL,
    [IsRecurring] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CompanyHolidays] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CompletionOrders] (
    [Id] uniqueidentifier NOT NULL,
    [CompletionNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [WarehouseId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [QaStatus] nvarchar(20) NOT NULL,
    [ReceivedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CompletionOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CostElements] (
    [Id] uniqueidentifier NOT NULL,
    [ElementCode] nvarchar(20) NOT NULL,
    [ElementName] nvarchar(100) NOT NULL,
    [ElementType] nvarchar(20) NOT NULL,
    [GLAccountCode] nvarchar(50) NULL,
    [GLAccountName] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CostElements] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CrossDeptTasks] (
    [Id] uniqueidentifier NOT NULL,
    [TaskName] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Urgency] nvarchar(20) NOT NULL,
    [InitiatedDeptId] uniqueidentifier NULL,
    [InitiatedBy] uniqueidentifier NULL,
    [OwnerDeptId] uniqueidentifier NULL,
    [OwnerBy] uniqueidentifier NULL,
    [Assistants] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [AssignDate] datetime2 NOT NULL,
    [ExpectedCompleteDate] datetime2 NULL,
    [ActualCompleteDate] datetime2 NULL,
    [VerificationNotes] nvarchar(500) NULL,
    [SatisfactionScore] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CrossDeptTasks] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerCode] nvarchar(20) NOT NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [CustomerShortName] nvarchar(50) NULL,
    [TaxId] nvarchar(10) NULL,
    [ContactPerson] nvarchar(50) NULL,
    [ContactTitle] nvarchar(50) NULL,
    [Phone] nvarchar(30) NULL,
    [Mobile] nvarchar(30) NULL,
    [Email] nvarchar(200) NULL,
    [FixedDiscount] decimal(5,4) NOT NULL,
    [BillingDay] int NOT NULL,
    [CollectionDay] int NOT NULL,
    [InvoiceMethod] nvarchar(20) NOT NULL,
    [CreditLimit] decimal(14,2) NOT NULL,
    [CreditStatus] nvarchar(20) NOT NULL,
    [CompanyType] nvarchar(20) NOT NULL,
    [Fax] nvarchar(30) NULL,
    [PaymentTerms] nvarchar(50) NULL,
    [TaxCategory] nvarchar(20) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [PriceLevel] nvarchar(20) NULL,
    [Industry] nvarchar(100) NULL,
    [Region] nvarchar(100) NULL,
    [SalesTerritory] nvarchar(100) NULL,
    [ApprovalStatus] nvarchar(20) NOT NULL,
    [SalesPersonId] uniqueidentifier NULL,
    [Address] nvarchar(500) NULL,
    [InvoiceAddress] nvarchar(500) NULL,
    [LeadSource] nvarchar(50) NULL,
    [ChurnRisk] bit NULL,
    [LastPurchaseDate] datetime2 NULL,
    [SatisfactionScore] int NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [DemandForecasts] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [ForecastMonth] nvarchar(7) NOT NULL,
    [ForecastQty] decimal(14,2) NOT NULL,
    [Algorithm] nvarchar(50) NOT NULL,
    [Accuracy] decimal(5,2) NULL,
    [ActualQty] decimal(14,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DemandForecasts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [DepartmentPages] (
    [Id] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    [PageTitle] nvarchar(100) NULL,
    [PageContent] nvarchar(max) NULL,
    [SopsJson] nvarchar(max) NULL,
    [KpiDataJson] nvarchar(max) NULL,
    [QuickLinksJson] nvarchar(max) NULL,
    [PrimaryContactId] uniqueidentifier NULL,
    [IsPublished] bit NOT NULL,
    [LastUpdatedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DepartmentPages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Departments] (
    [Id] uniqueidentifier NOT NULL,
    [DeptCode] nvarchar(20) NOT NULL,
    [DeptName] nvarchar(100) NOT NULL,
    [DeptType] nvarchar(20) NULL,
    [ParentDeptId] uniqueidentifier NULL,
    [CostCenterCode] nvarchar(20) NULL,
    [ManagerEmployeeId] uniqueidentifier NULL,
    [IsActive] bit NOT NULL,
    [IsCostCenter] bit NOT NULL,
    [BudgetLimit] decimal(14,2) NULL,
    [BudgetUsed] decimal(14,2) NULL,
    [Description] nvarchar(200) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_Departments_ParentDeptId] FOREIGN KEY ([ParentDeptId]) REFERENCES [Departments] ([Id])
);
GO


CREATE TABLE [DocumentPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [CanView] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDownload] bit NOT NULL,
    [CanPrint] bit NOT NULL,
    [WatermarkEnabled] bit NOT NULL,
    CONSTRAINT [PK_DocumentPermissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Documents] (
    [Id] uniqueidentifier NOT NULL,
    [DocNumber] nvarchar(30) NOT NULL,
    [DocName] nvarchar(200) NOT NULL,
    [DocType] nvarchar(50) NOT NULL,
    [CurrentVersion] nvarchar(20) NOT NULL,
    [ProjectId] uniqueidentifier NULL,
    [RelatedSkuId] uniqueidentifier NULL,
    [RelatedEcnId] uniqueidentifier NULL,
    [LifecycleStatus] nvarchar(20) NOT NULL,
    [ConfidentialityLevel] nvarchar(30) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Ecrs] (
    [Id] uniqueidentifier NOT NULL,
    [EcrNumber] nvarchar(30) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ChangeReason] nvarchar(500) NULL,
    [Priority] nvarchar(20) NOT NULL,
    [TargetBomId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [SubmittedBy] uniqueidentifier NULL,
    [SubmittedAt] datetime2 NULL,
    [ReviewedBy] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Ecrs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [EquipmentHistories] (
    [Id] uniqueidentifier NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [RelatedRecordId] uniqueidentifier NULL,
    [EventTime] datetime2 NOT NULL,
    CONSTRAINT [PK_EquipmentHistories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [EsgDashboardConfigs] (
    [Id] uniqueidentifier NOT NULL,
    [ConfigKey] nvarchar(50) NOT NULL,
    [KpiName] nvarchar(200) NULL,
    [KpiCategory] nvarchar(50) NULL,
    [VisualizationType] nvarchar(20) NULL,
    [TargetAudience] nvarchar(50) NULL,
    [AlertThreshold] decimal(14,4) NULL,
    [AlertLevel] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EsgDashboardConfigs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ExpenseBudgets] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetVersionId] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ExpenseBudgets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ExpiryAlerts] (
    [Id] uniqueidentifier NOT NULL,
    [LotId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [AlertLevel] nvarchar(20) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [DaysRemaining] int NOT NULL,
    [Acknowledged] bit NOT NULL,
    [AcknowledgedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExpiryAlerts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GhgEmissionRecords] (
    [Id] uniqueidentifier NOT NULL,
    [SourceId] uniqueidentifier NOT NULL,
    [Period] nvarchar(7) NOT NULL,
    [ActivityData] decimal(18,4) NOT NULL,
    [Unit] nvarchar(20) NULL,
    [CalculatedEmissions] decimal(18,4) NOT NULL,
    [SourceDocumentType] nvarchar(50) NULL,
    [SourceDocumentId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GhgEmissionRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GhgEmissionSources] (
    [Id] uniqueidentifier NOT NULL,
    [SourceCode] nvarchar(30) NOT NULL,
    [SourceName] nvarchar(200) NOT NULL,
    [FacilityId] uniqueidentifier NULL,
    [Scope] nvarchar(20) NOT NULL,
    [MachineId] uniqueidentifier NULL,
    [EnergyType] nvarchar(50) NULL,
    [EmissionFactor] decimal(12,6) NOT NULL,
    [GwpValue] decimal(8,2) NULL,
    CONSTRAINT [PK_GhgEmissionSources] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GLAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [AccountCode] nvarchar(20) NOT NULL,
    [AccountName] nvarchar(100) NOT NULL,
    [AccountType] nvarchar(20) NOT NULL,
    [IsCashAccount] bit NOT NULL,
    [ParentAccountId] uniqueidentifier NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GLAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GLAccounts_GLAccounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [GLAccounts] ([Id])
);
GO


CREATE TABLE [GoodsReceipts] (
    [Id] uniqueidentifier NOT NULL,
    [GrNumber] nvarchar(30) NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [AsnId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [GrDate] datetime2 NOT NULL,
    [WarehouseId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [IqcStatus] nvarchar(20) NOT NULL,
    [SupplierInvoiceNo] nvarchar(50) NULL,
    [PackingListNo] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ExchangeRate] decimal(12,6) NOT NULL,
    [TotalTariff] decimal(14,2) NOT NULL,
    [TotalFreight] decimal(14,2) NOT NULL,
    [TotalLandedCost] decimal(14,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Guarantees] (
    [Id] uniqueidentifier NOT NULL,
    [GuaranteeNumber] nvarchar(30) NOT NULL,
    [GuaranteeType] nvarchar(50) NOT NULL,
    [Beneficiary] nvarchar(200) NULL,
    [ApplicationDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [GuaranteeAmount] decimal(18,2) NOT NULL,
    [FeeRate] decimal(5,2) NOT NULL,
    [FeePaid] decimal(14,2) NOT NULL,
    [RelatedPoId] uniqueidentifier NULL,
    [RelatedContractNo] nvarchar(50) NULL,
    [ReleaseDate] datetime2 NULL,
    [ReleaseAmount] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Guarantees] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Investments] (
    [Id] uniqueidentifier NOT NULL,
    [InvestmentName] nvarchar(200) NOT NULL,
    [InvestmentCode] nvarchar(30) NULL,
    [InvestmentCategory] nvarchar(50) NOT NULL,
    [PurchaseDate] datetime2 NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,4) NOT NULL,
    [Fees] decimal(14,2) NOT NULL,
    [Tax] decimal(14,2) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [AccountingClassification] nvarchar(50) NULL,
    [CurrentMarketValue] decimal(18,2) NOT NULL,
    [UnrealizedGainLoss] decimal(18,2) NOT NULL,
    [DividendIncome] decimal(18,2) NOT NULL,
    [DisposalGainLoss] decimal(18,2) NOT NULL,
    [ShareholdingPercentage] decimal(5,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Investments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [IpqcReports] (
    [Id] uniqueidentifier NOT NULL,
    [ReportNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [OperationSeq] int NOT NULL,
    [WorkCenterId] uniqueidentifier NULL,
    [MachineId] uniqueidentifier NULL,
    [LotNumber] nvarchar(50) NULL,
    [InspectionType] nvarchar(30) NOT NULL,
    [InspectionTime] datetime2 NULL,
    [CpkValue] decimal(8,4) NULL,
    [AbnormalDescription] nvarchar(1000) NULL,
    [IsLineStopped] bit NOT NULL,
    [Result] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_IpqcReports] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [JobGrades] (
    [Id] uniqueidentifier NOT NULL,
    [GradeCode] nvarchar(10) NOT NULL,
    [GradeName] nvarchar(50) NOT NULL,
    [MinSalary] decimal(14,2) NOT NULL,
    [MaxSalary] decimal(14,2) NOT NULL,
    [OvertimeRate] decimal(4,2) NOT NULL,
    [TravelLimit] decimal(14,2) NOT NULL,
    [ApprovalLimit] decimal(14,2) NOT NULL,
    [Level] int NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_JobGrades] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [KpiDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [KpiCode] nvarchar(50) NOT NULL,
    [KpiName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Category] nvarchar(20) NOT NULL,
    [Formula] nvarchar(1000) NULL,
    [Unit] nvarchar(20) NOT NULL,
    [TargetValue] decimal(14,4) NULL,
    [WarningThreshold] decimal(14,4) NULL,
    [CriticalThreshold] decimal(14,4) NULL,
    [CalculationFrequency] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_KpiDefinitions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LaborBudgets] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetVersionId] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [DepartmentId] uniqueidentifier NOT NULL,
    [CurrentHeadcount] int NOT NULL,
    [CurrentSalaryTotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_LaborBudgets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LineApprovalTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(100) NOT NULL,
    [DocType] nvarchar(50) NOT NULL,
    [DocId] uniqueidentifier NOT NULL,
    [Action] nvarchar(20) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    [UsedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LineApprovalTokens] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LineConfigs] (
    [Id] uniqueidentifier NOT NULL,
    [ChannelId] nvarchar(50) NULL,
    [ChannelSecret] nvarchar(200) NULL,
    [WebhookUrl] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LineConfigs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LineNotificationLogs] (
    [Id] uniqueidentifier NOT NULL,
    [NotificationType] nvarchar(50) NOT NULL,
    [TargetLineUserId] nvarchar(50) NULL,
    [TargetGroupId] nvarchar(50) NULL,
    [MessageContent] nvarchar(max) NULL,
    [SendStatus] nvarchar(20) NOT NULL,
    [SentAt] datetime2 NULL,
    [DeliveredAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LineNotificationLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LineUserMappings] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [LineUserId] nvarchar(50) NOT NULL,
    [LineDisplayName] nvarchar(100) NULL,
    [BoundAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LineUserMappings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Loans] (
    [Id] uniqueidentifier NOT NULL,
    [LoanNumber] nvarchar(30) NOT NULL,
    [BankName] nvarchar(200) NULL,
    [BranchName] nvarchar(100) NULL,
    [LoanAmount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [AnnualInterestRate] decimal(8,4) NOT NULL,
    [InterestType] nvarchar(30) NOT NULL,
    [LoanDate] datetime2 NOT NULL,
    [MaturityDate] datetime2 NOT NULL,
    [RepaymentMethod] nvarchar(50) NOT NULL,
    [CollateralInfo] nvarchar(500) NULL,
    [Guarantor] nvarchar(100) NULL,
    [Status] nvarchar(20) NOT NULL,
    [NextInterestAmount] decimal(18,2) NULL,
    [NextInterestDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Loans] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MaintenancePlans] (
    [Id] uniqueidentifier NOT NULL,
    [PlanNumber] nvarchar(30) NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [MaintenanceType] nvarchar(50) NOT NULL,
    [Frequency] nvarchar(20) NOT NULL,
    [FrequencyDays] int NULL,
    [Description] nvarchar(500) NULL,
    [CheckpointList] nvarchar(100) NULL,
    [NextDueDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MaintenancePlans] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MaintenanceRecords] (
    [Id] uniqueidentifier NOT NULL,
    [RecordNumber] nvarchar(30) NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [MaintenancePlanId] uniqueidentifier NULL,
    [MaintenanceType] nvarchar(50) NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [PerformedBy] uniqueidentifier NULL,
    [WorkDescription] nvarchar(1000) NULL,
    [PartsReplaced] nvarchar(500) NULL,
    [LaborHours] decimal(14,2) NOT NULL,
    [PartsCost] decimal(14,2) NOT NULL,
    [TotalCost] decimal(14,2) NOT NULL,
    [Result] nvarchar(20) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MaterialCostForecasts] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetVersionId] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Quarter] nvarchar(10) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaterialCostForecasts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MaterialReturns] (
    [Id] uniqueidentifier NOT NULL,
    [ReturnNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [ReturnWarehouseId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [ReturnedBy] uniqueidentifier NULL,
    [ReturnedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaterialReturns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MaterialSupplements] (
    [Id] uniqueidentifier NOT NULL,
    [SupplementNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaterialSupplements] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MeetingRooms] (
    [Id] uniqueidentifier NOT NULL,
    [RoomName] nvarchar(50) NOT NULL,
    [Location] nvarchar(100) NULL,
    [Capacity] int NOT NULL,
    [Equipment] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MeetingRooms] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MonthlyPeriods] (
    [Id] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Month] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [ClosedBy] uniqueidentifier NULL,
    [ClosedAt] datetime2 NULL,
    [LockedBy] uniqueidentifier NULL,
    [LockedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [OrderAmount] decimal(18,2) NOT NULL,
    [InvoiceAmount] decimal(18,2) NOT NULL,
    [CollectedAmount] decimal(18,2) NOT NULL,
    [OutstandingAR] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_MonthlyPeriods] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MpsPlans] (
    [Id] uniqueidentifier NOT NULL,
    [PlanNumber] nvarchar(30) NOT NULL,
    [ProductSkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [SourceType] nvarchar(20) NOT NULL,
    [SalesOrderId] uniqueidentifier NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MpsPlans] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MpsSalesForecasts] (
    [Id] uniqueidentifier NOT NULL,
    [ProductSkuId] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Month] int NOT NULL,
    [ForecastQty] decimal(14,2) NOT NULL,
    [ActualQty] decimal(14,2) NOT NULL,
    [SourceType] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MpsSalesForecasts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MrpRuns] (
    [Id] uniqueidentifier NOT NULL,
    [RunNumber] nvarchar(30) NOT NULL,
    [RunType] nvarchar(20) NOT NULL,
    [PlanningHorizonStart] datetime2 NOT NULL,
    [PlanningHorizonEnd] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [TotalItemsProcessed] int NOT NULL,
    [PlannedOrdersGenerated] int NOT NULL,
    [PurchaseRequisitionsGenerated] int NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [TriggeredBy] uniqueidentifier NULL,
    CONSTRAINT [PK_MrpRuns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NonConformanceReports] (
    [Id] uniqueidentifier NOT NULL,
    [NcrNumber] nvarchar(30) NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [DefectType] nvarchar(50) NULL,
    [DefectQty] decimal(14,2) NOT NULL,
    [DefectDescription] nvarchar(1000) NULL,
    [Disposition] nvarchar(30) NULL,
    [CostImpact] decimal(14,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_NonConformanceReports] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NotificationQueues] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [TargetUserId] uniqueidentifier NULL,
    [Title] nvarchar(200) NULL,
    [Content] nvarchar(max) NULL,
    [Channel] nvarchar(50) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [SentAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_NotificationQueues] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Operations] (
    [Id] uniqueidentifier NOT NULL,
    [OperationCode] nvarchar(20) NOT NULL,
    [OperationName] nvarchar(100) NOT NULL,
    [SetupTime] decimal(8,2) NOT NULL,
    [RunTime] decimal(8,2) NOT NULL,
    [CostPerHour] decimal(10,2) NOT NULL,
    [IsOutsourced] bit NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Operations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [OqcReports] (
    [Id] uniqueidentifier NOT NULL,
    [ReportNumber] nvarchar(30) NOT NULL,
    [DeliveryOrderId] uniqueidentifier NOT NULL,
    [InspectionDate] datetime2 NOT NULL,
    [InspectorId] uniqueidentifier NOT NULL,
    [PackagingCheck] bit NOT NULL,
    [ReliabilityTestResult] nvarchar(200) NULL,
    [FinalDecision] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_OqcReports] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PaymentBatches] (
    [Id] uniqueidentifier NOT NULL,
    [BatchNumber] nvarchar(30) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(30) NOT NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [BankAccountId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PaymentBatches] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Permissions] (
    [Id] uniqueidentifier NOT NULL,
    [ParentId] uniqueidentifier NULL,
    [PermissionCode] nvarchar(100) NOT NULL,
    [ModuleName] nvarchar(50) NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [PermissionType] nvarchar(20) NOT NULL,
    [Description] nvarchar(200) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Permissions_Permissions_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Permissions] ([Id])
);
GO


CREATE TABLE [PortalMessageThreads] (
    [Id] uniqueidentifier NOT NULL,
    [Subject] nvarchar(200) NULL,
    [RelatedDocType] nvarchar(50) NULL,
    [RelatedDocId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PortalMessageThreads] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ProductionSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [ScheduleNumber] nvarchar(30) NOT NULL,
    [ScheduleType] nvarchar(20) NOT NULL,
    [ScheduleDate] datetime2 NOT NULL,
    [HorizonStart] datetime2 NOT NULL,
    [HorizonEnd] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ActivatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProductionSchedules] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [ProductCode] nvarchar(30) NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NULL,
    [Specification] nvarchar(200) NULL,
    [StandardPrice] decimal(14,2) NOT NULL,
    [SkuId] uniqueidentifier NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ProductSpecifications] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [TempSkuId] uniqueidentifier NULL,
    [ProductName] nvarchar(200) NULL,
    [ProductSeries] nvarchar(100) NULL,
    [TechnicalParams] nvarchar(max) NULL,
    [PackagingSpec] nvarchar(500) NULL,
    [CertificationRequirements] nvarchar(500) NULL,
    CONSTRAINT [PK_ProductSpecifications] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseRequisitions] (
    [Id] uniqueidentifier NOT NULL,
    [PrNumber] nvarchar(30) NOT NULL,
    [PrDate] datetime2 NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [RequesterId] uniqueidentifier NULL,
    [ProjectCode] nvarchar(50) NULL,
    [WorkOrderId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsLocked] bit NOT NULL,
    [TotalEstimatedAmount] decimal(14,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseRequisitions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Rfqs] (
    [Id] uniqueidentifier NOT NULL,
    [RfqNumber] nvarchar(30) NOT NULL,
    [PrId] uniqueidentifier NULL,
    [RfqDate] datetime2 NOT NULL,
    [Deadline] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Rfqs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RndLogs] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [ExperimentNumber] nvarchar(30) NULL,
    [ResearcherId] uniqueidentifier NOT NULL,
    [LogDate] datetime2 NOT NULL,
    [Location] nvarchar(100) NULL,
    [TestConditions] nvarchar(max) NULL,
    [Results] nvarchar(max) NULL,
    [Conclusion] nvarchar(max) NULL,
    [ConfidentialityLevel] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RndLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RndProjects] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectCode] nvarchar(30) NOT NULL,
    [ProjectName] nvarchar(200) NOT NULL,
    [ProjectManagerId] uniqueidentifier NULL,
    [ProjectType] nvarchar(30) NOT NULL,
    [CustomerId] uniqueidentifier NULL,
    [TargetPrice] decimal(14,2) NULL,
    [StartDate] datetime2 NULL,
    [TargetSampleDate] datetime2 NULL,
    [TargetProductionDate] datetime2 NULL,
    [EstimatedBudget] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [TechnicalFeasibility] nvarchar(200) NULL,
    [ComplianceStandards] nvarchar(500) NULL,
    [MarketResearchNo] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RndProjects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsSystem] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SalesForecasts] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetVersionId] uniqueidentifier NOT NULL,
    [Year] int NOT NULL,
    [Quarter] nvarchar(10) NULL,
    [Region] nvarchar(50) NULL,
    [SalesPersonId] uniqueidentifier NULL,
    [CustomerTier] nvarchar(30) NULL,
    [SalesTeamId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SalesForecasts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SalesInvoices] (
    [Id] uniqueidentifier NOT NULL,
    [InvoiceNumber] nvarchar(30) NOT NULL,
    [InvoiceDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [SalesOrderId] uniqueidentifier NULL,
    [DeliveryOrderId] uniqueidentifier NULL,
    [SalesAmount] decimal(14,2) NOT NULL,
    [TaxAmount] decimal(14,2) NOT NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [PaidAmount] decimal(14,2) NOT NULL,
    [TaxType] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [ApprovedAt] datetime2 NULL,
    [VoidedBy] uniqueidentifier NULL,
    [VoidedAt] datetime2 NULL,
    [VoidReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SalesInvoices] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SampleRequests] (
    [Id] uniqueidentifier NOT NULL,
    [RequestNumber] nvarchar(30) NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [Purpose] nvarchar(50) NOT NULL,
    [RequiredQty] int NOT NULL,
    [MaterialSource] nvarchar(50) NULL,
    [PilotRunYield] decimal(5,2) NULL,
    [DfmFeedback] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SampleRequests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ScrapRecords] (
    [Id] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [ScrapQty] decimal(14,2) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [DispositionCode] nvarchar(30) NULL,
    [CostAmount] decimal(14,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ScrapRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [StocktakeOrders] (
    [Id] uniqueidentifier NOT NULL,
    [StocktakeNumber] nvarchar(30) NOT NULL,
    [WarehouseId] uniqueidentifier NULL,
    [StocktakeType] nvarchar(30) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StocktakeOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SubcontractOrders] (
    [Id] uniqueidentifier NOT NULL,
    [SubcontractNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [OperationId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [IssuedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [ClosedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SubcontractOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SubstituteGroups] (
    [Id] uniqueidentifier NOT NULL,
    [GroupCode] nvarchar(50) NOT NULL,
    [GroupName] nvarchar(200) NULL,
    [Description] nvarchar(500) NULL,
    [SubstitutionType] nvarchar(20) NOT NULL,
    [AllowAutoSubstitute] bit NOT NULL,
    [IsBidirectional] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SubstituteGroups] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierAsns] (
    [Id] uniqueidentifier NOT NULL,
    [AsnNumber] nvarchar(30) NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [ExpectedArrivalDate] datetime2 NOT NULL,
    [CarrierName] nvarchar(100) NULL,
    [TrackingNumber] nvarchar(100) NULL,
    [Remarks] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierAsns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierDocuments] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [DocType] nvarchar(50) NOT NULL,
    [DocName] nvarchar(200) NULL,
    [FilePath] nvarchar(500) NULL,
    [ExpiryDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierDocuments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierEsgQuestionnaires] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [QuestionnaireNumber] nvarchar(30) NOT NULL,
    [QuestionnaireType] nvarchar(50) NOT NULL,
    [Year] int NOT NULL,
    [ResponsesJson] nvarchar(max) NULL,
    [EnvironmentalCertifications] nvarchar(200) NULL,
    [SocialInitiatives] nvarchar(200) NULL,
    [GovernancePractices] nvarchar(200) NULL,
    [TotalScore] decimal(5,2) NULL,
    [OverallRating] nvarchar(20) NULL,
    [Status] nvarchar(20) NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [ReviewedBy] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierEsgQuestionnaires] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierPerformanceScores] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [Period] nvarchar(7) NOT NULL,
    [OtifRate] decimal(5,2) NOT NULL,
    [Ppm] decimal(10,2) NOT NULL,
    [OrderAccuracyRate] decimal(5,2) NOT NULL,
    [ResponseTimeHours] decimal(8,2) NOT NULL,
    [OverallScore] decimal(5,2) NOT NULL,
    [Rating] nvarchar(5) NULL,
    [CalculatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierPerformanceScores] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierQuotes] (
    [Id] uniqueidentifier NOT NULL,
    [RfqId] uniqueidentifier NOT NULL,
    [SupplierUserId] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [QuotedPrice] decimal(14,4) NOT NULL,
    [LeadTimeDays] int NOT NULL,
    [Moq] decimal(14,2) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [SubmittedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierQuotes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Suppliers] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierCode] nvarchar(20) NOT NULL,
    [SupplierName] nvarchar(200) NOT NULL,
    [TaxId] nvarchar(10) NULL,
    [ContactPerson] nvarchar(50) NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(200) NULL,
    [Address] nvarchar(500) NULL,
    [InvoiceAddress] nvarchar(500) NULL,
    [DeliveryAddress] nvarchar(500) NULL,
    [PaymentTerms] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [Rating] nvarchar(10) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SystemConfigs] (
    [Id] uniqueidentifier NOT NULL,
    [ConfigKey] nvarchar(100) NOT NULL,
    [ConfigValue] nvarchar(max) NULL,
    [DataType] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemConfigs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SystemNotifications] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [TargetRole] nvarchar(50) NOT NULL,
    [Link] nvarchar(max) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemNotifications] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TransferOrders] (
    [Id] uniqueidentifier NOT NULL,
    [TransferNumber] nvarchar(30) NOT NULL,
    [SourceWarehouseId] uniqueidentifier NOT NULL,
    [TargetWarehouseId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TransferOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TreasuryTransfers] (
    [Id] uniqueidentifier NOT NULL,
    [TransferNumber] nvarchar(30) NOT NULL,
    [TransferDate] datetime2 NOT NULL,
    [Reason] nvarchar(200) NULL,
    [FromBankAccountId] uniqueidentifier NULL,
    [ToBankAccountId] uniqueidentifier NULL,
    [TransferAmount] decimal(18,2) NOT NULL,
    [Fee] decimal(10,2) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ExchangeRate] decimal(12,6) NOT NULL,
    [ExchangeGainLoss] decimal(14,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TreasuryTransfers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [UomGroups] (
    [Id] uniqueidentifier NOT NULL,
    [GroupCode] nvarchar(50) NOT NULL,
    [GroupName] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UomGroups] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Uoms] (
    [Id] uniqueidentifier NOT NULL,
    [UomCode] nvarchar(20) NOT NULL,
    [UomName] nvarchar(50) NOT NULL,
    [UomNameEn] nvarchar(50) NULL,
    [UomType] nvarchar(20) NOT NULL,
    [DecimalPlaces] int NOT NULL,
    [IsActive] bit NOT NULL,
    [AllowDecimal] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Uoms] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Email] nvarchar(200) NULL,
    [EmployeeId] uniqueidentifier NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [LastLoginIp] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Warehouses] (
    [Id] uniqueidentifier NOT NULL,
    [WarehouseCode] nvarchar(20) NOT NULL,
    [WarehouseName] nvarchar(100) NOT NULL,
    [Address] nvarchar(300) NULL,
    [Type] nvarchar(30) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [WorkCenters] (
    [Id] uniqueidentifier NOT NULL,
    [WorkCenterCode] nvarchar(20) NOT NULL,
    [WorkCenterName] nvarchar(100) NOT NULL,
    [Capacity] decimal(10,2) NOT NULL,
    [Efficiency] decimal(5,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_WorkCenters] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [WorkOrders] (
    [Id] uniqueidentifier NOT NULL,
    [WoNumber] nvarchar(30) NOT NULL,
    [WoType] nvarchar(30) NOT NULL,
    [ProductSkuId] uniqueidentifier NOT NULL,
    [BomId] uniqueidentifier NULL,
    [BomVersion] nvarchar(20) NULL,
    [SalesOrderId] uniqueidentifier NULL,
    [PlannedQty] decimal(14,2) NOT NULL,
    [ActualQty] decimal(14,2) NOT NULL,
    [ScrapQty] decimal(14,2) NOT NULL,
    [PlannedStartDate] datetime2 NULL,
    [PlannedEndDate] datetime2 NULL,
    [ActualStartDate] datetime2 NULL,
    [ActualEndDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AccountsPayableItems] (
    [Id] uniqueidentifier NOT NULL,
    [AccountsPayableId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [UnitPrice] decimal(14,4) NOT NULL,
    [Amount] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_AccountsPayableItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountsPayableItems_AccountsPayables_AccountsPayableId] FOREIGN KEY ([AccountsPayableId]) REFERENCES [AccountsPayables] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AgentTasks] (
    [Id] uniqueidentifier NOT NULL,
    [AgentExecutionId] uniqueidentifier NOT NULL,
    [TaskId] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ToolUsed] nvarchar(max) NULL,
    [Output] nvarchar(max) NULL,
    [Error] nvarchar(max) NULL,
    [Duration] decimal(18,4) NOT NULL,
    [Priority] int NOT NULL,
    [Dependencies] nvarchar(max) NULL,
    CONSTRAINT [PK_AgentTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AgentTasks_AgentExecutions_AgentExecutionId] FOREIGN KEY ([AgentExecutionId]) REFERENCES [AgentExecutions] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AnnouncementReadLogs] (
    [Id] uniqueidentifier NOT NULL,
    [AnnouncementId] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [ReadAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AnnouncementReadLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AnnouncementReadLogs_Announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [Announcements] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AsnItems] (
    [Id] uniqueidentifier NOT NULL,
    [AsnId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [ShippedQty] decimal(14,2) NOT NULL,
    [ManufactureDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_AsnItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AsnItems_Asns_AsnId] FOREIGN KEY ([AsnId]) REFERENCES [Asns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Assets] (
    [Id] uniqueidentifier NOT NULL,
    [AssetNumber] nvarchar(30) NOT NULL,
    [AssetName] nvarchar(200) NOT NULL,
    [Specification] nvarchar(500) NULL,
    [Manufacturer] nvarchar(100) NULL,
    [SupplierId] uniqueidentifier NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [LocationId] uniqueidentifier NULL,
    [CustodianDeptId] uniqueidentifier NULL,
    [CustodianEmployeeId] uniqueidentifier NULL,
    [AcquisitionCost] decimal(14,2) NOT NULL,
    [AcquisitionDate] datetime2 NULL,
    [CommissionDate] datetime2 NULL,
    [UsefulLife] int NOT NULL,
    [ResidualValue] decimal(14,2) NOT NULL,
    [DepreciationMethod] nvarchar(30) NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [InvoiceNumber] nvarchar(50) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Assets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assets_AssetCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [AssetCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AssetStocktakeItems] (
    [Id] uniqueidentifier NOT NULL,
    [AssetStocktakeId] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [SystemStatus] nvarchar(30) NULL,
    [ActualStatus] nvarchar(30) NULL,
    [LabelVerified] bit NOT NULL,
    [BarcodeScanned] bit NOT NULL,
    [CustodianConfirmed] bit NOT NULL,
    CONSTRAINT [PK_AssetStocktakeItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetStocktakeItems_AssetStocktakes_AssetStocktakeId] FOREIGN KEY ([AssetStocktakeId]) REFERENCES [AssetStocktakes] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BomItems] (
    [Id] uniqueidentifier NOT NULL,
    [BomId] uniqueidentifier NOT NULL,
    [ChildSkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,4) NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [ScrapRate] decimal(5,2) NOT NULL,
    [OperationSeq] int NOT NULL,
    [SubstituteGroupId] nvarchar(30) NULL,
    [IsPhantom] bit NOT NULL,
    [EffectiveDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_BomItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BomItems_Boms_BomId] FOREIGN KEY ([BomId]) REFERENCES [Boms] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BudgetVersions] (
    [Id] uniqueidentifier NOT NULL,
    [BudgetYearId] uniqueidentifier NOT NULL,
    [VersionNumber] nvarchar(20) NOT NULL,
    [VersionName] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_BudgetVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BudgetVersions_BudgetYears_BudgetYearId] FOREIGN KEY ([BudgetYearId]) REFERENCES [BudgetYears] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CashReceiptAllocations] (
    [Id] uniqueidentifier NOT NULL,
    [CashReceiptId] uniqueidentifier NOT NULL,
    [SalesInvoiceId] uniqueidentifier NOT NULL,
    [AllocatedAmount] decimal(14,2) NOT NULL,
    [ExchangeGainLoss] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_CashReceiptAllocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashReceiptAllocations_CashReceipts_CashReceiptId] FOREIGN KEY ([CashReceiptId]) REFERENCES [CashReceipts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CrossDeptTaskReplies] (
    [Id] uniqueidentifier NOT NULL,
    [TaskId] uniqueidentifier NOT NULL,
    [ReplierId] uniqueidentifier NOT NULL,
    [ReplyContent] nvarchar(max) NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CrossDeptTaskReplies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CrossDeptTaskReplies_CrossDeptTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [CrossDeptTasks] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CustomerContacts] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [ContactName] nvarchar(50) NOT NULL,
    [Title] nvarchar(50) NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(200) NULL,
    [IsPrimary] bit NOT NULL,
    CONSTRAINT [PK_CustomerContacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Quotes] (
    [Id] uniqueidentifier NOT NULL,
    [QuoteNumber] nvarchar(30) NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [QuoteDate] datetime2 NOT NULL,
    [ValidUntil] datetime2 NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [DiscountRate] decimal(5,4) NOT NULL,
    [FinalAmount] decimal(14,2) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [Status] nvarchar(50) NULL,
    [CustomerPoNumber] nvarchar(50) NULL,
    [CustomerDueDate] datetime2 NULL,
    [ConfirmedDeliveryDate] datetime2 NULL,
    [SalesPersonId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Quotes_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DocumentVersions] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentId] uniqueidentifier NOT NULL,
    [VersionNumber] nvarchar(20) NOT NULL,
    [FilePath] nvarchar(500) NULL,
    [FileSize] bigint NOT NULL,
    [Checksum] nvarchar(100) NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ChangeDescription] nvarchar(500) NULL,
    CONSTRAINT [PK_DocumentVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentVersions_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Ecns] (
    [Id] uniqueidentifier NOT NULL,
    [EcnNumber] nvarchar(30) NOT NULL,
    [EcrId] uniqueidentifier NOT NULL,
    [ChangeDescription] nvarchar(max) NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [PublishedBy] uniqueidentifier NULL,
    [PublishedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Ecns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ecns_Ecrs_EcrId] FOREIGN KEY ([EcrId]) REFERENCES [Ecrs] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ExpenseBudgetItems] (
    [Id] uniqueidentifier NOT NULL,
    [ExpenseBudgetId] uniqueidentifier NOT NULL,
    [AccountCode] nvarchar(20) NULL,
    [AccountName] nvarchar(100) NULL,
    [AnnualBudget] decimal(14,2) NOT NULL,
    [Month01] decimal(14,2) NOT NULL,
    [Month02] decimal(14,2) NOT NULL,
    [Month03] decimal(14,2) NOT NULL,
    [Month04] decimal(14,2) NOT NULL,
    [Month05] decimal(14,2) NOT NULL,
    [Month06] decimal(14,2) NOT NULL,
    [Month07] decimal(14,2) NOT NULL,
    [Month08] decimal(14,2) NOT NULL,
    [Month09] decimal(14,2) NOT NULL,
    [Month10] decimal(14,2) NOT NULL,
    [Month11] decimal(14,2) NOT NULL,
    [Month12] decimal(14,2) NOT NULL,
    [PriorYearActual] decimal(14,2) NOT NULL,
    [ControlLevel] nvarchar(20) NOT NULL,
    [ProjectCode] nvarchar(50) NULL,
    CONSTRAINT [PK_ExpenseBudgetItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpenseBudgetItems_ExpenseBudgets_ExpenseBudgetId] FOREIGN KEY ([ExpenseBudgetId]) REFERENCES [ExpenseBudgets] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [GoodsReceiptItems] (
    [Id] uniqueidentifier NOT NULL,
    [GoodsReceiptId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [PoQty] decimal(14,2) NOT NULL,
    [PoUnit] nvarchar(20) NULL,
    [ReceivedQty] decimal(14,2) NOT NULL,
    [AcceptedQty] decimal(14,2) NOT NULL,
    [RejectedQty] decimal(14,2) NOT NULL,
    [InventoryUnit] nvarchar(20) NULL,
    [ConversionRate] decimal(14,6) NOT NULL,
    [InventoryQty] decimal(14,2) NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [ManufactureDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [BinLocationId] uniqueidentifier NULL,
    [IqcResult] nvarchar(20) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ExchangeRate] decimal(12,6) NOT NULL,
    [ForeignCurrencyPrice] decimal(14,4) NOT NULL,
    [LocalCurrencyPrice] decimal(14,4) NOT NULL,
    [ForeignCurrencyAmount] decimal(14,2) NOT NULL,
    [LocalCurrencyAmount] decimal(14,2) NOT NULL,
    [TariffAmount] decimal(14,2) NOT NULL,
    [FreightAmount] decimal(14,2) NOT NULL,
    [TotalLandedCost] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_GoodsReceiptItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [GoodsReceipts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InvestmentValuations] (
    [Id] uniqueidentifier NOT NULL,
    [InvestmentId] uniqueidentifier NOT NULL,
    [ValuationDate] datetime2 NOT NULL,
    [MarketValue] decimal(18,2) NOT NULL,
    [UnrealizedGainLoss] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_InvestmentValuations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvestmentValuations_Investments_InvestmentId] FOREIGN KEY ([InvestmentId]) REFERENCES [Investments] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [IpqcReportItems] (
    [Id] uniqueidentifier NOT NULL,
    [IpqcReportId] uniqueidentifier NOT NULL,
    [KpcItem] nvarchar(100) NULL,
    [Specification] nvarchar(200) NULL,
    [UpperLimit] decimal(14,4) NULL,
    [LowerLimit] decimal(14,4) NULL,
    [MeasuredValue] nvarchar(100) NULL,
    [Result] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_IpqcReportItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IpqcReportItems_IpqcReports_IpqcReportId] FOREIGN KEY ([IpqcReportId]) REFERENCES [IpqcReports] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ApprovalAuthorities] (
    [Id] uniqueidentifier NOT NULL,
    [DocType] nvarchar(30) NOT NULL,
    [GradeId] uniqueidentifier NOT NULL,
    [MinAmount] decimal(14,2) NOT NULL,
    [MaxAmount] decimal(14,2) NOT NULL,
    [ApprovalLevel] int NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ApprovalAuthorities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalAuthorities_JobGrades_GradeId] FOREIGN KEY ([GradeId]) REFERENCES [JobGrades] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [JobTitles] (
    [Id] uniqueidentifier NOT NULL,
    [TitleCode] nvarchar(20) NOT NULL,
    [TitleName] nvarchar(50) NOT NULL,
    [GradeId] uniqueidentifier NULL,
    [ApplicableDeptType] nvarchar(20) NULL,
    [IsSupervisor] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_JobTitles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobTitles_JobGrades_GradeId] FOREIGN KEY ([GradeId]) REFERENCES [JobGrades] ([Id])
);
GO


CREATE TABLE [KpiValues] (
    [Id] uniqueidentifier NOT NULL,
    [KpiDefinitionId] uniqueidentifier NOT NULL,
    [PeriodStart] datetime2 NOT NULL,
    [PeriodEnd] datetime2 NOT NULL,
    [PeriodType] nvarchar(20) NOT NULL,
    [Value] decimal(14,4) NOT NULL,
    [TargetValue] decimal(14,4) NULL,
    [AchievementRate] decimal(5,2) NULL,
    [Status] nvarchar(20) NOT NULL,
    [RawData] nvarchar(max) NULL,
    [CalculatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_KpiValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KpiValues_KpiDefinitions_KpiDefinitionId] FOREIGN KEY ([KpiDefinitionId]) REFERENCES [KpiDefinitions] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LaborBudgetItems] (
    [Id] uniqueidentifier NOT NULL,
    [LaborBudgetId] uniqueidentifier NOT NULL,
    [PositionTitle] nvarchar(100) NULL,
    [JobGrade] nvarchar(20) NULL,
    [AdditionalHeadcount] int NOT NULL,
    [PlannedHireMonth] int NULL,
    [EstimatedStartingSalary] decimal(14,2) NOT NULL,
    [EstimatedOvertimeBudget] decimal(14,2) NOT NULL,
    [EstimatedInsuranceBudget] decimal(14,2) NOT NULL,
    [RecruitmentBudget] decimal(14,2) NOT NULL,
    [Justification] nvarchar(500) NULL,
    CONSTRAINT [PK_LaborBudgetItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LaborBudgetItems_LaborBudgets_LaborBudgetId] FOREIGN KEY ([LaborBudgetId]) REFERENCES [LaborBudgets] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LoanRepaymentSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [LoanId] uniqueidentifier NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PrincipalAmount] decimal(18,2) NOT NULL,
    [InterestAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_LoanRepaymentSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanRepaymentSchedules_Loans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [Loans] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MaterialCostForecastItems] (
    [Id] uniqueidentifier NOT NULL,
    [MaterialCostForecastId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [MainSupplierId] uniqueidentifier NULL,
    [ForecastDemandQty] decimal(14,2) NOT NULL,
    [CurrentPrice] decimal(14,4) NOT NULL,
    [ExpectedPriceChange] decimal(8,2) NOT NULL,
    [EstimatedUnitPrice] decimal(14,4) NOT NULL,
    [EstimatedTotalAmount] decimal(18,2) NOT NULL,
    [RiskNotes] nvarchar(500) NULL,
    CONSTRAINT [PK_MaterialCostForecastItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialCostForecastItems_MaterialCostForecasts_MaterialCostForecastId] FOREIGN KEY ([MaterialCostForecastId]) REFERENCES [MaterialCostForecasts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MaterialReturnItems] (
    [Id] uniqueidentifier NOT NULL,
    [ReturnId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [ReturnQty] decimal(14,2) NOT NULL,
    [Reason] nvarchar(500) NULL,
    CONSTRAINT [PK_MaterialReturnItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialReturnItems_MaterialReturns_ReturnId] FOREIGN KEY ([ReturnId]) REFERENCES [MaterialReturns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MaterialSupplementItems] (
    [Id] uniqueidentifier NOT NULL,
    [SupplementId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [BomStandardQty] decimal(14,2) NOT NULL,
    [ActualConsumedQty] decimal(14,2) NOT NULL,
    [SupplementQty] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_MaterialSupplementItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialSupplementItems_MaterialSupplements_SupplementId] FOREIGN KEY ([SupplementId]) REFERENCES [MaterialSupplements] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CalendarEvents] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [EventType] nvarchar(50) NOT NULL,
    [CategoryCode] nvarchar(50) NULL,
    [Location] nvarchar(200) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsAllDay] bit NOT NULL,
    [RecurrenceRule] nvarchar(max) NULL,
    [RecurrenceParentId] uniqueidentifier NULL,
    [Participants] nvarchar(max) NULL,
    [OrganizerId] uniqueidentifier NULL,
    [DepartmentId] uniqueidentifier NULL,
    [Visibility] nvarchar(20) NOT NULL,
    [SourceModule] nvarchar(30) NULL,
    [SourceId] uniqueidentifier NULL,
    [MeetingRoomId] uniqueidentifier NULL,
    [ProjectId] uniqueidentifier NULL,
    [Color] nvarchar(10) NULL,
    [IsPinned] bit NOT NULL,
    [Reminders] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [SyncToOutlook] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_CalendarEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CalendarEvents_CalendarCategories_CategoryCode] FOREIGN KEY ([CategoryCode]) REFERENCES [CalendarCategories] ([Code]),
    CONSTRAINT [FK_CalendarEvents_MeetingRooms_MeetingRoomId] FOREIGN KEY ([MeetingRoomId]) REFERENCES [MeetingRooms] ([Id])
);
GO


CREATE TABLE [MeetingReservations] (
    [Id] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [Subject] nvarchar(200) NULL,
    [ReserverId] uniqueidentifier NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    [Attendees] nvarchar(max) NULL,
    [CateringRequired] bit NOT NULL,
    [EquipmentRequired] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MeetingReservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MeetingReservations_MeetingRooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [MeetingRooms] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PaymentRecords] (
    [Id] uniqueidentifier NOT NULL,
    [PaymentBatchId] uniqueidentifier NOT NULL,
    [AccountsPayableId] uniqueidentifier NOT NULL,
    [PaidAmount] decimal(14,2) NOT NULL,
    [DiscountAmount] decimal(14,2) NOT NULL,
    [ExchangeGainLoss] decimal(14,2) NOT NULL,
    [BankReference] nvarchar(100) NULL,
    CONSTRAINT [PK_PaymentRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentRecords_PaymentBatches_PaymentBatchId] FOREIGN KEY ([PaymentBatchId]) REFERENCES [PaymentBatches] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PortalMessages] (
    [Id] uniqueidentifier NOT NULL,
    [ThreadId] uniqueidentifier NOT NULL,
    [SenderType] nvarchar(20) NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [Content] nvarchar(max) NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PortalMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PortalMessages_PortalMessageThreads_ThreadId] FOREIGN KEY ([ThreadId]) REFERENCES [PortalMessageThreads] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ScheduleConstraints] (
    [Id] uniqueidentifier NOT NULL,
    [ScheduleId] uniqueidentifier NOT NULL,
    [ConstraintType] nvarchar(30) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [ResourceId] uniqueidentifier NULL,
    [ResourceType] nvarchar(50) NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [Severity] nvarchar(20) NOT NULL,
    [IsResolved] bit NOT NULL,
    [ResolvedAt] datetime2 NULL,
    [ResolutionNotes] nvarchar(500) NULL,
    CONSTRAINT [PK_ScheduleConstraints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ScheduleConstraints_ProductionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [ProductionSchedules] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PurchaseRequisitionItems] (
    [Id] uniqueidentifier NOT NULL,
    [PurchaseRequisitionId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [Unit] nvarchar(20) NULL,
    [EstimatedUnitPrice] decimal(14,4) NOT NULL,
    [Currency] nvarchar(10) NOT NULL,
    [RequiredDate] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_PurchaseRequisitionItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseRequisitionItems_PurchaseRequisitions_PurchaseRequisitionId] FOREIGN KEY ([PurchaseRequisitionId]) REFERENCES [PurchaseRequisitions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RfqItems] (
    [Id] uniqueidentifier NOT NULL,
    [RfqId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [Unit] nvarchar(20) NULL,
    [Specifications] nvarchar(500) NULL,
    CONSTRAINT [PK_RfqItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RfqItems_Rfqs_RfqId] FOREIGN KEY ([RfqId]) REFERENCES [Rfqs] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RfqSupplierResponses] (
    [Id] uniqueidentifier NOT NULL,
    [RfqId] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [UnitPrice] decimal(14,4) NOT NULL,
    [LeadTimeDays] int NOT NULL,
    [Moq] decimal(14,2) NOT NULL,
    [TradeTerms] nvarchar(50) NULL,
    [Notes] nvarchar(500) NULL,
    [IsSelected] bit NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RfqSupplierResponses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RfqSupplierResponses_Rfqs_RfqId] FOREIGN KEY ([RfqId]) REFERENCES [Rfqs] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MassProductionTransitions] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [ProductionBomId] uniqueidentifier NOT NULL,
    [TransitionDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MassProductionTransitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MassProductionTransitions_Boms_ProductionBomId] FOREIGN KEY ([ProductionBomId]) REFERENCES [Boms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MassProductionTransitions_RndProjects_RndProjectId] FOREIGN KEY ([RndProjectId]) REFERENCES [RndProjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RndBudgets] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [BudgetCategory] nvarchar(50) NOT NULL,
    [BudgetAmount] decimal(14,2) NOT NULL,
    [ActualAmount] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_RndBudgets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RndBudgets_RndProjects_RndProjectId] FOREIGN KEY ([RndProjectId]) REFERENCES [RndProjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RndMilestones] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [MilestoneName] nvarchar(100) NOT NULL,
    [TargetDate] datetime2 NULL,
    [ActualDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_RndMilestones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RndMilestones_RndProjects_RndProjectId] FOREIGN KEY ([RndProjectId]) REFERENCES [RndProjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TrialProductions] (
    [Id] uniqueidentifier NOT NULL,
    [RndProjectId] uniqueidentifier NOT NULL,
    [TrialNumber] nvarchar(30) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [QuantityProduced] int NOT NULL,
    [GoodQuantity] int NOT NULL,
    [DefectiveQuantity] int NOT NULL,
    [YieldRate] decimal(5,2) NOT NULL,
    [DfmFeedback] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TrialProductions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrialProductions_RndProjects_RndProjectId] FOREIGN KEY ([RndProjectId]) REFERENCES [RndProjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [PermissionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SalesForecastItems] (
    [Id] uniqueidentifier NOT NULL,
    [SalesForecastId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [PriorYearQty] decimal(14,2) NOT NULL,
    [ForecastQty] decimal(14,2) NOT NULL,
    [GrowthRate] decimal(8,2) NOT NULL,
    [EstimatedUnitPrice] decimal(14,2) NOT NULL,
    [EstimatedAmount] decimal(18,2) NOT NULL,
    [Probability] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_SalesForecastItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesForecastItems_SalesForecasts_SalesForecastId] FOREIGN KEY ([SalesForecastId]) REFERENCES [SalesForecasts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesInvoiceItems] (
    [Id] uniqueidentifier NOT NULL,
    [SalesInvoiceId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [UnitPrice] decimal(14,2) NOT NULL,
    [Amount] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_SalesInvoiceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesInvoiceItems_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [SalesInvoices] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StocktakeOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [StocktakeOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [SystemQty] decimal(14,2) NOT NULL,
    [ActualQty] decimal(14,2) NOT NULL,
    [Difference] decimal(14,2) NOT NULL,
    [DifferenceAmount] decimal(14,2) NULL,
    [Reason] nvarchar(500) NULL,
    CONSTRAINT [PK_StocktakeOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StocktakeOrderItems_StocktakeOrders_StocktakeOrderId] FOREIGN KEY ([StocktakeOrderId]) REFERENCES [StocktakeOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SubcontractItems] (
    [Id] uniqueidentifier NOT NULL,
    [SubcontractOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [IssuedQty] decimal(14,2) NOT NULL,
    [ReceivedQty] decimal(14,2) NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    CONSTRAINT [PK_SubcontractItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubcontractItems_SubcontractOrders_SubcontractOrderId] FOREIGN KEY ([SubcontractOrderId]) REFERENCES [SubcontractOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SupplierAsnItems] (
    [Id] uniqueidentifier NOT NULL,
    [AsnId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NULL,
    [SkuNumber] nvarchar(50) NOT NULL,
    [SkuName] nvarchar(200) NOT NULL,
    [ShippedQty] int NOT NULL,
    [BatchNo] nvarchar(50) NULL,
    [ManufactureDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_SupplierAsnItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierAsnItems_SupplierAsns_AsnId] FOREIGN KEY ([AsnId]) REFERENCES [SupplierAsns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [IqcReports] (
    [Id] uniqueidentifier NOT NULL,
    [ReportNumber] nvarchar(30) NOT NULL,
    [GoodsReceiptId] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [InspectionDate] datetime2 NOT NULL,
    [InspectorId] uniqueidentifier NOT NULL,
    [SamplingLevel] nvarchar(30) NULL,
    [Aql] decimal(5,2) NULL,
    [SampleSize] int NOT NULL,
    [AcceptNumber] int NOT NULL,
    [RejectNumber] int NOT NULL,
    [Result] nvarchar(20) NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    CONSTRAINT [PK_IqcReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IqcReports_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] uniqueidentifier NOT NULL,
    [PoNumber] nvarchar(30) NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [RfqId] uniqueidentifier NULL,
    [PrId] uniqueidentifier NULL,
    [PoDate] datetime2 NOT NULL,
    [PaymentTerms] nvarchar(50) NULL,
    [TradeTerms] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ExchangeRate] decimal(12,6) NOT NULL,
    [TaxType] nvarchar(20) NOT NULL,
    [TaxCode] nvarchar(20) NULL,
    [Subtotal] decimal(14,2) NOT NULL,
    [TaxAmount] decimal(14,2) NOT NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsLocked] bit NOT NULL,
    [Version] int NOT NULL,
    [BlanketOrderId] uniqueidentifier NULL,
    [CancellationReason] nvarchar(500) NULL,
    [TermsAndConditions] nvarchar(2000) NULL,
    [SpecialPackaging] nvarchar(500) NULL,
    [DeliveryWarehouseId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupplierBankAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [BankCode] nvarchar(10) NULL,
    [BankBranch] nvarchar(20) NULL,
    [AccountNumber] nvarchar(30) NULL,
    [AccountName] nvarchar(100) NULL,
    [IsPrimary] bit NOT NULL,
    CONSTRAINT [PK_SupplierBankAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierBankAccounts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupplierComplaints] (
    [Id] uniqueidentifier NOT NULL,
    [ComplaintNumber] nvarchar(30) NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [PurchaseOrderId] uniqueidentifier NULL,
    [SkuId] uniqueidentifier NULL,
    [LotNumber] nvarchar(50) NULL,
    [DefectQty] decimal(14,2) NOT NULL,
    [Ppm] decimal(10,2) NOT NULL,
    [DefectType] nvarchar(50) NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierComplaints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierComplaints_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupplierContacts] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [ContactName] nvarchar(50) NOT NULL,
    [Title] nvarchar(50) NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(200) NULL,
    [IsPrimary] bit NOT NULL,
    CONSTRAINT [PK_SupplierContacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierContacts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupplierUsers] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [ContactName] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [Role] nvarchar(30) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierUsers_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UomGroupMembers] (
    [Id] uniqueidentifier NOT NULL,
    [UomGroupId] uniqueidentifier NOT NULL,
    [UomId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UomGroupMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UomGroupMembers_UomGroups_UomGroupId] FOREIGN KEY ([UomGroupId]) REFERENCES [UomGroups] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UomGroupMembers_Uoms_UomId] FOREIGN KEY ([UomId]) REFERENCES [Uoms] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityType] nvarchar(200) NULL,
    [EntityId] uniqueidentifier NULL,
    [Details] nvarchar(500) NULL,
    [UserId] uniqueidentifier NULL,
    [IpAddress] nvarchar(50) NULL,
    [TableName] nvarchar(100) NULL,
    [UserAgent] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [UserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UserThemePreferences] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ThemeName] nvarchar(50) NOT NULL,
    [CustomPrimaryColor] nvarchar(10) NULL,
    [CustomAccentColor] nvarchar(10) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserThemePreferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserThemePreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BinLocations] (
    [Id] uniqueidentifier NOT NULL,
    [WarehouseId] uniqueidentifier NOT NULL,
    [LocationCode] nvarchar(30) NOT NULL,
    [Zone] nvarchar(20) NULL,
    [Aisle] nvarchar(20) NULL,
    [Rack] nvarchar(20) NULL,
    [Level] nvarchar(20) NULL,
    [Position] nvarchar(20) NULL,
    [MaxWeight] decimal(10,2) NULL,
    [GridX] nvarchar(10) NULL,
    [GridY] nvarchar(10) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BinLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BinLocations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CapacityCalendars] (
    [Id] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [CalendarName] nvarchar(50) NOT NULL,
    [CalendarType] nvarchar(20) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CapacityCalendars] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CapacityCalendars_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CapacityLoads] (
    [Id] uniqueidentifier NOT NULL,
    [ScheduleId] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [LoadDate] datetime2 NOT NULL,
    [ShiftNumber] int NOT NULL,
    [AvailableHours] decimal(8,2) NOT NULL,
    [ScheduledHours] decimal(8,2) NOT NULL,
    [LoadedHours] decimal(8,2) NOT NULL,
    [LoadPercentage] decimal(5,2) NOT NULL,
    [LoadStatus] nvarchar(20) NOT NULL,
    [CapacityUtilization] decimal(14,2) NULL,
    CONSTRAINT [PK_CapacityLoads] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CapacityLoads_ProductionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [ProductionSchedules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CapacityLoads_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Equipments] (
    [Id] uniqueidentifier NOT NULL,
    [EquipmentCode] nvarchar(30) NOT NULL,
    [EquipmentName] nvarchar(100) NOT NULL,
    [EquipmentType] nvarchar(50) NULL,
    [WorkCenterId] uniqueidentifier NULL,
    [SerialNumber] nvarchar(50) NULL,
    [Model] nvarchar(50) NULL,
    [Manufacturer] nvarchar(50) NULL,
    [SupplierId] uniqueidentifier NULL,
    [PurchaseDate] datetime2 NULL,
    [PurchasePrice] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [MaintenanceStatus] nvarchar(20) NOT NULL,
    [LastMaintenanceDate] datetime2 NULL,
    [NextMaintenanceDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Equipments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Equipments_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Equipments_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [MachineOperationRecords] (
    [Id] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NULL,
    [RecordDate] datetime2 NOT NULL,
    [ShiftNumber] int NOT NULL,
    [PlannedRunTime] decimal(10,2) NOT NULL,
    [ActualRunTime] decimal(10,2) NOT NULL,
    [Downtime] decimal(10,2) NOT NULL,
    [SetupTime] decimal(10,2) NOT NULL,
    [WaitTime] decimal(10,2) NOT NULL,
    [TheoreticalOutput] decimal(14,2) NOT NULL,
    [ActualOutput] decimal(14,2) NOT NULL,
    [GoodOutput] decimal(14,2) NOT NULL,
    [DefectOutput] decimal(14,2) NOT NULL,
    [DowntimeReasonCode] nvarchar(20) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MachineOperationRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MachineOperationRecords_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MachineOperationRecords_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id])
);
GO


CREATE TABLE [MaterialIssues] (
    [Id] uniqueidentifier NOT NULL,
    [IssueNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [IssueWarehouseId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    [IssuedBy] uniqueidentifier NULL,
    [IssuedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaterialIssues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialIssues_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ScheduleTasks] (
    [Id] uniqueidentifier NOT NULL,
    [ScheduleId] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [OperationSeq] int NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [PlannedStart] datetime2 NOT NULL,
    [PlannedEnd] datetime2 NOT NULL,
    [SetupHours] decimal(8,2) NOT NULL,
    [RunHours] decimal(8,2) NOT NULL,
    [TotalHours] decimal(8,2) NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Priority] int NOT NULL,
    [SequenceType] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_ScheduleTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ScheduleTasks_ProductionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [ProductionSchedules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ScheduleTasks_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ScheduleTasks_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WipReports] (
    [Id] uniqueidentifier NOT NULL,
    [ReportNumber] nvarchar(30) NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [OperationSeq] int NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [ShiftClass] nvarchar(20) NULL,
    [CompletedQty] decimal(14,2) NOT NULL,
    [DefectQty] decimal(14,2) NOT NULL,
    [ScrapQty] decimal(14,2) NOT NULL,
    [ActualManHours] decimal(8,2) NOT NULL,
    [ActualMachineHours] decimal(8,2) NOT NULL,
    [StartTime] datetime2 NULL,
    [EndTime] datetime2 NULL,
    [MaterialLotNumber] nvarchar(50) NULL,
    [EnergyConsumption] decimal(14,2) NULL,
    [HasAnomaly] bit NOT NULL,
    [AnomalyType] nvarchar(50) NULL,
    [AiDiagnosis] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WipReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WipReports_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkOrderMaterials] (
    [Id] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [BomStandardQty] decimal(14,4) NOT NULL,
    [RequiredQty] decimal(14,2) NOT NULL,
    [IssuedQty] decimal(14,2) NOT NULL,
    [ReturnedQty] decimal(14,2) NOT NULL,
    [ShortageQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_WorkOrderMaterials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkOrderMaterials_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkOrderOperations] (
    [Id] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [OperationSeq] int NOT NULL,
    [OperationId] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [PlannedQty] decimal(14,2) NOT NULL,
    [CompletedQty] decimal(14,2) NOT NULL,
    [ScrapQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_WorkOrderOperations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkOrderOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Operations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WorkOrderOperations_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WorkOrderOperations_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AssetDepreciations] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [Period] nvarchar(7) NOT NULL,
    [DepreciationAmount] decimal(14,2) NOT NULL,
    [AccumulatedDepreciation] decimal(14,2) NOT NULL,
    [BookValue] decimal(14,2) NOT NULL,
    [JournalEntryId] uniqueidentifier NULL,
    CONSTRAINT [PK_AssetDepreciations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetDepreciations_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AssetMaintenances] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [MaintenanceType] nvarchar(50) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [PartsCost] decimal(14,2) NOT NULL,
    [LaborCost] decimal(14,2) NOT NULL,
    [ExternalCost] decimal(14,2) NOT NULL,
    [IsCapitalized] bit NOT NULL,
    [NextMaintenanceDate] datetime2 NULL,
    [CompletedDate] datetime2 NULL,
    [SerialNumber] nvarchar(50) NULL,
    CONSTRAINT [PK_AssetMaintenances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetMaintenances_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [EcnAffectedBoms] (
    [Id] uniqueidentifier NOT NULL,
    [EcnId] uniqueidentifier NOT NULL,
    [BomId] uniqueidentifier NOT NULL,
    [ActionType] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_EcnAffectedBoms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EcnAffectedBoms_Ecns_EcnId] FOREIGN KEY ([EcnId]) REFERENCES [Ecns] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Employees] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeNo] nvarchar(20) NOT NULL,
    [ChineseName] nvarchar(50) NOT NULL,
    [EnglishName] nvarchar(100) NULL,
    [NationalId] nvarchar(20) NULL,
    [PassportNo] nvarchar(30) NULL,
    [ResidencePermitNo] nvarchar(30) NULL,
    [ResidencePermitExpiry] datetime2 NULL,
    [BirthDate] datetime2 NULL,
    [Gender] nvarchar(10) NULL,
    [Phone] nvarchar(30) NULL,
    [Mobile] nvarchar(30) NULL,
    [Email] nvarchar(200) NULL,
    [RegisteredAddress] nvarchar(300) NULL,
    [MailingAddress] nvarchar(300) NULL,
    [EmergencyContactName] nvarchar(50) NULL,
    [EmergencyContactPhone] nvarchar(30) NULL,
    [EmergencyContactRelation] nvarchar(50) NULL,
    [DepartmentId] uniqueidentifier NULL,
    [JobTitleId] uniqueidentifier NULL,
    [GradeId] uniqueidentifier NULL,
    [PositionTitle] nvarchar(100) NULL,
    [JobGrade] nvarchar(20) NULL,
    [JobGradeCode] nvarchar(20) NULL,
    [JobCategory] nvarchar(50) NULL,
    [SupervisorId] uniqueidentifier NULL,
    [HireDate] datetime2 NULL,
    [ProbationEndDate] datetime2 NULL,
    [ResignationDate] datetime2 NULL,
    [ResignationReason] nvarchar(200) NULL,
    [Status] nvarchar(20) NOT NULL,
    [SalaryType] nvarchar(20) NULL,
    [BankCode] nvarchar(10) NULL,
    [BankBranch] nvarchar(20) NULL,
    [BankAccount] nvarchar(30) NULL,
    [BaseSalary] decimal(12,2) NOT NULL,
    [FoodAllowance] decimal(10,2) NOT NULL,
    [PositionAllowance] decimal(10,2) NOT NULL,
    [FullAttendanceBonus] decimal(10,2) NOT NULL,
    [LaborInsuranceGrade] int NOT NULL,
    [HealthInsuranceGrade] int NOT NULL,
    [PensionContributionRate] decimal(5,2) NOT NULL,
    [TaxDependents] int NOT NULL,
    [DisabilityCategory] nvarchar(50) NULL,
    [CardNumber] nvarchar(50) NULL,
    [BiometricId] nvarchar(50) NULL,
    [ShiftType] nvarchar(20) NULL,
    [OvertimePermission] bit NOT NULL,
    [HighestEducation] nvarchar(100) NULL,
    [Major] nvarchar(100) NULL,
    [GraduationYear] int NULL,
    [ForeignLanguages] nvarchar(200) NULL,
    [Skills] nvarchar(500) NULL,
    [IdCardScanUrl] nvarchar(500) NULL,
    [DiplomaScanUrl] nvarchar(500) NULL,
    [InsuranceTransferUrl] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_Employees_Employees_SupervisorId] FOREIGN KEY ([SupervisorId]) REFERENCES [Employees] ([Id]),
    CONSTRAINT [FK_Employees_JobGrades_GradeId] FOREIGN KEY ([GradeId]) REFERENCES [JobGrades] ([Id]),
    CONSTRAINT [FK_Employees_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [JobTitles] ([Id])
);
GO


CREATE TABLE [CalendarReminderLogs] (
    [Id] uniqueidentifier NOT NULL,
    [CalendarEventId] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [Channel] nvarchar(20) NOT NULL,
    [MinutesBefore] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ScheduledAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    [ErrorMessage] nvarchar(500) NULL,
    CONSTRAINT [PK_CalendarReminderLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CalendarReminderLogs_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [IqcReportItems] (
    [Id] uniqueidentifier NOT NULL,
    [IqcReportId] uniqueidentifier NOT NULL,
    [InspectionItem] nvarchar(100) NULL,
    [Specification] nvarchar(200) NULL,
    [MeasuredValue] nvarchar(100) NULL,
    [Result] nvarchar(20) NOT NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    CONSTRAINT [PK_IqcReportItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IqcReportItems_IqcReports_IqcReportId] FOREIGN KEY ([IqcReportId]) REFERENCES [IqcReports] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [PurchaseOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [Unit] nvarchar(20) NULL,
    [UnitPrice] decimal(14,4) NOT NULL,
    [Subtotal] decimal(14,2) NOT NULL,
    [ReceivedQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [InventoryUnit] nvarchar(20) NULL,
    [ConversionRate] decimal(14,6) NOT NULL,
    [InventoryQty] decimal(14,2) NOT NULL,
    [InventoryUnitPrice] decimal(14,4) NOT NULL,
    [InventorySubtotal] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SupplierCapas] (
    [Id] uniqueidentifier NOT NULL,
    [CapaNumber] nvarchar(30) NOT NULL,
    [ComplaintId] uniqueidentifier NOT NULL,
    [D1_Team] nvarchar(500) NULL,
    [D2_ProblemDescription] nvarchar(max) NULL,
    [D3_InterimContainment] nvarchar(max) NULL,
    [D4_RootCause] nvarchar(max) NULL,
    [D5_PermanentAction] nvarchar(max) NULL,
    [D6_Verification] nvarchar(max) NULL,
    [D7_Prevention] nvarchar(max) NULL,
    [D8_Closure] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [SupplierResponseDue] datetime2 NULL,
    [SupplierRespondedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SupplierCapas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierCapas_SupplierComplaints_ComplaintId] FOREIGN KEY ([ComplaintId]) REFERENCES [SupplierComplaints] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Skus] (
    [Id] uniqueidentifier NOT NULL,
    [SkuNumber] nvarchar(50) NOT NULL,
    [SkuName] nvarchar(200) NOT NULL,
    [Specification] nvarchar(500) NULL,
    [Barcode] nvarchar(50) NULL,
    [PurchaseUnit] nvarchar(20) NULL,
    [PurchaseUomCode] nvarchar(20) NULL,
    [Category] nvarchar(30) NULL,
    [ConversionRate] decimal(10,4) NOT NULL,
    [LotControlled] bit NOT NULL,
    [SerialControlled] bit NOT NULL,
    [Length] decimal(10,2) NULL,
    [Width] decimal(10,2) NULL,
    [Height] decimal(10,2) NULL,
    [Weight] decimal(10,2) NULL,
    [DefaultWarehouseId] uniqueidentifier NULL,
    [DefaultBinLocationId] uniqueidentifier NULL,
    [DefaultSupplierId] uniqueidentifier NULL,
    [Unit] nvarchar(10) NULL,
    [AbcClassification] nvarchar(10) NULL,
    [SourceType] nvarchar(20) NOT NULL,
    [SafetyStock] decimal(12,2) NOT NULL,
    [ReorderPoint] decimal(12,2) NOT NULL,
    [MaxStock] decimal(12,2) NOT NULL,
    [LeadTimeDays] int NOT NULL,
    [Moq] decimal(14,4) NOT NULL,
    [PurchasePrice] decimal(14,4) NOT NULL,
    [StandardCost] decimal(14,4) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [LowLevelCode] int NOT NULL,
    [Qty] decimal(14,2) NOT NULL,
    [UnitsPerCarton] int NULL,
    [CartonsPerPallet] int NULL,
    [HumidityRequirement] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [BaseUomCode] nvarchar(20) NULL,
    [SalesUomCode] nvarchar(20) NULL,
    [ProductionUomCode] nvarchar(20) NULL,
    [PurchaseToBaseFactor] decimal(18,6) NULL,
    [SalesToBaseFactor] decimal(18,6) NULL,
    [ProductionToBaseFactor] decimal(18,6) NULL,
    [BaseUomId] uniqueidentifier NULL,
    [PurchaseUomId] uniqueidentifier NULL,
    [SalesUomId] uniqueidentifier NULL,
    [ProductionUomId] uniqueidentifier NULL,
    [ItemType] nvarchar(20) NOT NULL,
    [Model] nvarchar(50) NULL,
    [Brand] nvarchar(100) NULL,
    [IsSellable] bit NOT NULL,
    [IsPurchasable] bit NOT NULL,
    [IsManufacturable] bit NOT NULL,
    [IsOutsourced] bit NOT NULL,
    [CostMethod] nvarchar(20) NOT NULL,
    [SuggestedPrice] decimal(14,4) NOT NULL,
    [BasePrice] decimal(14,4) NOT NULL,
    [PriceA] decimal(14,4) NULL,
    [PriceB] decimal(14,4) NULL,
    [PriceC] decimal(14,4) NULL,
    [PriceD] decimal(14,4) NULL,
    [PriceE] decimal(14,4) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [IsBomTop] bit NOT NULL,
    [IsBomComponent] bit NOT NULL,
    [IsPhantom] bit NOT NULL,
    [IsMto] bit NOT NULL,
    [IsMts] bit NOT NULL,
    [IsCustom] bit NOT NULL,
    [DefaultBomId] uniqueidentifier NULL,
    [DefaultRoutingId] uniqueidentifier NULL,
    [MakeOrBuy] nvarchar(20) NOT NULL,
    [SupplierItemCode] nvarchar(50) NULL,
    [SupplierLeadTime] int NULL,
    [DrawingNo] nvarchar(50) NULL,
    [DrawingRevision] nvarchar(20) NULL,
    [EffectiveFrom] datetime2 NULL,
    [EffectiveTo] datetime2 NULL,
    [QualitySpec] nvarchar(500) NULL,
    [ProductionDept] nvarchar(20) NULL,
    [MachineCode] nvarchar(50) NULL,
    [MoldCode] nvarchar(50) NULL,
    [CostUnit] nvarchar(20) NULL,
    [CostUomId] uniqueidentifier NULL,
    [LatestPurchaseCost] decimal(14,4) NOT NULL,
    [TaxCode] nvarchar(20) NULL,
    [TaxRate] nvarchar(10) NULL,
    [IsSubstitute] bit NOT NULL,
    [IsAlternate] bit NOT NULL,
    [IqcRequired] bit NOT NULL,
    [OqcRequired] bit NOT NULL,
    [Color] nvarchar(30) NULL,
    [PackingMethod] nvarchar(50) NULL,
    [MinPackingQty] int NULL,
    [IsStocked] bit NOT NULL,
    [MaterialType] nvarchar(20) NULL,
    [DesignOwner] nvarchar(50) NULL,
    [ShelfLifeDays] int NULL,
    [AvgDailyConsumption] decimal(12,2) NOT NULL,
    [PurchasePriceCeiling] decimal(5,2) NULL,
    [FloorPrice] decimal(14,4) NULL,
    [DiscountRate] decimal(5,2) NULL,
    [CommissionAccount] nvarchar(50) NULL,
    [RelatedSkuId] nvarchar(50) NULL,
    [CustomerItemCode] nvarchar(50) NULL,
    [BomType] nvarchar(20) NULL,
    [RoutingId] nvarchar(50) NULL,
    [SubstituteSkuId] nvarchar(50) NULL,
    [OutsourceSupplierId] uniqueidentifier NULL,
    [ProcessingFeeAccount] nvarchar(50) NULL,
    [OutsourceWarehouseId] uniqueidentifier NULL,
    [IsSupplierProvided] bit NOT NULL,
    CONSTRAINT [PK_Skus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Skus_BinLocations_DefaultBinLocationId] FOREIGN KEY ([DefaultBinLocationId]) REFERENCES [BinLocations] ([Id]),
    CONSTRAINT [FK_Skus_Suppliers_DefaultSupplierId] FOREIGN KEY ([DefaultSupplierId]) REFERENCES [Suppliers] ([Id]),
    CONSTRAINT [FK_Skus_Uoms_BaseUomId] FOREIGN KEY ([BaseUomId]) REFERENCES [Uoms] ([Id]),
    CONSTRAINT [FK_Skus_Uoms_CostUomId] FOREIGN KEY ([CostUomId]) REFERENCES [Uoms] ([Id]),
    CONSTRAINT [FK_Skus_Uoms_ProductionUomId] FOREIGN KEY ([ProductionUomId]) REFERENCES [Uoms] ([Id]),
    CONSTRAINT [FK_Skus_Uoms_PurchaseUomId] FOREIGN KEY ([PurchaseUomId]) REFERENCES [Uoms] ([Id]),
    CONSTRAINT [FK_Skus_Uoms_SalesUomId] FOREIGN KEY ([SalesUomId]) REFERENCES [Uoms] ([Id]),
    CONSTRAINT [FK_Skus_Warehouses_DefaultWarehouseId] FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [CapacityCalendarEntries] (
    [Id] uniqueidentifier NOT NULL,
    [CalendarId] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [ShiftNumber] int NOT NULL,
    [ShiftCode] nvarchar(10) NOT NULL,
    [AvailableHours] decimal(8,2) NOT NULL,
    [Efficiency] decimal(5,2) NOT NULL,
    [IsHoliday] bit NOT NULL,
    [Reason] nvarchar(200) NULL,
    CONSTRAINT [PK_CapacityCalendarEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CapacityCalendarEntries_CapacityCalendars_CalendarId] FOREIGN KEY ([CalendarId]) REFERENCES [CapacityCalendars] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [EquipmentDowntimes] (
    [Id] uniqueidentifier NOT NULL,
    [DowntimeNumber] nvarchar(30) NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [DowntimeType] nvarchar(50) NOT NULL,
    [Cause] nvarchar(50) NULL,
    [Description] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ReportedBy] uniqueidentifier NULL,
    [AssignedTo] uniqueidentifier NULL,
    [Resolution] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EquipmentDowntimes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EquipmentDowntimes_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MaterialIssueItems] (
    [Id] uniqueidentifier NOT NULL,
    [IssueId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [RequiredQty] decimal(14,2) NOT NULL,
    [IssuedQty] decimal(14,2) NOT NULL,
    [ShortageQty] decimal(14,2) NOT NULL,
    [BinLocationId] uniqueidentifier NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_MaterialIssueItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialIssueItems_MaterialIssues_IssueId] FOREIGN KEY ([IssueId]) REFERENCES [MaterialIssues] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AttendanceRecords] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [ProductionLine] nvarchar(50) NULL,
    [Date] datetime2 NOT NULL,
    [ShiftType] nvarchar(20) NULL,
    [ScheduledStartTime] time NULL,
    [ScheduledEndTime] time NULL,
    [ClockIn] datetime2 NULL,
    [ClockOut] datetime2 NULL,
    [ClockInLocation] nvarchar(200) NULL,
    [ClockOutLocation] nvarchar(200) NULL,
    [Status] nvarchar(20) NOT NULL,
    [LateFlag] bit NOT NULL,
    [LateMinutes] int NOT NULL,
    [EarlyLeaveFlag] bit NOT NULL,
    [EarlyLeaveMinutes] int NOT NULL,
    [ClockingAnomalyFlag] bit NOT NULL,
    [AnomalyType] nvarchar(50) NULL,
    [Ot1Hours] decimal(5,2) NOT NULL,
    [Ot2Hours] decimal(5,2) NOT NULL,
    [Ot3Hours] decimal(5,2) NOT NULL,
    [OtReason] nvarchar(500) NULL,
    [ApprovalStatus] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRecords_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_AttendanceRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CostCenters] (
    [Id] uniqueidentifier NOT NULL,
    [CostCenterCode] nvarchar(20) NOT NULL,
    [CostCenterName] nvarchar(100) NOT NULL,
    [CostCenterType] nvarchar(20) NOT NULL,
    [ParentCostCenterId] uniqueidentifier NULL,
    [DepartmentId] uniqueidentifier NULL,
    [WorkCenterId] uniqueidentifier NULL,
    [ManagerEmployeeId] uniqueidentifier NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [BudgetLimit] decimal(14,2) NOT NULL,
    [BudgetUsed] decimal(14,2) NOT NULL,
    [Description] nvarchar(200) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CostCenters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CostCenters_CostCenters_ParentCostCenterId] FOREIGN KEY ([ParentCostCenterId]) REFERENCES [CostCenters] ([Id]),
    CONSTRAINT [FK_CostCenters_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CostCenters_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CostCenters_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [EmployeeCertifications] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [CertificationName] nvarchar(200) NOT NULL,
    [CertifyingAuthority] nvarchar(100) NULL,
    [IssueDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [CertNumber] nvarchar(200) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EmployeeCertifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeCertifications_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [EmploymentHistories] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [ChangeType] nvarchar(50) NOT NULL,
    [OldDeptId] uniqueidentifier NULL,
    [NewDeptId] uniqueidentifier NULL,
    [OldPosition] nvarchar(100) NULL,
    [NewPosition] nvarchar(100) NULL,
    [OldSalary] decimal(12,2) NULL,
    [NewSalary] decimal(12,2) NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [Reason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EmploymentHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmploymentHistories_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveBalances] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [LeaveType] nvarchar(30) NOT NULL,
    [Year] int NOT NULL,
    [TotalEntitled] decimal(8,2) NOT NULL,
    [Used] decimal(8,2) NOT NULL,
    [Remaining] decimal(8,2) NOT NULL,
    CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveRequests] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [ApplicationNo] nvarchar(30) NOT NULL,
    [LeaveType] nvarchar(30) NOT NULL,
    [ApplicationDate] datetime2 NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [StartTime] time NULL,
    [EndTime] time NULL,
    [TotalHours] decimal(8,2) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    [IncludesSpecialPeriod] bit NOT NULL,
    [ShiftConflictFlag] bit NOT NULL,
    [AutoDeductPayFlag] bit NOT NULL,
    [ImpactOtOrBonus] bit NOT NULL,
    [ApprovalLevel] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ApproverId] uniqueidentifier NULL,
    [ApprovedAt] datetime2 NULL,
    [RejectReason] nvarchar(500) NULL,
    [Remarks] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [OvertimeRequests] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [TotalHours] decimal(5,2) NOT NULL,
    [OvertimeType] nvarchar(30) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ApproverId] uniqueidentifier NULL,
    [ApprovedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OvertimeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OvertimeRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PayrollAnomalies] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [PayrollId] uniqueidentifier NULL,
    [AnomalyCategory] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    [ExpectedResolution] nvarchar(30) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ResolvedAt] datetime2 NULL,
    CONSTRAINT [PK_PayrollAnomalies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollAnomalies_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Payrolls] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [PeriodStart] datetime2 NOT NULL,
    [PeriodEnd] datetime2 NOT NULL,
    [BasicSalary] decimal(14,2) NOT NULL,
    [InsuranceBase] decimal(14,2) NOT NULL,
    [PensionBase] decimal(14,2) NOT NULL,
    [NewLeavingFlag] bit NOT NULL,
    [ScheduledDays] int NOT NULL,
    [ActualWorkingDays] int NOT NULL,
    [TotalLateMinutes] int NOT NULL,
    [TotalEarlyMinutes] int NOT NULL,
    [AnomalyDays] int NOT NULL,
    [TotalOt1Hours] decimal(5,2) NOT NULL,
    [TotalOt2Hours] decimal(5,2) NOT NULL,
    [TotalOt3Hours] decimal(5,2) NOT NULL,
    [Ot1Pay] decimal(14,2) NOT NULL,
    [Ot2Pay] decimal(14,2) NOT NULL,
    [Ot3Pay] decimal(14,2) NOT NULL,
    [TotalLeaveHours] decimal(8,2) NOT NULL,
    [AnnualLeaveHours] decimal(8,2) NOT NULL,
    [PersonalLeaveHours] decimal(8,2) NOT NULL,
    [SickLeaveHours] decimal(8,2) NOT NULL,
    [FullAttendanceFlag] bit NOT NULL,
    [LateDeduction] decimal(14,2) NOT NULL,
    [EarlyDeduction] decimal(14,2) NOT NULL,
    [AnomalyPenalty] decimal(14,2) NOT NULL,
    [PersonalLeaveDeduction] decimal(14,2) NOT NULL,
    [SickLeaveDeduction] decimal(14,2) NOT NULL,
    [OtherDeductions] decimal(14,2) NOT NULL,
    [OvertimePay] decimal(14,2) NOT NULL,
    [Bonus] decimal(14,2) NOT NULL,
    [AttendanceBonus] decimal(14,2) NOT NULL,
    [LaborInsuranceDeduction] decimal(14,2) NOT NULL,
    [HealthInsuranceDeduction] decimal(14,2) NOT NULL,
    [PensionDeduction] decimal(14,2) NOT NULL,
    [WithholdingTax] decimal(14,2) NOT NULL,
    [SupplementalHealthInsurance] decimal(14,2) NULL,
    [EmployerLi] decimal(14,2) NOT NULL,
    [EmployerHi] decimal(14,2) NOT NULL,
    [EmployerPf] decimal(14,2) NOT NULL,
    [EmploymentInsurance] decimal(14,2) NOT NULL,
    [GrossPay] decimal(14,2) NOT NULL,
    [TotalDeductions] decimal(14,2) NOT NULL,
    [EmployerContributions] decimal(14,2) NOT NULL,
    [NetPay] decimal(14,2) NOT NULL,
    [CalculatedBy] nvarchar(50) NULL,
    [CalculatedAt] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [PaymentDate] datetime2 NULL,
    [PaymentBankAccount] nvarchar(30) NULL,
    [PayrollSlipNo] nvarchar(30) NULL,
    [CostAllocation] nvarchar(500) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payrolls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payrolls_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PerformanceReviews] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [ReviewPeriod] nvarchar(20) NOT NULL,
    [ReviewType] nvarchar(30) NOT NULL,
    [Score] decimal(5,2) NULL,
    [Comments] nvarchar(max) NULL,
    [ReviewerId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PerformanceReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PerformanceReviews_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SalesOrders] (
    [Id] uniqueidentifier NOT NULL,
    [OrderNumber] nvarchar(30) NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [QuoteId] uniqueidentifier NULL,
    [OrderDate] datetime2 NOT NULL,
    [PaymentTerms] nvarchar(50) NULL,
    [TradeTerms] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ExchangeRate] decimal(12,6) NOT NULL,
    [TaxType] nvarchar(20) NOT NULL,
    [CustomerPoNumber] nvarchar(50) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ManufacturingStatus] nvarchar(20) NOT NULL,
    [CreditChecked] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Subtotal] decimal(14,2) NOT NULL,
    [TaxAmount] decimal(14,2) NOT NULL,
    [TotalAmount] decimal(14,2) NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [SalespersonId] uniqueidentifier NULL,
    [DepartmentId] uniqueidentifier NULL,
    [SalesChannel] nvarchar(50) NULL,
    [AdvanceDepositAmount] decimal(14,2) NULL,
    [OrderType] nvarchar(20) NULL,
    [ProductionGroupCode] nvarchar(30) NULL,
    [HsCode] nvarchar(20) NULL,
    [Freight] decimal(14,2) NULL,
    [ShippingMethod] nvarchar(50) NULL,
    [Incoterms] nvarchar(10) NULL,
    [ConfirmedDeliveryDate] datetime2 NULL,
    [ConfirmedBy] uniqueidentifier NULL,
    [ConfirmedAt] datetime2 NULL,
    [CustomerDueDate] datetime2 NULL,
    [OrderRemarks] nvarchar(500) NULL,
    CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrders_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrders_Employees_SalespersonId] FOREIGN KEY ([SalespersonId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ShiftSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [YearMonth] nvarchar(7) NOT NULL,
    [ScheduleData] nvarchar(max) NULL,
    [ShiftType] nvarchar(20) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ShiftSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShiftSchedules_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TrainingRecords] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [TrainingName] nvarchar(200) NOT NULL,
    [TrainingProvider] nvarchar(100) NULL,
    [TrainingDate] datetime2 NOT NULL,
    [TrainingHours] int NOT NULL,
    [CertificationResult] nvarchar(20) NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_TrainingRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrainingRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkExperiences] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [CompanyName] nvarchar(200) NULL,
    [Position] nvarchar(100) NULL,
    [Department] nvarchar(100) NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [Responsibilities] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkExperiences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkExperiences_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CompletionOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [CompletionOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [ReceivedQty] decimal(14,2) NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [ManufactureDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [BinLocationId] uniqueidentifier NULL,
    CONSTRAINT [PK_CompletionOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompletionOrderItems_CompletionOrders_CompletionOrderId] FOREIGN KEY ([CompletionOrderId]) REFERENCES [CompletionOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CompletionOrderItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [DemandForecastExts] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [ForecastMonth] nvarchar(7) NOT NULL,
    [ForecastQty] decimal(14,2) NOT NULL,
    [Algorithm] nvarchar(50) NOT NULL,
    [Accuracy] decimal(5,2) NULL,
    [ActualQty] decimal(14,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DemandForecastExts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DemandForecastExts_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryLots] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NOT NULL,
    [Qty] decimal(14,2) NOT NULL,
    [WarehouseId] uniqueidentifier NULL,
    [BinLocationId] uniqueidentifier NULL,
    [ManufactureDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_InventoryLots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryLots_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [TrxId] nvarchar(30) NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [QtyChange] decimal(14,2) NOT NULL,
    [QtyBefore] decimal(14,2) NOT NULL,
    [QtyAfter] decimal(14,2) NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [RelatedDocType] nvarchar(50) NULL,
    [RelatedDocId] uniqueidentifier NULL,
    [WarehouseId] uniqueidentifier NULL,
    [BinLocationId] uniqueidentifier NULL,
    [HandlerId] uniqueidentifier NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [UnitCost] decimal(14,4) NULL,
    [TotalCost] decimal(14,2) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransactions_BinLocations_BinLocationId] FOREIGN KEY ([BinLocationId]) REFERENCES [BinLocations] ([Id]),
    CONSTRAINT [FK_InventoryTransactions_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [MrpPlannedOrders] (
    [Id] uniqueidentifier NOT NULL,
    [MrpRunId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [OrderType] nvarchar(20) NOT NULL,
    [OrderQty] decimal(14,2) NOT NULL,
    [PlannedStartDate] datetime2 NOT NULL,
    [PlannedEndDate] datetime2 NOT NULL,
    [SourceRequirementId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [ConvertedDocumentId] uniqueidentifier NULL,
    [ConvertedDocumentNumber] nvarchar(30) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MrpPlannedOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MrpPlannedOrders_MrpRuns_MrpRunId] FOREIGN KEY ([MrpRunId]) REFERENCES [MrpRuns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MrpPlannedOrders_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MrpRequirements] (
    [Id] uniqueidentifier NOT NULL,
    [MrpRunId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [RequirementType] nvarchar(30) NOT NULL,
    [SourceDocumentId] uniqueidentifier NULL,
    [SourceDocumentNumber] nvarchar(30) NULL,
    [RequiredDate] datetime2 NOT NULL,
    [RequiredQty] decimal(14,2) NOT NULL,
    [AvailableQty] decimal(14,2) NOT NULL,
    [ShortageQty] decimal(14,2) NOT NULL,
    [PlannedReceiptQty] decimal(14,2) NOT NULL,
    [NetRequirement] decimal(14,2) NOT NULL,
    [ActionCode] nvarchar(20) NOT NULL,
    [SubstituteSkuId] uniqueidentifier NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_MrpRequirements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MrpRequirements_MrpRuns_MrpRunId] FOREIGN KEY ([MrpRunId]) REFERENCES [MrpRuns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MrpRequirements_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [QuoteItems] (
    [Id] uniqueidentifier NOT NULL,
    [QuoteId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [UnitPrice] decimal(14,2) NOT NULL,
    [DiscountRate] decimal(5,4) NOT NULL,
    [Subtotal] decimal(14,2) NOT NULL,
    CONSTRAINT [PK_QuoteItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuoteItems_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuoteItems_Skus_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Skus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuoteItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id])
);
GO


CREATE TABLE [Routings] (
    [Id] uniqueidentifier NOT NULL,
    [RoutingCode] nvarchar(30) NOT NULL,
    [RoutingName] nvarchar(100) NOT NULL,
    [ProductSkuId] uniqueidentifier NOT NULL,
    [Version] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Routings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Routings_Skus_ProductSkuId] FOREIGN KEY ([ProductSkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SafetyStockLevels] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [SafetyStockQty] decimal(14,2) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [Reason] nvarchar(500) NULL,
    [SetBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SafetyStockLevels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SafetyStockLevels_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ScrapRecordExts] (
    [Id] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [ScrapQty] decimal(14,2) NOT NULL,
    [ScrapReasonCode] nvarchar(20) NULL,
    [Reason] nvarchar(500) NULL,
    [DispositionCode] nvarchar(30) NULL,
    [CostAmount] decimal(14,4) NOT NULL,
    [ResponsibleDept] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ScrapRecordExts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ScrapRecordExts_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ScrapRecordExts_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SerialNumbers] (
    [Id] uniqueidentifier NOT NULL,
    [SerialNo] nvarchar(50) NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [SkuNumber] nvarchar(50) NULL,
    [SkuName] nvarchar(200) NULL,
    [LotNumber] nvarchar(50) NULL,
    [Warehouse] nvarchar(100) NULL,
    [Location] nvarchar(50) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ManufactureDate] datetime2 NULL,
    [WarrantyExpiry] datetime2 NULL,
    CONSTRAINT [PK_SerialNumbers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SerialNumbers_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SubstituteItems] (
    [Id] uniqueidentifier NOT NULL,
    [GroupId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [Priority] int NOT NULL,
    [ConversionRate] decimal(8,4) NULL,
    [IsPrimary] bit NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [EffectiveFrom] datetime2 NULL,
    [EffectiveTo] datetime2 NULL,
    [ConditionExpression] nvarchar(500) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SubstituteItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubstituteItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubstituteItems_SubstituteGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [SubstituteGroups] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SubstituteUsageLogs] (
    [Id] uniqueidentifier NOT NULL,
    [OriginalSkuId] uniqueidentifier NOT NULL,
    [SubstituteSkuId] uniqueidentifier NOT NULL,
    [WorkOrderId] uniqueidentifier NOT NULL,
    [OriginalQty] decimal(14,4) NOT NULL,
    [SubstituteQty] decimal(14,4) NOT NULL,
    [CostVariance] decimal(14,4) NOT NULL,
    [Reason] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [ApprovedBy] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SubstituteUsageLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SubstituteUsageLogs_Skus_OriginalSkuId] FOREIGN KEY ([OriginalSkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubstituteUsageLogs_Skus_SubstituteSkuId] FOREIGN KEY ([SubstituteSkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubstituteUsageLogs_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TransferOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [TransferOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [TransferQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_TransferOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferOrderItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransferOrderItems_TransferOrders_TransferOrderId] FOREIGN KEY ([TransferOrderId]) REFERENCES [TransferOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [UomConversions] (
    [Id] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NULL,
    [FromUomId] uniqueidentifier NOT NULL,
    [ToUomId] uniqueidentifier NOT NULL,
    [Factor] decimal(18,6) NOT NULL,
    [Operator] nvarchar(1) NOT NULL,
    [InverseFactor] decimal(18,6) NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UomConversions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UomConversions_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]),
    CONSTRAINT [FK_UomConversions_Uoms_FromUomId] FOREIGN KEY ([FromUomId]) REFERENCES [Uoms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UomConversions_Uoms_ToUomId] FOREIGN KEY ([ToUomId]) REFERENCES [Uoms] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AccountingEntries] (
    [Id] uniqueidentifier NOT NULL,
    [VoucherId] uniqueidentifier NOT NULL,
    [GLAccountId] uniqueidentifier NOT NULL,
    [CostCenterId] uniqueidentifier NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [Memo] nvarchar(200) NULL,
    CONSTRAINT [PK_AccountingEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountingEntries_AccountingVouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [AccountingVouchers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AccountingEntries_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]),
    CONSTRAINT [FK_AccountingEntries_GLAccounts_GLAccountId] FOREIGN KEY ([GLAccountId]) REFERENCES [GLAccounts] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityTypes] (
    [Id] uniqueidentifier NOT NULL,
    [ActivityCode] nvarchar(20) NOT NULL,
    [ActivityName] nvarchar(100) NOT NULL,
    [CostCenterId] uniqueidentifier NOT NULL,
    [UnitOfMeasure] nvarchar(10) NOT NULL,
    [StandardRate] decimal(14,4) NOT NULL,
    [ActualRate] decimal(14,4) NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ActivityTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityTypes_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AllocationRules] (
    [Id] uniqueidentifier NOT NULL,
    [SourceCostCenterId] uniqueidentifier NOT NULL,
    [TargetCostCenterId] uniqueidentifier NOT NULL,
    [AllocationBasis] nvarchar(20) NOT NULL,
    [Percentage] decimal(8,4) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AllocationRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AllocationRules_CostCenters_SourceCostCenterId] FOREIGN KEY ([SourceCostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AllocationRules_CostCenters_TargetCostCenterId] FOREIGN KEY ([TargetCostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CostCenterPostings] (
    [Id] uniqueidentifier NOT NULL,
    [CostCenterId] uniqueidentifier NOT NULL,
    [CostElementId] uniqueidentifier NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [Amount] decimal(14,2) NOT NULL,
    [Quantity] decimal(14,4) NOT NULL,
    [ReferenceType] nvarchar(50) NOT NULL,
    [ReferenceId] uniqueidentifier NULL,
    [Notes] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CostCenterPostings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CostCenterPostings_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CostCenterPostings_CostElements_CostElementId] FOREIGN KEY ([CostElementId]) REFERENCES [CostElements] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CostElementAssignments] (
    [Id] uniqueidentifier NOT NULL,
    [CostCenterId] uniqueidentifier NOT NULL,
    [CostElementId] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CostElementAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CostElementAssignments_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CostElementAssignments_CostElements_CostElementId] FOREIGN KEY ([CostElementId]) REFERENCES [CostElements] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkCenterCosts] (
    [Id] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [CostCenterId] uniqueidentifier NOT NULL,
    [LaborRate] decimal(14,4) NOT NULL,
    [OverheadRate] decimal(14,4) NOT NULL,
    [MachineRate] decimal(14,4) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkCenterCosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkCenterCosts_CostCenters_CostCenterId] FOREIGN KEY ([CostCenterId]) REFERENCES [CostCenters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WorkCenterCosts_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PayrollCorrections] (
    [Id] uniqueidentifier NOT NULL,
    [AnomalyId] uniqueidentifier NOT NULL,
    [VerificationResult] nvarchar(50) NULL,
    [AdjustmentItems] nvarchar(max) NULL,
    [AccountingPreview] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CurrentApproverId] uniqueidentifier NULL,
    [ApprovalFlow] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PayrollCorrections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollCorrections_PayrollAnomalies_AnomalyId] FOREIGN KEY ([AnomalyId]) REFERENCES [PayrollAnomalies] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PayrollDetails] (
    [Id] uniqueidentifier NOT NULL,
    [PayrollId] uniqueidentifier NOT NULL,
    [ItemType] nvarchar(20) NOT NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemName] nvarchar(100) NOT NULL,
    [Amount] decimal(14,2) NOT NULL,
    [CalculationBase] decimal(14,2) NULL,
    [CalculationUnit] nvarchar(10) NULL,
    [Quantity] decimal(5,2) NULL,
    [TaxType] nvarchar(10) NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_PayrollDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollDetails_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [DeliveryOrders] (
    [Id] uniqueidentifier NOT NULL,
    [DeliveryNumber] nvarchar(30) NOT NULL,
    [SalesOrderId] uniqueidentifier NOT NULL,
    [DeliveryDate] datetime2 NOT NULL,
    [ShippingMethod] nvarchar(100) NULL,
    [TrackingNumber] nvarchar(100) NULL,
    [DeliveryAddress] nvarchar(500) NULL,
    [SignedBy] nvarchar(100) NULL,
    [SignedAt] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DeliveryOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeliveryOrders_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RoutingOperations] (
    [Id] uniqueidentifier NOT NULL,
    [RoutingId] uniqueidentifier NOT NULL,
    [OperationSeq] int NOT NULL,
    [OperationId] uniqueidentifier NOT NULL,
    [WorkCenterId] uniqueidentifier NOT NULL,
    [TransferTime] decimal(8,2) NOT NULL,
    [WaitTime] decimal(8,2) NOT NULL,
    CONSTRAINT [PK_RoutingOperations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoutingOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Operations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoutingOperations_Routings_RoutingId] FOREIGN KEY ([RoutingId]) REFERENCES [Routings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoutingOperations_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SalesOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [SalesOrderId] uniqueidentifier NOT NULL,
    [LineNumber] int NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NULL,
    [Quantity] decimal(14,2) NOT NULL,
    [UnitPrice] decimal(14,2) NOT NULL,
    [DiscountRate] decimal(5,4) NOT NULL,
    [Subtotal] decimal(14,2) NOT NULL,
    [DeliveredQty] decimal(14,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CustomerItemCode] nvarchar(50) NULL,
    [WarehouseId] uniqueidentifier NULL,
    [TaxCode] nvarchar(20) NULL,
    [TaxRate] nvarchar(20) NULL,
    [BomId] uniqueidentifier NULL,
    [BomVersion] nvarchar(20) NULL,
    [RoutingId] uniqueidentifier NULL,
    [WorkCenterId] uniqueidentifier NULL,
    [NeedManufacturing] bit NOT NULL,
    [NeedOutsourcing] bit NOT NULL,
    [PlannedStartDate] datetime2 NULL,
    [PlannedFinishDate] datetime2 NULL,
    [BackorderQty] decimal(14,2) NOT NULL,
    [LineRemarks] nvarchar(500) NULL,
    [ReviewedBy] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    [ProductionStatus] nvarchar(20) NULL,
    CONSTRAINT [PK_SalesOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrderItems_Boms_BomId] FOREIGN KEY ([BomId]) REFERENCES [Boms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderItems_Routings_RoutingId] FOREIGN KEY ([RoutingId]) REFERENCES [Routings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderItems_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesOrderItems_Skus_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Skus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesOrderItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]),
    CONSTRAINT [FK_SalesOrderItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderItems_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DeliveryOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [DeliveryOrderId] uniqueidentifier NOT NULL,
    [SkuId] uniqueidentifier NOT NULL,
    [LotNumber] nvarchar(50) NULL,
    [DeliveredQty] decimal(14,2) NOT NULL,
    [BinLocationId] uniqueidentifier NULL,
    [SerialNumber] nvarchar(50) NULL,
    CONSTRAINT [PK_DeliveryOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeliveryOrderItems_DeliveryOrders_DeliveryOrderId] FOREIGN KEY ([DeliveryOrderId]) REFERENCES [DeliveryOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesOrderProductionLinks] (
    [Id] uniqueidentifier NOT NULL,
    [SalesOrderItemId] uniqueidentifier NOT NULL,
    [ProductionDocType] nvarchar(20) NOT NULL,
    [ProductionDocId] uniqueidentifier NOT NULL,
    [ProductionDocNo] nvarchar(30) NULL,
    [LinkedQty] decimal(14,2) NOT NULL,
    [LinkedAt] datetime2 NOT NULL,
    [Remarks] nvarchar(500) NULL,
    CONSTRAINT [PK_SalesOrderProductionLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrderProductionLinks_SalesOrderItems_SalesOrderItemId] FOREIGN KEY ([SalesOrderItemId]) REFERENCES [SalesOrderItems] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AccountingEntries_CostCenterId] ON [AccountingEntries] ([CostCenterId]);
GO


CREATE INDEX [IX_AccountingEntries_GLAccountId] ON [AccountingEntries] ([GLAccountId]);
GO


CREATE INDEX [IX_AccountingEntries_VoucherId] ON [AccountingEntries] ([VoucherId]);
GO


CREATE INDEX [IX_AccountingVouchers_ReferenceType_ReferenceId] ON [AccountingVouchers] ([ReferenceType], [ReferenceId]);
GO


CREATE INDEX [IX_AccountingVouchers_VoucherDate] ON [AccountingVouchers] ([VoucherDate]);
GO


CREATE UNIQUE INDEX [IX_AccountingVouchers_VoucherNumber] ON [AccountingVouchers] ([VoucherNumber]);
GO


CREATE INDEX [IX_AccountsPayableItems_AccountsPayableId] ON [AccountsPayableItems] ([AccountsPayableId]);
GO


CREATE UNIQUE INDEX [IX_AccountsPayables_ApNumber] ON [AccountsPayables] ([ApNumber]);
GO


CREATE UNIQUE INDEX [IX_ActivityTypes_ActivityCode] ON [ActivityTypes] ([ActivityCode]);
GO


CREATE INDEX [IX_ActivityTypes_CostCenterId] ON [ActivityTypes] ([CostCenterId]);
GO


CREATE INDEX [IX_AgentTasks_AgentExecutionId] ON [AgentTasks] ([AgentExecutionId]);
GO


CREATE INDEX [IX_AiStocktakeAnalyses_StocktakeOrderId] ON [AiStocktakeAnalyses] ([StocktakeOrderId]);
GO


CREATE UNIQUE INDEX [IX_AiStocktakeMissions_MissionNumber] ON [AiStocktakeMissions] ([MissionNumber]);
GO


CREATE UNIQUE INDEX [IX_AiVisionCaptures_CaptureNumber] ON [AiVisionCaptures] ([CaptureNumber]);
GO


CREATE INDEX [IX_AiVisionCaptures_SkuId] ON [AiVisionCaptures] ([SkuId]);
GO


CREATE INDEX [IX_AllocationRules_SourceCostCenterId_TargetCostCenterId] ON [AllocationRules] ([SourceCostCenterId], [TargetCostCenterId]);
GO


CREATE INDEX [IX_AllocationRules_TargetCostCenterId] ON [AllocationRules] ([TargetCostCenterId]);
GO


CREATE INDEX [IX_AnnouncementReadLogs_AnnouncementId] ON [AnnouncementReadLogs] ([AnnouncementId]);
GO


CREATE INDEX [IX_ApprovalAuthorities_GradeId] ON [ApprovalAuthorities] ([GradeId]);
GO


CREATE INDEX [IX_AsnItems_AsnId] ON [AsnItems] ([AsnId]);
GO


CREATE UNIQUE INDEX [IX_Asns_AsnNumber] ON [Asns] ([AsnNumber]);
GO


CREATE UNIQUE INDEX [IX_AsrsTasks_TaskNumber] ON [AsrsTasks] ([TaskNumber]);
GO


CREATE UNIQUE INDEX [IX_AssetCategories_CategoryCode] ON [AssetCategories] ([CategoryCode]);
GO


CREATE INDEX [IX_AssetDepreciations_AssetId] ON [AssetDepreciations] ([AssetId]);
GO


CREATE UNIQUE INDEX [IX_AssetLocations_LocationCode] ON [AssetLocations] ([LocationCode]);
GO


CREATE INDEX [IX_AssetMaintenances_AssetId] ON [AssetMaintenances] ([AssetId]);
GO


CREATE UNIQUE INDEX [IX_Assets_AssetNumber] ON [Assets] ([AssetNumber]);
GO


CREATE INDEX [IX_Assets_CategoryId] ON [Assets] ([CategoryId]);
GO


CREATE INDEX [IX_AssetStocktakeItems_AssetStocktakeId] ON [AssetStocktakeItems] ([AssetStocktakeId]);
GO


CREATE INDEX [IX_AttendanceRecords_DepartmentId] ON [AttendanceRecords] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_Date] ON [AttendanceRecords] ([EmployeeId], [Date]);
GO


CREATE INDEX [IX_AttendanceRecords_Status] ON [AttendanceRecords] ([Status]);
GO


CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
GO


CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO


CREATE INDEX [IX_BinLocations_WarehouseId] ON [BinLocations] ([WarehouseId]);
GO


CREATE INDEX [IX_BomItems_BomId] ON [BomItems] ([BomId]);
GO


CREATE INDEX [IX_BomPaths_AncestorSkuId_DescendantSkuId_Distance] ON [BomPaths] ([AncestorSkuId], [DescendantSkuId], [Distance]);
GO


CREATE UNIQUE INDEX [IX_Boms_BomCode] ON [Boms] ([BomCode]);
GO


CREATE INDEX [IX_BudgetVersions_BudgetYearId] ON [BudgetVersions] ([BudgetYearId]);
GO


CREATE UNIQUE INDEX [IX_CalendarCategories_Code] ON [CalendarCategories] ([Code]);
GO


CREATE INDEX [IX_CalendarEvents_CategoryCode] ON [CalendarEvents] ([CategoryCode]);
GO


CREATE INDEX [IX_CalendarEvents_MeetingRoomId] ON [CalendarEvents] ([MeetingRoomId]);
GO


CREATE INDEX [IX_CalendarEvents_SourceModule] ON [CalendarEvents] ([SourceModule]);
GO


CREATE INDEX [IX_CalendarEvents_StartDate_EndDate] ON [CalendarEvents] ([StartDate], [EndDate]);
GO


CREATE INDEX [IX_CalendarReminderLogs_CalendarEventId] ON [CalendarReminderLogs] ([CalendarEventId]);
GO


CREATE INDEX [IX_CalendarReminderLogs_Status_ScheduledAt] ON [CalendarReminderLogs] ([Status], [ScheduledAt]);
GO


CREATE INDEX [IX_CalendarShares_CalendarOwnerId] ON [CalendarShares] ([CalendarOwnerId]);
GO


CREATE INDEX [IX_CapacityCalendarEntries_CalendarId] ON [CapacityCalendarEntries] ([CalendarId]);
GO


CREATE INDEX [IX_CapacityCalendars_WorkCenterId] ON [CapacityCalendars] ([WorkCenterId]);
GO


CREATE INDEX [IX_CapacityLoads_ScheduleId] ON [CapacityLoads] ([ScheduleId]);
GO


CREATE INDEX [IX_CapacityLoads_WorkCenterId] ON [CapacityLoads] ([WorkCenterId]);
GO


CREATE INDEX [IX_CashReceiptAllocations_CashReceiptId] ON [CashReceiptAllocations] ([CashReceiptId]);
GO


CREATE UNIQUE INDEX [IX_CashReceipts_ReceiptNumber] ON [CashReceipts] ([ReceiptNumber]);
GO


CREATE INDEX [IX_CompanyHolidays_Date] ON [CompanyHolidays] ([Date]);
GO


CREATE INDEX [IX_CompletionOrderItems_CompletionOrderId] ON [CompletionOrderItems] ([CompletionOrderId]);
GO


CREATE INDEX [IX_CompletionOrderItems_SkuId] ON [CompletionOrderItems] ([SkuId]);
GO


CREATE UNIQUE INDEX [IX_CompletionOrders_CompletionNumber] ON [CompletionOrders] ([CompletionNumber]);
GO


CREATE INDEX [IX_CostCenterPostings_CostCenterId] ON [CostCenterPostings] ([CostCenterId]);
GO


CREATE INDEX [IX_CostCenterPostings_CostElementId] ON [CostCenterPostings] ([CostElementId]);
GO


CREATE UNIQUE INDEX [IX_CostCenters_CostCenterCode] ON [CostCenters] ([CostCenterCode]);
GO


CREATE INDEX [IX_CostCenters_DepartmentId] ON [CostCenters] ([DepartmentId]);
GO


CREATE INDEX [IX_CostCenters_ManagerEmployeeId] ON [CostCenters] ([ManagerEmployeeId]);
GO


CREATE INDEX [IX_CostCenters_ParentCostCenterId] ON [CostCenters] ([ParentCostCenterId]);
GO


CREATE INDEX [IX_CostCenters_WorkCenterId] ON [CostCenters] ([WorkCenterId]);
GO


CREATE UNIQUE INDEX [IX_CostElementAssignments_CostCenterId_CostElementId] ON [CostElementAssignments] ([CostCenterId], [CostElementId]);
GO


CREATE INDEX [IX_CostElementAssignments_CostElementId] ON [CostElementAssignments] ([CostElementId]);
GO


CREATE UNIQUE INDEX [IX_CostElements_ElementCode] ON [CostElements] ([ElementCode]);
GO


CREATE INDEX [IX_CrossDeptTaskReplies_TaskId] ON [CrossDeptTaskReplies] ([TaskId]);
GO


CREATE INDEX [IX_CustomerContacts_CustomerId] ON [CustomerContacts] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Customers] ([CustomerCode]);
GO


CREATE INDEX [IX_Customers_TaxId] ON [Customers] ([TaxId]);
GO


CREATE INDEX [IX_DeliveryOrderItems_DeliveryOrderId] ON [DeliveryOrderItems] ([DeliveryOrderId]);
GO


CREATE UNIQUE INDEX [IX_DeliveryOrders_DeliveryNumber] ON [DeliveryOrders] ([DeliveryNumber]);
GO


CREATE INDEX [IX_DeliveryOrders_SalesOrderId] ON [DeliveryOrders] ([SalesOrderId]);
GO


CREATE INDEX [IX_DemandForecastExts_SkuId] ON [DemandForecastExts] ([SkuId]);
GO


CREATE INDEX [IX_DepartmentPages_DepartmentId] ON [DepartmentPages] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_Departments_DeptCode] ON [Departments] ([DeptCode]);
GO


CREATE INDEX [IX_Departments_ParentDeptId] ON [Departments] ([ParentDeptId]);
GO


CREATE UNIQUE INDEX [IX_Documents_DocNumber] ON [Documents] ([DocNumber]);
GO


CREATE INDEX [IX_DocumentVersions_DocumentId] ON [DocumentVersions] ([DocumentId]);
GO


CREATE INDEX [IX_EcnAffectedBoms_EcnId] ON [EcnAffectedBoms] ([EcnId]);
GO


CREATE UNIQUE INDEX [IX_Ecns_EcnNumber] ON [Ecns] ([EcnNumber]);
GO


CREATE INDEX [IX_Ecns_EcrId] ON [Ecns] ([EcrId]);
GO


CREATE UNIQUE INDEX [IX_Ecrs_EcrNumber] ON [Ecrs] ([EcrNumber]);
GO


CREATE INDEX [IX_EmployeeCertifications_EmployeeId] ON [EmployeeCertifications] ([EmployeeId]);
GO


CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_Employees_EmployeeNo] ON [Employees] ([EmployeeNo]);
GO


CREATE INDEX [IX_Employees_GradeId] ON [Employees] ([GradeId]);
GO


CREATE INDEX [IX_Employees_JobTitleId] ON [Employees] ([JobTitleId]);
GO


CREATE INDEX [IX_Employees_NationalId] ON [Employees] ([NationalId]);
GO


CREATE INDEX [IX_Employees_SupervisorId] ON [Employees] ([SupervisorId]);
GO


CREATE INDEX [IX_EmploymentHistories_EmployeeId] ON [EmploymentHistories] ([EmployeeId]);
GO


CREATE INDEX [IX_EquipmentDowntimes_EquipmentId] ON [EquipmentDowntimes] ([EquipmentId]);
GO


CREATE INDEX [IX_Equipments_SupplierId] ON [Equipments] ([SupplierId]);
GO


CREATE INDEX [IX_Equipments_WorkCenterId] ON [Equipments] ([WorkCenterId]);
GO


CREATE UNIQUE INDEX [IX_EsgDashboardConfigs_ConfigKey] ON [EsgDashboardConfigs] ([ConfigKey]);
GO


CREATE INDEX [IX_ExpenseBudgetItems_ExpenseBudgetId] ON [ExpenseBudgetItems] ([ExpenseBudgetId]);
GO


CREATE UNIQUE INDEX [IX_GLAccounts_AccountCode] ON [GLAccounts] ([AccountCode]);
GO


CREATE INDEX [IX_GLAccounts_ParentAccountId] ON [GLAccounts] ([ParentAccountId]);
GO


CREATE INDEX [IX_GoodsReceiptItems_GoodsReceiptId] ON [GoodsReceiptItems] ([GoodsReceiptId]);
GO


CREATE UNIQUE INDEX [IX_GoodsReceipts_GrNumber] ON [GoodsReceipts] ([GrNumber]);
GO


CREATE UNIQUE INDEX [IX_Guarantees_GuaranteeNumber] ON [Guarantees] ([GuaranteeNumber]);
GO


CREATE UNIQUE INDEX [IX_InventoryLots_SkuId_LotNumber] ON [InventoryLots] ([SkuId], [LotNumber]);
GO


CREATE INDEX [IX_InventoryTransactions_BinLocationId] ON [InventoryTransactions] ([BinLocationId]);
GO


CREATE INDEX [IX_InventoryTransactions_CreatedAt] ON [InventoryTransactions] ([CreatedAt]);
GO


CREATE INDEX [IX_InventoryTransactions_SkuId_CreatedAt] ON [InventoryTransactions] ([SkuId], [CreatedAt]);
GO


CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [InventoryTransactions] ([WarehouseId]);
GO


CREATE INDEX [IX_InvestmentValuations_InvestmentId] ON [InvestmentValuations] ([InvestmentId]);
GO


CREATE INDEX [IX_IpqcReportItems_IpqcReportId] ON [IpqcReportItems] ([IpqcReportId]);
GO


CREATE UNIQUE INDEX [IX_IpqcReports_ReportNumber] ON [IpqcReports] ([ReportNumber]);
GO


CREATE INDEX [IX_IqcReportItems_IqcReportId] ON [IqcReportItems] ([IqcReportId]);
GO


CREATE UNIQUE INDEX [IX_IqcReports_ReportNumber] ON [IqcReports] ([ReportNumber]);
GO


CREATE INDEX [IX_IqcReports_SupplierId] ON [IqcReports] ([SupplierId]);
GO


CREATE INDEX [IX_JobTitles_GradeId] ON [JobTitles] ([GradeId]);
GO


CREATE INDEX [IX_KpiValues_KpiDefinitionId] ON [KpiValues] ([KpiDefinitionId]);
GO


CREATE INDEX [IX_LaborBudgetItems_LaborBudgetId] ON [LaborBudgetItems] ([LaborBudgetId]);
GO


CREATE INDEX [IX_LeaveBalances_EmployeeId] ON [LeaveBalances] ([EmployeeId]);
GO


CREATE UNIQUE INDEX [IX_LeaveRequests_ApplicationNo] ON [LeaveRequests] ([ApplicationNo]);
GO


CREATE INDEX [IX_LeaveRequests_EmployeeId_Status] ON [LeaveRequests] ([EmployeeId], [Status]);
GO


CREATE UNIQUE INDEX [IX_LineApprovalTokens_Token] ON [LineApprovalTokens] ([Token]);
GO


CREATE UNIQUE INDEX [IX_LineUserMappings_LineUserId] ON [LineUserMappings] ([LineUserId]);
GO


CREATE INDEX [IX_LoanRepaymentSchedules_LoanId] ON [LoanRepaymentSchedules] ([LoanId]);
GO


CREATE UNIQUE INDEX [IX_Loans_LoanNumber] ON [Loans] ([LoanNumber]);
GO


CREATE INDEX [IX_MachineOperationRecords_WorkCenterId] ON [MachineOperationRecords] ([WorkCenterId]);
GO


CREATE INDEX [IX_MachineOperationRecords_WorkOrderId] ON [MachineOperationRecords] ([WorkOrderId]);
GO


CREATE INDEX [IX_MassProductionTransitions_ProductionBomId] ON [MassProductionTransitions] ([ProductionBomId]);
GO


CREATE INDEX [IX_MassProductionTransitions_RndProjectId] ON [MassProductionTransitions] ([RndProjectId]);
GO


CREATE INDEX [IX_MaterialCostForecastItems_MaterialCostForecastId] ON [MaterialCostForecastItems] ([MaterialCostForecastId]);
GO


CREATE INDEX [IX_MaterialIssueItems_IssueId] ON [MaterialIssueItems] ([IssueId]);
GO


CREATE UNIQUE INDEX [IX_MaterialIssues_IssueNumber] ON [MaterialIssues] ([IssueNumber]);
GO


CREATE INDEX [IX_MaterialIssues_WorkOrderId] ON [MaterialIssues] ([WorkOrderId]);
GO


CREATE INDEX [IX_MaterialReturnItems_ReturnId] ON [MaterialReturnItems] ([ReturnId]);
GO


CREATE UNIQUE INDEX [IX_MaterialReturns_ReturnNumber] ON [MaterialReturns] ([ReturnNumber]);
GO


CREATE INDEX [IX_MaterialSupplementItems_SupplementId] ON [MaterialSupplementItems] ([SupplementId]);
GO


CREATE UNIQUE INDEX [IX_MaterialSupplements_SupplementNumber] ON [MaterialSupplements] ([SupplementNumber]);
GO


CREATE INDEX [IX_MeetingReservations_RoomId] ON [MeetingReservations] ([RoomId]);
GO


CREATE UNIQUE INDEX [IX_MonthlyPeriods_Year_Month] ON [MonthlyPeriods] ([Year], [Month]);
GO


CREATE INDEX [IX_MrpPlannedOrders_MrpRunId] ON [MrpPlannedOrders] ([MrpRunId]);
GO


CREATE INDEX [IX_MrpPlannedOrders_SkuId] ON [MrpPlannedOrders] ([SkuId]);
GO


CREATE INDEX [IX_MrpRequirements_MrpRunId] ON [MrpRequirements] ([MrpRunId]);
GO


CREATE INDEX [IX_MrpRequirements_SkuId] ON [MrpRequirements] ([SkuId]);
GO


CREATE UNIQUE INDEX [IX_NonConformanceReports_NcrNumber] ON [NonConformanceReports] ([NcrNumber]);
GO


CREATE UNIQUE INDEX [IX_Operations_OperationCode] ON [Operations] ([OperationCode]);
GO


CREATE UNIQUE INDEX [IX_OqcReports_ReportNumber] ON [OqcReports] ([ReportNumber]);
GO


CREATE INDEX [IX_OvertimeRequests_EmployeeId_Date] ON [OvertimeRequests] ([EmployeeId], [Date]);
GO


CREATE UNIQUE INDEX [IX_PaymentBatches_BatchNumber] ON [PaymentBatches] ([BatchNumber]);
GO


CREATE INDEX [IX_PaymentRecords_PaymentBatchId] ON [PaymentRecords] ([PaymentBatchId]);
GO


CREATE INDEX [IX_PayrollAnomalies_EmployeeId] ON [PayrollAnomalies] ([EmployeeId]);
GO


CREATE INDEX [IX_PayrollCorrections_AnomalyId] ON [PayrollCorrections] ([AnomalyId]);
GO


CREATE INDEX [IX_PayrollDetails_PayrollId] ON [PayrollDetails] ([PayrollId]);
GO


CREATE INDEX [IX_Payrolls_EmployeeId_PeriodStart_PeriodEnd] ON [Payrolls] ([EmployeeId], [PeriodStart], [PeriodEnd]);
GO


CREATE INDEX [IX_Payrolls_Status] ON [Payrolls] ([Status]);
GO


CREATE INDEX [IX_PerformanceReviews_EmployeeId] ON [PerformanceReviews] ([EmployeeId]);
GO


CREATE INDEX [IX_Permissions_ParentId] ON [Permissions] ([ParentId]);
GO


CREATE UNIQUE INDEX [IX_Permissions_PermissionCode] ON [Permissions] ([PermissionCode]);
GO


CREATE INDEX [IX_PortalMessages_ThreadId] ON [PortalMessages] ([ThreadId]);
GO


CREATE UNIQUE INDEX [IX_Products_ProductCode] ON [Products] ([ProductCode]);
GO


CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_PoNumber] ON [PurchaseOrders] ([PoNumber]);
GO


CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
GO


CREATE INDEX [IX_PurchaseRequisitionItems_PurchaseRequisitionId] ON [PurchaseRequisitionItems] ([PurchaseRequisitionId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseRequisitions_PrNumber] ON [PurchaseRequisitions] ([PrNumber]);
GO


CREATE INDEX [IX_QuoteItems_ProductId] ON [QuoteItems] ([ProductId]);
GO


CREATE INDEX [IX_QuoteItems_QuoteId] ON [QuoteItems] ([QuoteId]);
GO


CREATE INDEX [IX_QuoteItems_SkuId] ON [QuoteItems] ([SkuId]);
GO


CREATE INDEX [IX_Quotes_CustomerId] ON [Quotes] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_Quotes_QuoteNumber] ON [Quotes] ([QuoteNumber]);
GO


CREATE INDEX [IX_RfqItems_RfqId] ON [RfqItems] ([RfqId]);
GO


CREATE UNIQUE INDEX [IX_Rfqs_RfqNumber] ON [Rfqs] ([RfqNumber]);
GO


CREATE INDEX [IX_RfqSupplierResponses_RfqId] ON [RfqSupplierResponses] ([RfqId]);
GO


CREATE INDEX [IX_RndBudgets_RndProjectId] ON [RndBudgets] ([RndProjectId]);
GO


CREATE INDEX [IX_RndMilestones_RndProjectId] ON [RndMilestones] ([RndProjectId]);
GO


CREATE UNIQUE INDEX [IX_RndProjects_ProjectCode] ON [RndProjects] ([ProjectCode]);
GO


CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO


CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);
GO


CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
GO


CREATE INDEX [IX_RoutingOperations_OperationId] ON [RoutingOperations] ([OperationId]);
GO


CREATE INDEX [IX_RoutingOperations_RoutingId] ON [RoutingOperations] ([RoutingId]);
GO


CREATE INDEX [IX_RoutingOperations_WorkCenterId] ON [RoutingOperations] ([WorkCenterId]);
GO


CREATE INDEX [IX_Routings_ProductSkuId] ON [Routings] ([ProductSkuId]);
GO


CREATE UNIQUE INDEX [IX_Routings_RoutingCode] ON [Routings] ([RoutingCode]);
GO


CREATE INDEX [IX_SafetyStockLevels_SkuId] ON [SafetyStockLevels] ([SkuId]);
GO


CREATE INDEX [IX_SalesForecastItems_SalesForecastId] ON [SalesForecastItems] ([SalesForecastId]);
GO


CREATE INDEX [IX_SalesInvoiceItems_SalesInvoiceId] ON [SalesInvoiceItems] ([SalesInvoiceId]);
GO


CREATE UNIQUE INDEX [IX_SalesInvoices_InvoiceNumber] ON [SalesInvoices] ([InvoiceNumber]);
GO


CREATE INDEX [IX_SalesOrderItems_BomId] ON [SalesOrderItems] ([BomId]);
GO


CREATE INDEX [IX_SalesOrderItems_ProductId] ON [SalesOrderItems] ([ProductId]);
GO


CREATE INDEX [IX_SalesOrderItems_RoutingId] ON [SalesOrderItems] ([RoutingId]);
GO


CREATE UNIQUE INDEX [IX_SalesOrderItems_SalesOrderId_LineNumber] ON [SalesOrderItems] ([SalesOrderId], [LineNumber]);
GO


CREATE INDEX [IX_SalesOrderItems_SkuId] ON [SalesOrderItems] ([SkuId]);
GO


CREATE INDEX [IX_SalesOrderItems_WarehouseId] ON [SalesOrderItems] ([WarehouseId]);
GO


CREATE INDEX [IX_SalesOrderItems_WorkCenterId] ON [SalesOrderItems] ([WorkCenterId]);
GO


CREATE INDEX [IX_SalesOrderProductionLinks_SalesOrderItemId] ON [SalesOrderProductionLinks] ([SalesOrderItemId]);
GO


CREATE INDEX [IX_SalesOrders_CustomerId] ON [SalesOrders] ([CustomerId]);
GO


CREATE INDEX [IX_SalesOrders_DepartmentId] ON [SalesOrders] ([DepartmentId]);
GO


CREATE UNIQUE INDEX [IX_SalesOrders_OrderNumber] ON [SalesOrders] ([OrderNumber]);
GO


CREATE INDEX [IX_SalesOrders_SalespersonId] ON [SalesOrders] ([SalespersonId]);
GO


CREATE UNIQUE INDEX [IX_SampleRequests_RequestNumber] ON [SampleRequests] ([RequestNumber]);
GO


CREATE INDEX [IX_ScheduleConstraints_ScheduleId] ON [ScheduleConstraints] ([ScheduleId]);
GO


CREATE INDEX [IX_ScheduleTasks_ScheduleId] ON [ScheduleTasks] ([ScheduleId]);
GO


CREATE INDEX [IX_ScheduleTasks_WorkCenterId] ON [ScheduleTasks] ([WorkCenterId]);
GO


CREATE INDEX [IX_ScheduleTasks_WorkOrderId] ON [ScheduleTasks] ([WorkOrderId]);
GO


CREATE INDEX [IX_ScrapRecordExts_SkuId] ON [ScrapRecordExts] ([SkuId]);
GO


CREATE INDEX [IX_ScrapRecordExts_WorkOrderId] ON [ScrapRecordExts] ([WorkOrderId]);
GO


CREATE UNIQUE INDEX [IX_SerialNumbers_SerialNo] ON [SerialNumbers] ([SerialNo]);
GO


CREATE INDEX [IX_SerialNumbers_SkuId] ON [SerialNumbers] ([SkuId]);
GO


CREATE UNIQUE INDEX [IX_ShiftSchedules_EmployeeId_YearMonth] ON [ShiftSchedules] ([EmployeeId], [YearMonth]);
GO


CREATE INDEX [IX_Skus_Barcode] ON [Skus] ([Barcode]);
GO


CREATE INDEX [IX_Skus_BaseUomId] ON [Skus] ([BaseUomId]);
GO


CREATE INDEX [IX_Skus_CostUomId] ON [Skus] ([CostUomId]);
GO


CREATE INDEX [IX_Skus_DefaultBinLocationId] ON [Skus] ([DefaultBinLocationId]);
GO


CREATE INDEX [IX_Skus_DefaultSupplierId] ON [Skus] ([DefaultSupplierId]);
GO


CREATE INDEX [IX_Skus_DefaultWarehouseId] ON [Skus] ([DefaultWarehouseId]);
GO


CREATE INDEX [IX_Skus_ProductionUomId] ON [Skus] ([ProductionUomId]);
GO


CREATE INDEX [IX_Skus_PurchaseUomId] ON [Skus] ([PurchaseUomId]);
GO


CREATE INDEX [IX_Skus_SalesUomId] ON [Skus] ([SalesUomId]);
GO


CREATE UNIQUE INDEX [IX_Skus_SkuNumber] ON [Skus] ([SkuNumber]);
GO


CREATE INDEX [IX_StocktakeOrderItems_StocktakeOrderId] ON [StocktakeOrderItems] ([StocktakeOrderId]);
GO


CREATE UNIQUE INDEX [IX_StocktakeOrders_StocktakeNumber] ON [StocktakeOrders] ([StocktakeNumber]);
GO


CREATE INDEX [IX_SubcontractItems_SubcontractOrderId] ON [SubcontractItems] ([SubcontractOrderId]);
GO


CREATE UNIQUE INDEX [IX_SubcontractOrders_SubcontractNumber] ON [SubcontractOrders] ([SubcontractNumber]);
GO


CREATE INDEX [IX_SubstituteItems_GroupId] ON [SubstituteItems] ([GroupId]);
GO


CREATE INDEX [IX_SubstituteItems_SkuId] ON [SubstituteItems] ([SkuId]);
GO


CREATE INDEX [IX_SubstituteUsageLogs_OriginalSkuId] ON [SubstituteUsageLogs] ([OriginalSkuId]);
GO


CREATE INDEX [IX_SubstituteUsageLogs_SubstituteSkuId] ON [SubstituteUsageLogs] ([SubstituteSkuId]);
GO


CREATE INDEX [IX_SubstituteUsageLogs_WorkOrderId] ON [SubstituteUsageLogs] ([WorkOrderId]);
GO


CREATE INDEX [IX_SupplierAsnItems_AsnId] ON [SupplierAsnItems] ([AsnId]);
GO


CREATE INDEX [IX_SupplierBankAccounts_SupplierId] ON [SupplierBankAccounts] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_SupplierCapas_CapaNumber] ON [SupplierCapas] ([CapaNumber]);
GO


CREATE UNIQUE INDEX [IX_SupplierCapas_ComplaintId] ON [SupplierCapas] ([ComplaintId]);
GO


CREATE UNIQUE INDEX [IX_SupplierComplaints_ComplaintNumber] ON [SupplierComplaints] ([ComplaintNumber]);
GO


CREATE INDEX [IX_SupplierComplaints_SupplierId] ON [SupplierComplaints] ([SupplierId]);
GO


CREATE INDEX [IX_SupplierContacts_SupplierId] ON [SupplierContacts] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_SupplierEsgQuestionnaires_QuestionnaireNumber] ON [SupplierEsgQuestionnaires] ([QuestionnaireNumber]);
GO


CREATE INDEX [IX_SupplierEsgQuestionnaires_SupplierId] ON [SupplierEsgQuestionnaires] ([SupplierId]);
GO


CREATE INDEX [IX_SupplierQuotes_RfqId_SupplierId] ON [SupplierQuotes] ([RfqId], [SupplierId]);
GO


CREATE UNIQUE INDEX [IX_Suppliers_SupplierCode] ON [Suppliers] ([SupplierCode]);
GO


CREATE INDEX [IX_SupplierUsers_SupplierId] ON [SupplierUsers] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_SupplierUsers_Username] ON [SupplierUsers] ([Username]);
GO


CREATE UNIQUE INDEX [IX_SystemConfigs_ConfigKey] ON [SystemConfigs] ([ConfigKey]);
GO


CREATE INDEX [IX_TrainingRecords_EmployeeId] ON [TrainingRecords] ([EmployeeId]);
GO


CREATE INDEX [IX_TransferOrderItems_SkuId] ON [TransferOrderItems] ([SkuId]);
GO


CREATE INDEX [IX_TransferOrderItems_TransferOrderId] ON [TransferOrderItems] ([TransferOrderId]);
GO


CREATE UNIQUE INDEX [IX_TransferOrders_TransferNumber] ON [TransferOrders] ([TransferNumber]);
GO


CREATE UNIQUE INDEX [IX_TreasuryTransfers_TransferNumber] ON [TreasuryTransfers] ([TransferNumber]);
GO


CREATE INDEX [IX_TrialProductions_RndProjectId] ON [TrialProductions] ([RndProjectId]);
GO


CREATE INDEX [IX_UomConversions_FromUomId_ToUomId] ON [UomConversions] ([FromUomId], [ToUomId]);
GO


CREATE INDEX [IX_UomConversions_SkuId] ON [UomConversions] ([SkuId]);
GO


CREATE INDEX [IX_UomConversions_ToUomId] ON [UomConversions] ([ToUomId]);
GO


CREATE UNIQUE INDEX [IX_UomGroupMembers_UomGroupId_UomId] ON [UomGroupMembers] ([UomGroupId], [UomId]);
GO


CREATE INDEX [IX_UomGroupMembers_UomId] ON [UomGroupMembers] ([UomId]);
GO


CREATE UNIQUE INDEX [IX_UomGroups_GroupCode] ON [UomGroups] ([GroupCode]);
GO


CREATE UNIQUE INDEX [IX_Uoms_UomCode] ON [Uoms] ([UomCode]);
GO


CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);
GO


CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO


CREATE INDEX [IX_UserThemePreferences_UserId] ON [UserThemePreferences] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Warehouses_WarehouseCode] ON [Warehouses] ([WarehouseCode]);
GO


CREATE UNIQUE INDEX [IX_WipReports_ReportNumber] ON [WipReports] ([ReportNumber]);
GO


CREATE INDEX [IX_WipReports_WorkOrderId] ON [WipReports] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkCenterCosts_CostCenterId] ON [WorkCenterCosts] ([CostCenterId]);
GO


CREATE UNIQUE INDEX [IX_WorkCenterCosts_WorkCenterId_EffectiveFrom] ON [WorkCenterCosts] ([WorkCenterId], [EffectiveFrom]);
GO


CREATE UNIQUE INDEX [IX_WorkCenters_WorkCenterCode] ON [WorkCenters] ([WorkCenterCode]);
GO


CREATE INDEX [IX_WorkExperiences_EmployeeId] ON [WorkExperiences] ([EmployeeId]);
GO


CREATE INDEX [IX_WorkOrderMaterials_WorkOrderId] ON [WorkOrderMaterials] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkOrderOperations_OperationId] ON [WorkOrderOperations] ([OperationId]);
GO


CREATE INDEX [IX_WorkOrderOperations_WorkCenterId] ON [WorkOrderOperations] ([WorkCenterId]);
GO


CREATE INDEX [IX_WorkOrderOperations_WorkOrderId] ON [WorkOrderOperations] ([WorkOrderId]);
GO


CREATE UNIQUE INDEX [IX_WorkOrders_WoNumber] ON [WorkOrders] ([WoNumber]);
GO


