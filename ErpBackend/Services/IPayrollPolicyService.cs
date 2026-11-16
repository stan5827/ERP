using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IPayrollPolicyService
{
    Task<IReadOnlyList<PayrollPolicyVersionDto>> GetPoliciesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollPolicyVersionDto>> GetPendingFinanceReviewAsync(CancellationToken cancellationToken);
    Task<PayrollPolicyVersionDto?> GetPolicyAsync(Guid id, CancellationToken cancellationToken);
    Task<PayrollPolicyOperationResultDto> CreateDraftAsync(
        SavePayrollPolicyRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<PayrollPolicyOperationResultDto> UpdateDraftAsync(
        Guid id,
        SavePayrollPolicyRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<PayrollPolicyOperationResultDto> SubmitAsync(
        Guid id,
        Guid actorUserId,
        bool confirmedOfficialSources,
        CancellationToken cancellationToken);
    Task<PayrollPolicyOperationResultDto> ApproveAsync(
        Guid id,
        Guid actorUserId,
        bool confirmedOfficialSources,
        string? comment,
        CancellationToken cancellationToken);
    Task<PayrollPolicyOperationResultDto> RejectAsync(
        Guid id,
        Guid actorUserId,
        string comment,
        CancellationToken cancellationToken);
}
