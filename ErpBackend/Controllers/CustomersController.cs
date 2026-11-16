using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class CustomersController : BaseController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetCustomers(
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var (items, totalCount) = await _customerService.GetCustomersAsync(
            isActive,
            page,
            pageSize,
            searchTerm,
            cancellationToken);

        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        return customer is null
            ? NotFound(ApiHelper.Fail<CustomerDto>("找不到客戶資料"))
            : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer(
        [FromBody] CreateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var (customer, duplicateCode) = await _customerService.CreateCustomerAsync(request, cancellationToken);
        if (duplicateCode)
            return Conflict(ApiHelper.Fail<CustomerDto>("客戶代碼已存在"));

        return Ok(customer!, "客戶資料已建立");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var (customer, notFound, duplicateCode) =
            await _customerService.UpdateCustomerAsync(id, request, cancellationToken);

        if (notFound)
            return NotFound(ApiHelper.Fail<CustomerDto>("找不到客戶資料"));
        if (duplicateCode)
            return Conflict(ApiHelper.Fail<CustomerDto>("客戶代碼已存在"));

        return Ok(customer!, "客戶資料已更新");
    }
}
