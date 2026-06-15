namespace ErpBackend.Helpers;

public static class DocumentNumberGenerator
{
    private static readonly object _lock = new();
    private static long _sequence;
    
    public static string Generate(string prefix)
    {
        lock (_lock)
        {
            _sequence++;
            var timestamp = DateTime.UtcNow.Ticks % 1000000000000;
            return $"{prefix}-{timestamp}-{_sequence:X4}";
        }
    }
    
    public static string GenerateShort(string prefix)
    {
        var guid = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"{prefix}-{guid}";
    }
    
    public static string GenerateWithDate(string prefix)
    {
        var now = DateTime.UtcNow;
        var datePart = now.ToString("yyyyMMdd");
        var timePart = now.ToString("HHmmss");
        var seq = (Interlocked.Increment(ref _sequence) % 10000).ToString("D4");
        return $"{prefix}-{datePart}-{timePart}-{seq}";
    }
    
    public static string GenerateWorkOrder() => Generate("WO");
    public static string GeneratePurchaseRequisition() => Generate("PR");
    public static string GeneratePurchaseOrder() => Generate("PO");
    public static string GenerateSalesOrder() => Generate("SO");
    public static string GenerateQuote() => Generate("QO");
    public static string GenerateMaterialIssue() => Generate("MAT");
    public static string GenerateSchedule() => Generate("SCH");
    public static string GenerateMrpRun() => Generate("MRP");
    public static string GenerateInventoryAdj() => Generate("ADJ");
    public static string GenerateCashReceipt() => Generate("CR");
    public static string GenerateMps() => Generate("MPS");
    public static string GenerateEcn() => Generate("ECN");
    public static string GenerateWipReport() => Generate("WIR");
    public static string GenerateDelivery() => Generate("DEL");
}