using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class CustomerServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CustomerService(db);

        var (items, totalCount) = await service.GetCustomersAsync(null, 1, 50, null);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsAllOrdered_WhenNoFilter()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.AddRange(
            new Customer { CustomerCode = "C002", CustomerName = "Beta", IsActive = true },
            new Customer { CustomerCode = "C001", CustomerName = "Alpha", IsActive = true });
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var (items, totalCount) = await service.GetCustomersAsync(null, 1, 50, null);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.Equal("C001", items[0].CustomerCode);
        Assert.Equal("C002", items[1].CustomerCode);
    }

    [Fact]
    public async Task GetCustomersAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.AddRange(
            new Customer { CustomerCode = "C001", CustomerName = "Alpha", IsActive = true },
            new Customer { CustomerCode = "C002", CustomerName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var (items, totalCount) = await service.GetCustomersAsync(true, 1, 50, null);

        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("C001", items[0].CustomerCode);
    }

    [Fact]
    public async Task GetCustomersAsync_FiltersBySearchTerm()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.AddRange(
            new Customer { CustomerCode = "C001", CustomerName = "Alpha Corp", IsActive = true },
            new Customer { CustomerCode = "C002", CustomerName = "Beta Corp", IsActive = true });
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var (items, totalCount) = await service.GetCustomersAsync(null, 1, 50, "Alpha");

        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("C001", items[0].CustomerCode);
    }

    [Fact]
    public async Task GetCustomersAsync_PaginatesResults()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.AddRange(
            new Customer { CustomerCode = "C001", CustomerName = "Alpha", IsActive = true },
            new Customer { CustomerCode = "C002", CustomerName = "Beta", IsActive = true },
            new Customer { CustomerCode = "C003", CustomerName = "Gamma", IsActive = true });
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var (items, totalCount) = await service.GetCustomersAsync(null, 1, 2, null);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetCustomersAsync_MapsAllDtoFields()
    {
        using var db = CreateInMemoryDbContext();
        var id = Guid.NewGuid();
        db.Customers.Add(new Customer
        {
            Id = id,
            CustomerCode = "C-TEST",
            CustomerName = "TestCo",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var (items, _) = await service.GetCustomersAsync(null, 1, 50, null);

        var dto = items[0];
        Assert.Equal(id, dto.Id);
        Assert.Equal("C-TEST", dto.CustomerCode);
        Assert.Equal("TestCo", dto.CustomerName);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task CreateCustomerAsync_SavesAndReturnsCustomerDetails()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CustomerService(db);

        var (customer, duplicateCode) = await service.CreateCustomerAsync(new CreateCustomerRequestDto
        {
            CustomerCode = " C-NEW ",
            CustomerName = " New Customer ",
            ContactPerson = "Contact",
            Phone = "02-12345678",
            Email = "contact@example.com",
            InvoiceAddress = "Invoice address",
            DeliveryAddress = "Delivery address",
            PaymentTerms = "月結30天",
            Currency = "TWD",
            CreditLimit = 10000m,
        });

        Assert.False(duplicateCode);
        Assert.NotNull(customer);
        Assert.Equal("C-NEW", customer.CustomerCode);
        Assert.Equal("New Customer", customer.CustomerName);
        Assert.Equal("Contact", customer.ContactPerson);
        Assert.Equal("Delivery address", customer.DeliveryAddress);
        Assert.Equal(10000m, customer.CreditLimit);
        Assert.Single(db.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_ReturnsDuplicateCode_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.Customers.Add(new Customer { CustomerCode = "C-EXISTING", CustomerName = "Existing" });
        await db.SaveChangesAsync();
        var service = new CustomerService(db);

        var (customer, duplicateCode) = await service.CreateCustomerAsync(new CreateCustomerRequestDto
        {
            CustomerCode = "C-EXISTING",
            CustomerName = "Another customer",
        });

        Assert.Null(customer);
        Assert.True(duplicateCode);
        Assert.Single(db.Customers);
    }

    [Fact]
    public async Task UpdateCustomerAsync_UpdatesExistingCustomerAndReturnsNotFoundForMissingId()
    {
        using var db = CreateInMemoryDbContext();
        var existing = new Customer { CustomerCode = "C-OLD", CustomerName = "Old name" };
        db.Customers.Add(existing);
        await db.SaveChangesAsync();
        var service = new CustomerService(db);

        var (customer, notFound, duplicateCode) = await service.UpdateCustomerAsync(
            existing.Id,
            new UpdateCustomerRequestDto
            {
                CustomerCode = "C-NEW",
                CustomerName = "New name",
                DeliveryAddress = "Updated address",
            });

        Assert.False(notFound);
        Assert.False(duplicateCode);
        Assert.Equal("New name", customer?.CustomerName);
        Assert.Equal("Updated address", customer?.DeliveryAddress);

        var (missingCustomer, missing, _) = await service.UpdateCustomerAsync(
            Guid.NewGuid(),
            new UpdateCustomerRequestDto { CustomerCode = "C-MISSING", CustomerName = "Missing" });

        Assert.Null(missingCustomer);
        Assert.True(missing);
    }
}
