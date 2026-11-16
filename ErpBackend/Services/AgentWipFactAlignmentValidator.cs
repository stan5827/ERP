using ErpBackend.DTOs;

namespace ErpBackend.Services;

public static class AgentWipFactAlignmentValidator
{
    public static bool TryValidateFacts(AgentWipWorkOrderDto facts, out string failureReason)
    {
        if (facts is null || facts.AuthorizedOperations is null || facts.ProductionUnit is null)
        {
            failureReason = "numeric_fact_packet_invalid";
            return false;
        }

        var numericFacts = new Dictionary<string, NumericFact>(StringComparer.Ordinal);

        foreach (var fact in EnumerateNumericFacts(facts))
        {
            if (string.IsNullOrWhiteSpace(fact.FactId))
            {
                failureReason = "numeric_fact_id_missing";
                return false;
            }

            if (numericFacts.TryGetValue(fact.FactId, out var existing)
                && (existing.Value != fact.Value
                    || !string.Equals(existing.Unit, fact.Unit, StringComparison.Ordinal)))
            {
                failureReason = "numeric_fact_id_conflict";
                return false;
            }

            numericFacts[fact.FactId] = fact;
        }

        if (facts.FinalOperation is { } finalOperation)
        {
            var productionUnit = facts.ProductionUnit.Value;
            if (!facts.AuthorizedOperations.Any(operation =>
                    operation.OperationSequence.Value == finalOperation.OperationSequence.Value))
            {
                failureReason = "final_operation_not_authorized";
                return false;
            }

            if (string.IsNullOrWhiteSpace(productionUnit)
                || !HasUnit(finalOperation.PlannedQuantity, productionUnit)
                || !HasUnit(finalOperation.GoodQuantity, productionUnit)
                || !HasUnit(finalOperation.DefectQuantity, productionUnit)
                || !HasUnit(finalOperation.RemainingQuantity, productionUnit)
                || !HasUnit(finalOperation.DefectRatePercent, "%"))
            {
                failureReason = "numeric_fact_unit_mismatch";
                return false;
            }

            if (finalOperation.PlannedQuantity.Value < 0
                || finalOperation.GoodQuantity.Value < 0
                || finalOperation.DefectQuantity.Value < 0
                || finalOperation.RemainingQuantity.Value < 0)
            {
                failureReason = "numeric_fact_negative_quantity";
                return false;
            }

            var denominator = finalOperation.GoodQuantity.Value + finalOperation.DefectQuantity.Value;
            if (denominator == 0)
            {
                if (finalOperation.DefectRatePercent.Value is not null
                    || finalOperation.DefectRatePercent.NotApplicableReason != "zero_denominator")
                {
                    failureReason = "numeric_fact_defect_rate_invalid";
                    return false;
                }
            }
            else if (finalOperation.DefectRatePercent.Value
                    != finalOperation.DefectQuantity.Value / denominator * 100m
                || finalOperation.DefectRatePercent.NotApplicableReason is not null)
            {
                failureReason = "numeric_fact_defect_rate_invalid";
                return false;
            }

            var expectedRemaining = Math.Max(
                finalOperation.PlannedQuantity.Value - finalOperation.GoodQuantity.Value,
                0m);
            if (finalOperation.RemainingQuantity.Value != expectedRemaining)
            {
                failureReason = "numeric_fact_remaining_quantity_invalid";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    public static bool TryValidateAnswerClaims(
        AgentWipWorkOrderDto currentTurnFacts,
        string currentSessionId,
        string currentTurnId,
        AgentWipAnswerDraftDto answer,
        out string failureReason)
    {
        if (currentTurnFacts is null
            || answer is null
            || answer.NumericClaims is null
            || string.IsNullOrWhiteSpace(currentSessionId)
            || string.IsNullOrWhiteSpace(currentTurnId))
        {
            failureReason = "answer_context_invalid";
            return false;
        }

        if (!string.Equals(answer.SessionId, currentSessionId, StringComparison.Ordinal)
            || !string.Equals(answer.TurnId, currentTurnId, StringComparison.Ordinal))
        {
            failureReason = "answer_context_mismatch";
            return false;
        }

        if (!TryValidateFacts(currentTurnFacts, out failureReason))
            return false;

        var authorizedFacts = EnumerateNumericFacts(currentTurnFacts)
            .GroupBy(fact => fact.FactId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var claim in answer.NumericClaims)
        {
            if (string.IsNullOrWhiteSpace(claim.FactId)
                || !authorizedFacts.TryGetValue(claim.FactId, out var fact)
                || fact.Value is null
                || fact.Value.Value != claim.Value
                || !string.Equals(fact.Unit, claim.Unit, StringComparison.Ordinal))
            {
                failureReason = "answer_numeric_claim_mismatch";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private static IEnumerable<NumericFact> EnumerateNumericFacts(AgentWipWorkOrderDto facts)
    {
        foreach (var operation in facts.AuthorizedOperations)
            yield return new NumericFact(
                operation.OperationSequence.FactId,
                operation.OperationSequence.Value,
                operation.OperationSequence.Unit);

        if (facts.FinalOperation is not { } finalOperation)
            yield break;

        yield return new NumericFact(
            finalOperation.OperationSequence.FactId,
            finalOperation.OperationSequence.Value,
            finalOperation.OperationSequence.Unit);
        yield return new NumericFact(
            finalOperation.PlannedQuantity.FactId,
            finalOperation.PlannedQuantity.Value,
            finalOperation.PlannedQuantity.Unit);
        yield return new NumericFact(
            finalOperation.GoodQuantity.FactId,
            finalOperation.GoodQuantity.Value,
            finalOperation.GoodQuantity.Unit);
        yield return new NumericFact(
            finalOperation.DefectQuantity.FactId,
            finalOperation.DefectQuantity.Value,
            finalOperation.DefectQuantity.Unit);

        yield return new NumericFact(
            finalOperation.DefectRatePercent.FactId,
            finalOperation.DefectRatePercent.Value,
            finalOperation.DefectRatePercent.Unit);

        yield return new NumericFact(
            finalOperation.RemainingQuantity.FactId,
            finalOperation.RemainingQuantity.Value,
            finalOperation.RemainingQuantity.Unit);
    }

    private static bool HasUnit(AgentWipDecimalFactDto fact, string expectedUnit)
        => string.Equals(fact.Unit, expectedUnit, StringComparison.Ordinal);

    private static bool HasUnit(AgentWipNullableDecimalFactDto fact, string expectedUnit)
        => string.Equals(fact.Unit, expectedUnit, StringComparison.Ordinal);

    private sealed record NumericFact(string FactId, decimal? Value, string? Unit);
}
