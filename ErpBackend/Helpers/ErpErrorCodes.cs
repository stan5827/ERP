namespace ErpBackend.Helpers;

public static class ErpErrorCodes
{
    // General
    public const int Success = 0;
    public const int GeneralError = 1000;
    public const int Unauthorized = 1001;
    public const int Forbidden = 1002;
    public const int NotFound = 1003;
    public const int ValidationError = 1004;

    // Inventory
    public const int StockShortage = 2001;
    public const int WarehouseLocked = 2002;
    public const int InvalidSku = 2003;

    // Sales & Procurement
    public const int OrderClosed = 3001;
    public const int SupplierBlacklisted = 3002;
    public const int CreditLimitExceeded = 3003;

    // Manufacturing
    public const int BomNotEffective = 4001;
    public const int ResourceConflict = 4002;
    public const int WorkOrderLocked = 4003;

    // HR
    public const int EmployeeInactive = 5001;
    public const int AttendanceAnomaly = 5002;

    // MRP
    public const int MrpRunFailed = 6001;
    public const int MrpInvalidHorizon = 6002;
    public const int MrpConversionFailed = 6003;
}
