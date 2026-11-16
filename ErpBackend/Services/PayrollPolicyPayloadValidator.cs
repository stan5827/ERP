using System.Text.Json;

namespace ErpBackend.Services;

public static class PayrollPolicyPayloadValidator
{
    private static readonly string[] InsuranceTableNames =
    [
        "laborInsurance",
        "healthInsurance",
        "employmentInsurance",
        "occupationalAccidentInsurance"
    ];

    public static bool IsValidJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static IReadOnlyList<string> GetMissingFields(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var missing = new List<string>();

        if (!TryArray(root, "overtimeRules", out var overtimeRules) || overtimeRules.GetArrayLength() == 0)
        {
            missing.Add("overtimeRules");
        }
        else
        {
            ValidateOvertimeRules(overtimeRules, missing);
        }

        if (!TryObject(root, "insuranceTables", out var insuranceTables))
        {
            missing.Add("insuranceTables");
        }
        else
        {
            foreach (var tableName in InsuranceTableNames)
            {
                if (!TryArray(insuranceTables, tableName, out var table) || table.GetArrayLength() == 0)
                    missing.Add($"insuranceTables.{tableName}");
                else
                    ValidateInsuranceTable(table, tableName, missing);
            }
        }

        if (!TryObject(root, "pensionRules", out var pensionRules))
        {
            missing.Add("pensionRules");
        }
        else
        {
            ValidateNumbers(pensionRules, "pensionRules", ["employerRate", "employeeMinRate", "employeeMaxRate"], missing);
            if (HasNumber(pensionRules, "employeeMinRate", out var minimum)
                && HasNumber(pensionRules, "employeeMaxRate", out var maximum)
                && minimum > maximum)
                missing.Add("pensionRules.employeeMinRate_must_not_exceed_employeeMaxRate");
        }

        if (!TryObject(root, "taxRules", out var taxRules))
        {
            missing.Add("taxRules");
        }
        else
        {
            if (!TryArray(taxRules, "withholdingTable", out var withholdingTable) || withholdingTable.GetArrayLength() == 0)
                missing.Add("taxRules.withholdingTable");
            else
                ValidateRows(withholdingTable, "taxRules.withholdingTable", [], ["lowerBound", "rate"], missing);

            if (!TryObject(taxRules, "supplementalHealthInsurance", out var supplementalRules))
                missing.Add("taxRules.supplementalHealthInsurance");
            else
                ValidateNumbers(
                    supplementalRules,
                    "taxRules.supplementalHealthInsurance",
                    ["threshold", "rate"],
                    missing);
        }

        if (!TryObject(root, "leaveRules", out var leaveRules))
        {
            missing.Add("leaveRules");
        }
        else
        {
            ValidateNumbers(leaveRules, "leaveRules", ["dailyDivisor", "hourlyDivisor"], missing, requirePositive: true);
            if (!TryArray(leaveRules, "deductions", out var deductions) || deductions.GetArrayLength() == 0)
                missing.Add("leaveRules.deductions");
            else
                ValidateRows(deductions, "leaveRules.deductions", ["leaveType"], ["deductionRate"], missing);
        }

        return missing;
    }

    private static void ValidateRows(
        JsonElement rows,
        string path,
        IReadOnlyList<string> requiredStringFields,
        IReadOnlyList<string> requiredNumberFields,
        ICollection<string> missing)
    {
        var index = 0;
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                missing.Add($"{path}[{index}]");
                index++;
                continue;
            }

            foreach (var field in requiredStringFields)
            {
                if (!row.TryGetProperty(field, out var value)
                    || value.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(value.GetString()))
                    missing.Add($"{path}[{index}].{field}");
            }

            foreach (var field in requiredNumberFields)
            {
                if (!row.TryGetProperty(field, out var value)
                    || value.ValueKind != JsonValueKind.Number
                    || !value.TryGetDouble(out var number)
                    || !double.IsFinite(number)
                    || number < 0)
                    missing.Add($"{path}[{index}].{field}");
            }

            index++;
        }
    }

    private static void ValidateOvertimeRules(JsonElement rows, ICollection<string> missing)
    {
        ValidateRows(
            rows,
            "overtimeRules",
            ["dayType", "multiplierMode"],
            ["fromHour", "toHour", "multiplier"],
            missing);
        var index = 0;
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            if (HasNumber(row, "fromHour", out var fromHour)
                && HasNumber(row, "toHour", out var toHour)
                && toHour <= fromHour)
                missing.Add($"overtimeRules[{index}].toHour_must_exceed_fromHour");
            if (HasNumber(row, "multiplier", out var multiplier) && multiplier <= 0)
                missing.Add($"overtimeRules[{index}].multiplier_must_be_positive");
            if (row.TryGetProperty("multiplierMode", out var mode)
                && mode.ValueKind == JsonValueKind.String
                && mode.GetString() is not ("additional" or "total"))
                missing.Add($"overtimeRules[{index}].multiplierMode_must_be_additional_or_total");

            index++;
        }
    }

    private static void ValidateInsuranceTable(JsonElement rows, string tableName, ICollection<string> missing)
    {
        var path = $"insuranceTables.{tableName}";
        ValidateRows(rows, path, ["grade"], ["monthlyBase", "employeeRate", "employerRate"], missing);
        var index = 0;
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object
                && HasNumber(row, "monthlyBase", out var monthlyBase)
                && monthlyBase <= 0)
                missing.Add($"{path}[{index}].monthlyBase_must_be_positive");
            index++;
        }
    }

    private static void ValidateNumbers(
        JsonElement value,
        string path,
        IReadOnlyList<string> fields,
        ICollection<string> missing,
        bool requirePositive = false)
    {
        foreach (var field in fields)
        {
            if (!HasNumber(value, field, out var number) || (requirePositive && number <= 0))
                missing.Add($"{path}.{field}");
        }
    }

    private static bool HasNumber(JsonElement value, string field, out double number)
    {
        number = 0;
        return value.TryGetProperty(field, out var fieldValue)
            && fieldValue.ValueKind == JsonValueKind.Number
            && fieldValue.TryGetDouble(out number)
            && double.IsFinite(number)
            && number >= 0;
    }

    private static bool TryObject(JsonElement parent, string propertyName, out JsonElement value)
        => parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object;

    private static bool TryArray(JsonElement parent, string propertyName, out JsonElement value)
        => parent.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array;
}
