namespace ErpBackend.DTOs;

public sealed record IqcInspectionItemRequestDto(
    string InspectionItem,
    string Specification,
    decimal? LowerLimit,
    decimal? UpperLimit,
    string MeasuredValue,
    string Result);

public sealed record CompleteIqcInspectionRequestDto(
    string Result,
    int SampleSize,
    int AcceptNumber,
    int RejectNumber,
    IReadOnlyList<IqcInspectionItemRequestDto> Items);

public sealed record IqcInspectionItemResponseDto(
    Guid Id,
    string? InspectionItem,
    string? Specification,
    decimal? LowerLimit,
    decimal? UpperLimit,
    string? MeasuredValue,
    string Result);

public sealed record IqcInspectionResponseDto(
    Guid Id,
    string ReportNumber,
    Guid GoodsReceiptId,
    string GoodsReceiptNumber,
    Guid? GoodsReceiptItemId,
    Guid? SkuId,
    string? SkuNumber,
    string? SkuName,
    string? SupplierName,
    string? LotNumber,
    decimal ReceivedQty,
    Guid? ReleaseBinLocationId,
    string? ReleaseBinLocationCode,
    string Result,
    int SampleSize,
    int AcceptNumber,
    int RejectNumber,
    Guid? InspectorId,
    DateTime InspectionDate,
    IReadOnlyList<IqcInspectionItemResponseDto> Items);
