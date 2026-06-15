using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Seeders;

public static class UomSeedData
{
    private sealed record UnitSeed(string Code, string NameZh, string NameEn, string Type);

    private static readonly UnitSeed[] Units =
    [
        new("PCS", "個", "Piece (PCS)", "數量單位"),
        new("ITEM", "件", "Item", "數量單位"),
        new("EQP", "台", "Unit", "設備單位"),
        new("SET", "組", "Set", "組合單位"),
        new("BOX", "箱", "Box (BOX)", "包裝單位"),
        new("PK", "包", "Pack (PK)", "包裝單位"),
        new("BAG", "袋", "Bag", "包裝單位"),
        new("DRUM", "桶", "Barrel / Drum", "容器單位"),
        new("BTL", "瓶", "Bottle", "容器單位"),
        new("CAN", "罐", "Can", "容器單位"),
        new("STK", "支", "Piece / Stick", "數量單位"),
        new("STR", "條", "Strip", "數量單位"),
        new("SHT", "張", "Sheet", "平面單位"),
        new("BOOK", "本", "Book / Volume", "數量單位"),
        new("ROLL", "捲", "Roll", "包裝單位"),
        new("DOZ", "打", "Dozen", "數量單位"),
        new("PRS", "雙", "Pair", "配對單位"),
        new("KIT", "套", "Suite / Set", "組合單位"),
        new("PC", "顆", "Piece / Grain", "數量單位"),
        new("GRN", "粒", "Grain", "數量單位"),
        new("HOOP", "箍", "Hoop", "特殊單位"),
        new("BATCH", "批", "Batch", "生產單位"),
        new("TRAY", "盤", "Tray", "容器單位"),
        new("PLT", "棧板", "Pallet", "倉儲單位"),
        new("CARD", "卡", "Card", "數量單位"),
        new("PORT", "份", "Portion", "餐飲單位"),
        new("HR", "小時", "Hour (HR)", "時間單位"),
        new("MIN", "分鐘", "Minute (MIN)", "時間單位"),
        new("SEC", "秒", "Second (SEC)", "時間單位"),
        new("DAY", "天", "Day", "時間單位"),
        new("MON", "月", "Month", "時間單位"),
        new("YR", "年", "Year", "時間單位"),
        new("M", "公尺", "Meter (M)", "長度單位"),
        new("CM", "公分", "Centimeter (CM)", "長度單位"),
        new("MM", "毫米", "Millimeter (MM)", "長度單位"),
        new("KM", "公里", "Kilometer (KM)", "長度單位"),
        new("IN", "英吋", "Inch (IN)", "長度單位"),
        new("FT", "英尺", "Foot (FT)", "長度單位"),
        new("YD", "碼", "Yard (YD)", "長度單位"),
        new("SQM", "平方公尺", "Square Meter (㎡)", "面積單位"),
        new("PING", "坪", "Ping", "面積單位"),
        new("HA", "公頃", "Hectare (HA)", "面積單位"),
        new("CBM", "立方公尺", "Cubic Meter (CBM)", "體積單位"),
        new("L", "公升", "Liter (L)", "容量單位"),
        new("ML", "毫升", "Milliliter (ML)", "容量單位"),
        new("GAL", "加侖", "Gallon (GAL)", "容量單位"),
        new("KG", "公斤", "Kilogram (KG)", "重量單位"),
        new("G", "公克", "Gram (G)", "重量單位"),
        new("MG", "毫克", "Milligram (MG)", "重量單位"),
        new("TON", "公噸", "Metric Ton (TON)", "重量單位"),
        new("LB", "磅", "Pound (LB)", "重量單位"),
        new("OZ", "盎司", "Ounce (OZ)", "重量單位"),
        new("A", "安培", "Ampere (A)", "電力單位"),
        new("V", "伏特", "Volt (V)", "電力單位"),
        new("W", "瓦特", "Watt (W)", "電力單位"),
        new("KW", "千瓦", "Kilowatt (KW)", "電力單位"),
        new("KWH", "度", "Kilowatt-hour (KWH)", "電力單位"),
        new("PERSON", "人", "Person", "人員單位"),
        new("VEH", "車", "Vehicle", "運輸單位"),
        new("VOY", "船次", "Voyage", "運輸單位"),
        new("CNT", "櫃", "Container", "物流單位"),
        new("FCL", "呎櫃", "Foot Container", "物流單位"),
        new("OCC", "次", "Time / Occurrence", "次數單位"),
        new("REC", "筆", "Record", "資料單位"),
        new("HOUSE", "戶", "Household / Account", "客戶單位"),
        new("ROOM", "間", "Room", "空間單位"),
        new("BED", "床", "Bed", "醫療／住宿單位"),
        new("SEAT", "位", "Seat / Person", "人員單位"),
        new("NAME", "名", "Person", "人員單位")
    ];

    public static async Task<int> SeedAsync(ErpGeneralDbContext db, CancellationToken cancellationToken = default)
    {
        var existingCodes = await db.Uoms
            .AsNoTracking()
            .Select(u => u.UomCode)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var toInsert = new List<Uom>();

        for (var i = 0; i < Units.Length; i++)
        {
            var unit = Units[i];
            if (existingSet.Contains(unit.Code))
                continue;

            toInsert.Add(new Uom
            {
                UomCode = unit.Code,
                UomName = unit.NameZh,
                UomNameEn = unit.NameEn,
                UomType = unit.Type,
                DecimalPlaces = GetDecimalPlaces(unit.Type),
                IsActive = true,
                AllowDecimal = AllowDecimal(unit.Type),
                Description = $"{unit.NameZh} / {unit.NameEn}",
                DisplayOrder = i + 1,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (toInsert.Count == 0)
            return 0;

        db.Uoms.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);
        return toInsert.Count;
    }

    private static int GetDecimalPlaces(string type) => type switch
    {
        "長度單位" or "面積單位" or "體積單位" or "容量單位" or "重量單位" => 3,
        "時間單位" or "電力單位" => 2,
        _ => 0
    };

    private static bool AllowDecimal(string type) => type is
        "長度單位" or "面積單位" or "體積單位" or "容量單位" or "重量單位" or "時間單位" or "電力單位";
}
