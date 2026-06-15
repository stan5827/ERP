BEGIN TRANSACTION;
GO

ALTER TABLE [WorkOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkOrderOperations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [WorkOrderOperations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkOrderOperations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkOrderOperations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkOrderMaterials] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [WorkOrderMaterials] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkOrderMaterials] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkOrderMaterials] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkExperiences] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkExperiences] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkExperiences] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkExperiences] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [WorkCenters] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [WorkCenters] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkCenters] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkCenters] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkCenters] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [WorkCenterCosts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkCenterCosts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WorkCenterCosts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WorkCenterCosts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [work_order_material_actuals] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [work_order_material_actuals] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_material_actuals] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [work_order_material_actuals] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_material_actuals] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [work_order_machine_actuals] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_machine_actuals] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [work_order_machine_actuals] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_machine_actuals] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [work_order_labor_actuals] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_labor_actuals] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [work_order_labor_actuals] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_labor_actuals] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [work_order_costs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [work_order_costs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [work_order_costs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [work_order_costs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WipReports] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WipReports] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [WipReports] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [WipReports] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Warehouses] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Warehouses] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Warehouses] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Warehouses] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Warehouses] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [UserThemePreferences] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [UserThemePreferences] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UserThemePreferences] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [UserThemePreferences] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UserThemePreferences] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Users] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Users] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Users] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Users] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [UserRoles] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [UserRoles] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UserRoles] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [UserRoles] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UserRoles] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Uoms] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Uoms] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Uoms] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Uoms] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [UomGroups] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomGroups] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [UomGroups] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomGroups] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [UomGroupMembers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomGroupMembers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [UomGroupMembers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomGroupMembers] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [UomConversions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomConversions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [UomConversions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [UomConversions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [TrialProductions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TrialProductions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [TrialProductions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TreasuryTransfers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TreasuryTransfers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [TreasuryTransfers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TreasuryTransfers] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [TransferOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [TransferOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TransferOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [TransferOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TransferOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [TransferOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TrainingRecords] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [TrainingRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TrainingRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [TrainingRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [TrainingRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SystemNotifications] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SystemNotifications] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SystemNotifications] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SystemNotifications] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SystemConfigs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SystemConfigs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SystemConfigs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SystemConfigs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SystemConfigs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SupplierUsers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierUsers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierUsers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierUsers] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Suppliers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Suppliers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Suppliers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierQuotes] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierQuotes] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierQuotes] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierQuotes] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierQuotes] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SupplierPerformanceScores] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierPerformanceScores] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierPerformanceScores] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierPerformanceScores] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierPerformanceScores] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SupplierEsgQuestionnaires] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierEsgQuestionnaires] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierEsgQuestionnaires] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierDocuments] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierDocuments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierDocuments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierDocuments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierContacts] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierContacts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierContacts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierContacts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierContacts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SupplierComplaints] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierComplaints] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierComplaints] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierCapas] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierCapas] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierCapas] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierBankAccounts] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierBankAccounts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierBankAccounts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierBankAccounts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierBankAccounts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SupplierAsns] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierAsns] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierAsnItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SupplierAsnItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierAsnItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SupplierAsnItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SupplierAsnItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SubstituteUsageLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteUsageLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SubstituteUsageLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteUsageLogs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SubstituteItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SubstituteItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteGroups] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteGroups] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SubstituteGroups] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubstituteGroups] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SubcontractOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubcontractOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SubcontractOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubcontractItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SubcontractItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubcontractItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SubcontractItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SubcontractItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [StocktakeOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [StocktakeOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [StocktakeOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [StocktakeOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [StocktakeOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [StocktakeOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [StocktakeOrderItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Skus] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Skus] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Skus] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ShiftSchedules] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ShiftSchedules] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ShiftSchedules] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ShiftSchedules] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ShiftSchedules] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SerialNumbers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SerialNumbers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SerialNumbers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SerialNumbers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScrapRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScrapRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ScrapRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScrapRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ScrapRecordExts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScrapRecordExts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ScrapRecordExts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScrapRecordExts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ScheduleTasks] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ScheduleTasks] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScheduleTasks] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ScheduleTasks] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScheduleConstraints] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ScheduleConstraints] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScheduleConstraints] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ScheduleConstraints] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ScheduleConstraints] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SampleRequests] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SampleRequests] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SampleRequests] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrders] ADD [BaseSubtotal] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesOrders] ADD [BaseTaxAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesOrders] ADD [BaseTotalAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrders] ADD [TaxCode] nvarchar(20) NULL;
GO

ALTER TABLE [SalesOrderProductionLinks] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SalesOrderProductionLinks] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrderProductionLinks] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesOrderProductionLinks] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrderProductionLinks] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SalesOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SalesOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesInvoices] ADD [BaseSalesAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesInvoices] ADD [BaseTaxAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesInvoices] ADD [BaseTotalAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesInvoices] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesInvoices] ADD [Currency] nvarchar(10) NOT NULL DEFAULT N'';
GO

ALTER TABLE [SalesInvoices] ADD [ExchangeRate] decimal(12,6) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [SalesInvoices] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesInvoices] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesInvoices] ADD [TaxCode] nvarchar(20) NULL;
GO

ALTER TABLE [SalesInvoiceItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SalesInvoiceItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesInvoiceItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesInvoiceItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesInvoiceItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SalesForecasts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesForecasts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesForecasts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesForecasts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SalesForecastItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [SalesForecastItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesForecastItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SalesForecastItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SalesForecastItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [SafetyStockLevels] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SafetyStockLevels] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [SafetyStockLevels] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [SafetyStockLevels] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Routings] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Routings] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Routings] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Routings] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Routings] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RoutingOperations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RoutingOperations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RoutingOperations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RoutingOperations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RoutingOperations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Roles] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Roles] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Roles] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Roles] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RolePermissions] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RolePermissions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RolePermissions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RolePermissions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RolePermissions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RndProjects] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndProjects] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RndProjects] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndMilestones] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RndMilestones] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndMilestones] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RndMilestones] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RndLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndLogs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RndBudgets] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RndBudgets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndBudgets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RndBudgets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RndBudgets] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RfqSupplierResponses] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RfqSupplierResponses] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RfqSupplierResponses] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RfqSupplierResponses] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RfqSupplierResponses] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Rfqs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Rfqs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Rfqs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RfqItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RfqItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RfqItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RfqItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RfqItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RccpResources] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RccpResources] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RccpResources] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RccpResources] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RccpResources] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [RccpItemResources] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [RccpItemResources] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RccpItemResources] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [RccpItemResources] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [RccpItemResources] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Quotes] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Quotes] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Quotes] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [QuoteItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [QuoteItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [QuoteItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [QuoteItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [QuoteItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [PurchaseRequisitions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseRequisitions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PurchaseRequisitions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseRequisitionItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [PurchaseRequisitionItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseRequisitionItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PurchaseRequisitionItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseRequisitionItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [PurchaseOrders] ADD [BaseSubtotal] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [PurchaseOrders] ADD [BaseTaxAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [PurchaseOrders] ADD [BaseTotalAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [PurchaseOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PurchaseOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [PurchaseOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PurchaseOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PurchaseOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ProductSpecifications] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ProductSpecifications] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ProductSpecifications] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ProductSpecifications] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ProductSpecifications] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Products] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Products] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Products] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Products] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Products] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ProductionSchedules] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ProductionSchedules] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PortalMessageThreads] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PortalMessageThreads] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PortalMessages] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PortalMessages] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PortalMessages] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PortalMessages] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Permissions] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Permissions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Permissions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Permissions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Permissions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [PerformanceReviews] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PerformanceReviews] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PerformanceReviews] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Payrolls] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Payrolls] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Payrolls] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollDetails] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [PayrollDetails] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollDetails] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PayrollDetails] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollDetails] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [PayrollCorrections] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollCorrections] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PayrollCorrections] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollAnomalies] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PayrollAnomalies] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PayrollAnomalies] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentRecords] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [PaymentRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PaymentRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [PaymentBatches] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentBatches] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [PaymentBatches] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [OvertimeRequests] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [OvertimeRequests] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [OvertimeRequests] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [OqcReports] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [OqcReports] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [OqcReports] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [OqcReports] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [OqcReports] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Operations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Operations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Operations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Operations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Operations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [NotificationQueues] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [NotificationQueues] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [NotificationQueues] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [NonConformanceReports] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [NonConformanceReports] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [NonConformanceReports] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [NonConformanceReports] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MrpRuns] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MrpRuns] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MrpRuns] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MrpRuns] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MrpRequirements] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MrpRequirements] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MrpRequirements] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MrpRequirements] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MrpRequirements] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MrpPlannedOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MrpPlannedOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MrpPlannedOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsLines] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MpsLines] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsLines] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MpsLines] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsLines] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MpsHeaders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MpsHeaders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsExceptions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsExceptions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MpsExceptions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MpsExceptions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MonthlyPeriods] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MonthlyPeriods] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MonthlyPeriods] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MeetingRooms] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MeetingRooms] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MeetingRooms] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MeetingRooms] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MeetingRooms] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MeetingReservations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MeetingReservations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MeetingReservations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialSupplements] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialSupplements] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialSupplementItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MaterialSupplementItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialSupplementItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialSupplementItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialSupplementItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MaterialReturns] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialReturns] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialReturns] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialReturnItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MaterialReturnItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialReturnItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialReturnItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialReturnItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MaterialIssues] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialIssues] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialIssues] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialIssueItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MaterialIssueItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialIssueItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialIssueItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialCostForecasts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialCostForecasts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialCostForecasts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialCostForecasts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MaterialCostForecastItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MaterialCostForecastItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialCostForecastItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaterialCostForecastItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaterialCostForecastItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MassProductionTransitions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MassProductionTransitions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MassProductionTransitions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaintenanceRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaintenanceRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaintenanceRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaintenanceRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [MaintenancePlans] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [MaintenancePlans] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MaintenancePlans] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MaintenancePlans] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MachineOperationRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MachineOperationRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [MachineOperationRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [MachineOperationRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Loans] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Loans] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Loans] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LoanRepaymentSchedules] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LoanRepaymentSchedules] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LoanRepaymentSchedules] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LoanRepaymentSchedules] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineUserMappings] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LineUserMappings] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineUserMappings] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LineUserMappings] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineUserMappings] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LineNotificationLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineNotificationLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LineNotificationLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineNotificationLogs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LineConfigs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LineConfigs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineConfigs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LineConfigs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineConfigs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LineApprovalTokens] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineApprovalTokens] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LineApprovalTokens] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LineApprovalTokens] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LeaveRequests] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LeaveRequests] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LeaveRequests] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LeaveBalances] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LeaveBalances] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LeaveBalances] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LeaveBalances] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LeaveBalances] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LaborBudgets] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LaborBudgets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LaborBudgets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LaborBudgets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LaborBudgets] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [LaborBudgetItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [LaborBudgetItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LaborBudgetItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [LaborBudgetItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [LaborBudgetItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [KpiValues] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [KpiValues] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [KpiValues] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [KpiValues] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [KpiDefinitions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [KpiDefinitions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [KpiDefinitions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [KpiDefinitions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [JobTitles] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [JobTitles] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [JobTitles] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [JobTitles] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [JobGrades] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [JobGrades] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [JobGrades] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [JobGrades] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [IqcReports] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [IqcReports] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IqcReports] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [IqcReports] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IqcReports] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [IqcReportItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [IqcReportItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IqcReportItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [IqcReportItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IqcReportItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [IpqcReports] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [IpqcReports] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IpqcReports] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [IpqcReports] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IpqcReports] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [IpqcReportItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [IpqcReportItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IpqcReportItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [IpqcReportItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [IpqcReportItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [InvestmentValuations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [InvestmentValuations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [InvestmentValuations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [InvestmentValuations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [InvestmentValuations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Investments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Investments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Investments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Investments] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [InventoryTransactions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryTransactions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [InventoryTransactions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryTransactions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [InventoryLots] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [InventoryLots] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryLots] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [InventoryLots] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Guarantees] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Guarantees] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Guarantees] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Guarantees] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [GoodsReceipts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GoodsReceipts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [GoodsReceipts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GoodsReceiptItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [GoodsReceiptItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GoodsReceiptItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [GoodsReceiptItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GoodsReceiptItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [GLAccounts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GLAccounts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [GLAccounts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GLAccounts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [GhgEmissionSources] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [GhgEmissionSources] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GhgEmissionSources] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [GhgEmissionSources] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GhgEmissionSources] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [GhgEmissionRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GhgEmissionRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [GhgEmissionRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [GhgEmissionRecords] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ForecastConsumptionDetails] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ForecastConsumptionDetails] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ForecastConsumptionDetails] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ForecastConsumptionDetails] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ForecastConsumptionDetails] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ExpiryAlerts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpiryAlerts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ExpiryAlerts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpiryAlerts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ExpenseBudgets] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ExpenseBudgets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpenseBudgets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ExpenseBudgets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpenseBudgets] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ExpenseBudgetItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [ExpenseBudgetItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpenseBudgetItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ExpenseBudgetItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExpenseBudgetItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ExchangeRates] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExchangeRates] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ExchangeRates] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ExchangeRates] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [EsgDashboardConfigs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [EsgDashboardConfigs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EsgDashboardConfigs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EsgDashboardConfigs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EsgDashboardConfigs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Equipments] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [Equipments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Equipments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Equipments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EquipmentHistories] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [EquipmentHistories] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EquipmentHistories] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EquipmentHistories] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EquipmentHistories] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [EquipmentDowntimes] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EquipmentDowntimes] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EquipmentDowntimes] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmploymentHistories] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmploymentHistories] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EmploymentHistories] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmploymentHistories] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Employees] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Employees] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Employees] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmployeeCertifications] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmployeeCertifications] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EmployeeCertifications] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EmployeeCertifications] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Ecrs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Ecrs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Ecrs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Ecns] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Ecns] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Ecns] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EcnAffectedBoms] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [EcnAffectedBoms] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EcnAffectedBoms] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [EcnAffectedBoms] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [EcnAffectedBoms] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DocumentVersions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DocumentVersions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DocumentVersions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Documents] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Documents] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Documents] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DocumentPermissions] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [DocumentPermissions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DocumentPermissions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DocumentPermissions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DocumentPermissions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Departments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Departments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Departments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Departments] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DepartmentPages] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DepartmentPages] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DepartmentPages] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DepartmentPages] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DemandForecasts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DemandForecasts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DemandForecasts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DemandForecasts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DemandForecastExts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DemandForecastExts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DemandForecastExts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DemandForecastExts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [DeliveryOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DeliveryOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DeliveryOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DeliveryOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [DeliveryOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DeliveryOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [DeliveryOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [DeliveryOrderItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Customers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Customers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Customers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Customers] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CustomerContacts] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CustomerContacts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CustomerContacts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CustomerContacts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CustomerContacts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Currencies] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Currencies] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Currencies] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Currencies] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CrossDeptTasks] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CrossDeptTasks] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CrossDeptTasks] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CrossDeptTaskReplies] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CrossDeptTaskReplies] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CrossDeptTaskReplies] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CrossDeptTaskReplies] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CostElements] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostElements] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CostElements] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostElements] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CostElementAssignments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostElementAssignments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CostElementAssignments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostElementAssignments] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CostCenters] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostCenters] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CostCenters] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostCenters] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CostCenterPostings] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostCenterPostings] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CostCenterPostings] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CostCenterPostings] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CompletionOrders] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompletionOrders] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CompletionOrders] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompletionOrderItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CompletionOrderItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompletionOrderItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CompletionOrderItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompletionOrderItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CompanyHolidays] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompanyHolidays] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CompanyHolidays] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CompanyHolidays] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CashReceipts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CashReceipts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CashReceipts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CashReceipts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CashReceiptAllocations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CashReceiptAllocations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CashReceiptAllocations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CashReceiptAllocations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CashReceiptAllocations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CarbonFootprintMappings] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CarbonFootprintMappings] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CarbonFootprintMappings] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CarbonFootprintMappings] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CarbonFootprintMappings] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CapexBudgets] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CapexBudgets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapexBudgets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CapexBudgets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapexBudgets] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CapacityLoads] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CapacityLoads] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityLoads] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CapacityLoads] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityLoads] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CapacityCalendars] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityCalendars] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CapacityCalendars] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityCalendars] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CapacityCalendarEntries] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CapacityCalendarEntries] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityCalendarEntries] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CapacityCalendarEntries] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CapacityCalendarEntries] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CalendarShares] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarShares] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CalendarShares] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarShares] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CalendarReminderLogs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CalendarReminderLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarReminderLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CalendarReminderLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarEvents] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarEvents] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CalendarEvents] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarCategories] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarCategories] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CalendarCategories] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarCategories] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [CalendarBuckets] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [CalendarBuckets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarBuckets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [CalendarBuckets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [CalendarBuckets] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [BudgetYears] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BudgetYears] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BudgetVersions] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BudgetVersions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BudgetVersions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BudgetVersions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BudgetVersions] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Boms] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Boms] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Boms] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BomPaths] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BomPaths] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BomPaths] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BomPaths] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BomPaths] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [BomItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BomItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BomItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BomItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BomItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [BinLocations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BinLocations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BinLocations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BinLocations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BinLocations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [BarcodeScanHistories] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BarcodeScanHistories] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BarcodeScanHistories] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BarcodeScanHistories] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BarcodeScanHistories] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [BarcodeRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BarcodeRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BarcodeRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BankAccounts] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [BankAccounts] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BankAccounts] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [BankAccounts] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [BankAccounts] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AutoReplenishments] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AutoReplenishments] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AutoReplenishments] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AuditLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AuditLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AuditLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AuditLogs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AttendanceRecords] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AttendanceRecords] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AttendanceRecords] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AttendanceRecords] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetTransfers] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetTransfers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetTransfers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetTransfers] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AssetStocktakes] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetStocktakes] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetStocktakes] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetStocktakeItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetStocktakeItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetStocktakeItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetStocktakeItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetStocktakeItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Assets] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Assets] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Assets] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetMaintenances] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetMaintenances] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetMaintenances] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetMaintenances] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetMaintenances] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AssetLocations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetLocations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetLocations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetLocations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetLocations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AssetDisposals] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetDisposals] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetDisposals] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetDisposals] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AssetDepreciations] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetDepreciations] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetDepreciations] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetDepreciations] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetDepreciations] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AssetCategories] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AssetCategories] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetCategories] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AssetCategories] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AssetCategories] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AsrsTasks] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AsrsTasks] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AsrsTasks] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Asns] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Asns] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Asns] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AsnItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AsnItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AsnItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AsnItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AsnItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ApsScheduleResults] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ApsScheduleResults] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ApsScheduleResults] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ApsScheduleResults] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [ApprovalAuthorities] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ApprovalAuthorities] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ApprovalAuthorities] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ApprovalAuthorities] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [Announcements] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Announcements] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [Announcements] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [Announcements] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AnnouncementReadLogs] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AnnouncementReadLogs] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AnnouncementReadLogs] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AnnouncementReadLogs] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AnnouncementReadLogs] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AllocationRules] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AllocationRules] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AllocationRules] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AllocationRules] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AiVisionCaptures] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiVisionCaptures] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiVisionCaptures] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiVisionCaptures] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AiStocktakeMissions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiStocktakeMissions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiStocktakeMissions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiStocktakeAnalyses] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiStocktakeAnalyses] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiStocktakeAnalyses] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiStocktakeAnalyses] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AiInsights] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiInsights] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiInsights] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiFormGuidances] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiFormGuidances] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiFormGuidances] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiFormGuidances] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AiAnomalyDetections] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiAnomalyDetections] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AiAnomalyDetections] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AiAnomalyDetections] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AgentTasks] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AgentTasks] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AgentTasks] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AgentTasks] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AgentExecutions] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AgentExecutions] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AgentExecutions] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ActivityTypes] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ActivityTypes] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [ActivityTypes] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [ActivityTypes] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AccountsPayables] ADD [BaseInvoiceAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [AccountsPayables] ADD [BaseTaxAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [AccountsPayables] ADD [BaseTotalAmount] decimal(14,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [AccountsPayables] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountsPayables] ADD [Currency] nvarchar(10) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AccountsPayables] ADD [ExchangeRate] decimal(12,6) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [AccountsPayables] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AccountsPayables] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountsPayables] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AccountsPayables] ADD [TaxCode] nvarchar(20) NULL;
GO

ALTER TABLE [AccountsPayables] ADD [TaxType] nvarchar(20) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AccountsPayableItems] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AccountsPayableItems] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountsPayableItems] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AccountsPayableItems] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountsPayableItems] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

ALTER TABLE [AccountingVouchers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AccountingVouchers] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AccountingVouchers] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountingEntries] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
GO

ALTER TABLE [AccountingEntries] ADD [CreatedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountingEntries] ADD [ModifiedAt] datetime2 NULL;
GO

ALTER TABLE [AccountingEntries] ADD [ModifiedBy] uniqueidentifier NULL;
GO

ALTER TABLE [AccountingEntries] ADD [Status] nvarchar(20) NULL DEFAULT N'Active';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422034237_Phase1_AuditAndMultiCurrency', N'8.0.11');
GO

COMMIT;
GO

