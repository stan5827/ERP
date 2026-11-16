using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;

namespace ErpBackend.Services;

public interface ICustomerService
{
    Task<(List<CustomerDto> Items, int TotalCount)> GetCustomersAsync(
        bool? isActive,
        int page,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(CustomerDto? Customer, bool DuplicateCode)> CreateCustomerAsync(
        CreateCustomerRequestDto request,
        CancellationToken cancellationToken = default);
    Task<(CustomerDto? Customer, bool NotFound, bool DuplicateCode)> UpdateCustomerAsync(
        Guid id,
        UpdateCustomerRequestDto request,
        CancellationToken cancellationToken = default);
}

public class CustomerService : ICustomerService
{
    private readonly ErpGeneralDbContext _db;

    public CustomerService(ErpGeneralDbContext db) => _db = db;

    public async Task<(List<CustomerDto> Items, int TotalCount)> GetCustomersAsync(
        bool? isActive,
        int page,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(c => c.CustomerCode.Contains(searchTerm) || c.CustomerName.Contains(searchTerm));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.CustomerCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapCustomer).ToList();

        return (dtos, totalCount);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null ? null : MapCustomer(customer);
    }

    public async Task<(CustomerDto? Customer, bool DuplicateCode)> CreateCustomerAsync(
        CreateCustomerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var customerCode = request.CustomerCode.Trim();
        if (await _db.Customers.AnyAsync(c => c.CustomerCode == customerCode, cancellationToken))
            return (null, true);

        var customer = new Customer
        {
            CustomerCode = customerCode,
            CustomerName = request.CustomerName.Trim(),
            TaxId = NormalizeOptional(request.TaxId),
            ContactPerson = NormalizeOptional(request.ContactPerson),
            Phone = NormalizeOptional(request.Phone),
            Email = NormalizeOptional(request.Email),
            InvoiceAddress = NormalizeOptional(request.InvoiceAddress),
            Address = NormalizeOptional(request.DeliveryAddress),
            PaymentTerms = NormalizeOptional(request.PaymentTerms),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TWD" : request.Currency.Trim(),
            CreditLimit = request.CreditLimit,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return (MapCustomer(customer), false);
    }

    public async Task<(CustomerDto? Customer, bool NotFound, bool DuplicateCode)> UpdateCustomerAsync(
        Guid id,
        UpdateCustomerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
            return (null, true, false);

        var customerCode = request.CustomerCode.Trim();
        if (await _db.Customers.AnyAsync(
                c => c.Id != id && c.CustomerCode == customerCode,
                cancellationToken))
        {
            return (null, false, true);
        }

        customer.CustomerCode = customerCode;
        customer.CustomerName = request.CustomerName.Trim();
        customer.TaxId = NormalizeOptional(request.TaxId);
        customer.ContactPerson = NormalizeOptional(request.ContactPerson);
        customer.Phone = NormalizeOptional(request.Phone);
        customer.Email = NormalizeOptional(request.Email);
        customer.InvoiceAddress = NormalizeOptional(request.InvoiceAddress);
        customer.Address = NormalizeOptional(request.DeliveryAddress);
        customer.PaymentTerms = NormalizeOptional(request.PaymentTerms);
        customer.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TWD" : request.Currency.Trim();
        customer.CreditLimit = request.CreditLimit;
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (MapCustomer(customer), false, false);
    }

    private static CustomerDto MapCustomer(Customer customer) => new()
    {
        Id = customer.Id,
        CustomerCode = customer.CustomerCode,
        CustomerName = customer.CustomerName,
        TaxId = customer.TaxId,
        ContactPerson = customer.ContactPerson,
        Phone = customer.Phone,
        Email = customer.Email,
        InvoiceAddress = customer.InvoiceAddress,
        DeliveryAddress = customer.Address,
        PaymentTerms = customer.PaymentTerms,
        Currency = customer.Currency,
        CreditLimit = customer.CreditLimit,
        IsActive = customer.IsActive,
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
