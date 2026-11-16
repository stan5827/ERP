using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ErpBackend.Tests.Services;

public class OrderToCashWorkflowTests
{
    [Fact]
    public async Task DeliveryInvoiceAndReceipt_PostInventoryAndBalancedVouchers()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-001", CustomerName = "Customer" };
        var sku = new Sku
        {
            SkuNumber = "FG-001",
            SkuName = "Finished product",
            StandardCost = 4,
            Qty = 10
        };
        var order = new SalesOrder
        {
            OrderNumber = "SO-001",
            CustomerId = customer.Id,
            Customer = customer,
            OrderDate = DateTime.UtcNow
        };
        order.Items.Add(new SalesOrderItem
        {
            SalesOrderId = order.Id,
            SalesOrder = order,
            ProductId = sku.Id,
            SkuId = sku.Id,
            Quantity = 10,
            UnitPrice = 10
        });
        var warehouse = new Warehouse { WarehouseCode = "FG", WarehouseName = "Finished goods" };
        var bin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationCode = "A-01"
        };
        var cogsAccount = new GLAccount
        {
            AccountCode = "5101",
            AccountName = "COGS",
            AccountType = "Expense"
        };
        var inventoryAccount = new GLAccount
        {
            AccountCode = "1205",
            AccountName = "Finished goods",
            AccountType = "Asset"
        };

        db.Customers.Add(customer);
        db.Skus.Add(sku);
        db.SalesOrders.Add(order);
        db.Warehouses.Add(warehouse);
        db.BinLocations.Add(bin);
        db.GLAccounts.AddRange(cogsAccount, inventoryAccount);
        db.GLAccountMappings.AddRange(
            new GLAccountMapping
            {
                MappingKey = "sales_cogs",
                GLAccount = cogsAccount
            },
            new GLAccountMapping
            {
                MappingKey = "finished_goods_inventory",
                GLAccount = inventoryAccount
            });
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            Sku = sku,
            LotNumber = "LOT-001",
            Qty = 10,
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id
        });
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-001",
            SkuId = sku.Id,
            Type = "receipt",
            QtyChange = 10,
            QtyBefore = 0,
            QtyAfter = 10,
            LotNumber = "LOT-001",
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id,
            UnitCost = 3.25m,
            TotalCost = 32.50m
        });
        await db.SaveChangesAsync();

        var accounting = new AccountingService(db);
        var deliveryService = new DeliveryOrderService(db, accounting);
        var (delivery, deliveryStatus) = await deliveryService.CreateDeliveryOrderAsync(
            new CreateDeliveryOrderRequest
            {
                SalesOrderId = order.Id,
                Items =
                [
                    new CreateDeliveryOrderItemRequest
                    {
                        SkuId = sku.Id,
                        Quantity = 6,
                        WarehouseId = warehouse.Id,
                        BinLocationId = bin.Id,
                        LotNumber = "LOT-001"
                    }
                ]
            });

        Assert.Equal("ok", deliveryStatus);
        Assert.NotNull(delivery);
        Assert.Equal(10, (await db.Skus.SingleAsync()).Qty);

        var (invoiceBeforeDelivery, invoiceStatusBeforeDelivery) =
            await CreateInvoiceService(db).CreateInvoiceAsync(CreateInvoiceRequest(
                customer.Id, delivery!.Id, sku.Id, 1));
        Assert.Null(invoiceBeforeDelivery);
        Assert.Equal("delivery_not_completed_or_customer_mismatch", invoiceStatusBeforeDelivery);

        var (signed, signStatus) = await deliveryService.SignDeliveryAsync(
            delivery.Id, new SignDeliveryRequest { SignedBy = "Operator" });
        Assert.True(signed);
        Assert.Equal("ok", signStatus);
        Assert.Equal(4, (await db.Skus.SingleAsync()).Qty);
        Assert.Equal(4, (await db.InventoryLots.SingleAsync()).Qty);
        Assert.Equal(6, (await db.SalesOrderItems.SingleAsync()).DeliveredQty);
        Assert.Equal(-6, (await db.InventoryTransactions
            .SingleAsync(transaction => transaction.RelatedDocId == delivery.Id)).QtyChange);
        var deliveryTransaction = await db.InventoryTransactions
            .SingleAsync(transaction => transaction.RelatedDocId == delivery.Id);
        Assert.Equal(3.25m, deliveryTransaction.UnitCost);
        Assert.Equal(19.50m, deliveryTransaction.TotalCost);
        var cogsVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher =>
                voucher.ReferenceType == "DeliveryOrder" && voucher.ReferenceId == delivery.Id);
        Assert.Equal(19.50m, cogsVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(19.50m, cogsVoucher.Entries.Sum(entry => entry.CreditAmount));

        var invoiceService = CreateInvoiceService(db);
        var (invoice, invoiceStatus) = await invoiceService.CreateInvoiceAsync(
            CreateInvoiceRequest(customer.Id, delivery.Id, sku.Id, 6));
        Assert.Equal("ok", invoiceStatus);
        Assert.NotNull(invoice);

        var (duplicateInvoice, duplicateStatus) = await invoiceService.CreateInvoiceAsync(
            CreateInvoiceRequest(customer.Id, delivery.Id, sku.Id, 1));
        Assert.Null(duplicateInvoice);
        Assert.Equal("invoice_quantity_exceeds_uninvoiced_delivery", duplicateStatus);

        await accounting.SeedDefaultAccountsAsync();
        db.GLAccountMappings.AddRange(
            new GLAccountMapping
            {
                MappingKey = "accounts_receivable",
                GLAccount = await db.GLAccounts.SingleAsync(account => account.AccountCode == "1131")
            },
            new GLAccountMapping
            {
                MappingKey = "sales_revenue",
                GLAccount = await db.GLAccounts.SingleAsync(account => account.AccountCode == "4101")
            },
            new GLAccountMapping
            {
                MappingKey = "output_tax",
                GLAccount = await db.GLAccounts.SingleAsync(account => account.AccountCode == "2201")
            },
            new GLAccountMapping
            {
                MappingKey = "customer_advances",
                GLAccount = await db.GLAccounts.SingleAsync(account => account.AccountCode == "2101")
            });
        await db.SaveChangesAsync();
        var (found, approvalStatus) = await invoiceService.ApproveInvoiceAsync(
            invoice!.Id, new ApproveInvoiceRequest { ApprovedBy = Guid.NewGuid() });
        Assert.True(found);
        Assert.Equal("ok", approvalStatus);
        Assert.Contains(await db.AccountingVouchers.ToListAsync(), voucher =>
            voucher.ReferenceType == "SalesInvoice" && voucher.ReferenceId == invoice.Id);
        var invoiceVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "SalesInvoice");
        Assert.Contains(invoiceVoucher.Entries, entry =>
            db.GLAccounts.Single(account => account.Id == entry.GLAccountId).AccountCode == "2201");
        Assert.Equal(invoice.TotalAmount, invoiceVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(invoice.TotalAmount, invoiceVoucher.Entries.Sum(entry => entry.CreditAmount));

        var cashAccount = await db.GLAccounts.SingleAsync(account => account.AccountCode == "1103");
        var bankAccount = new BankAccount
        {
            BankName = "Settlement account",
            AccountNumber = "0001",
            GLAccountId = cashAccount.Id
        };
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync();

        var cashReceiptService = new CashReceiptService(db, accounting);
        var (invalidReceipt, invalidReceiptStatus) = await cashReceiptService.CreateAsync(new CreateCashReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = "wire",
            ReceivedAmount = 62,
            BankAccountId = bankAccount.Id,
            Allocations =
            [
                new CreateCashReceiptAllocationRequest
                {
                    SalesInvoiceId = invoice.Id,
                    AllocatedAmount = 63
                }
            ]
        });
        Assert.Null(invalidReceipt);
        Assert.Equal("receipt_amount_mismatch", invalidReceiptStatus);
        Assert.Equal(0, (await db.SalesInvoices.SingleAsync()).PaidAmount);

        var (receipt, receiptStatus) = await cashReceiptService.CreateAsync(new CreateCashReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = "wire",
            ReceivedAmount = 63,
            BankAccountId = bankAccount.Id,
            Allocations =
            [
                new CreateCashReceiptAllocationRequest
                {
                    SalesInvoiceId = invoice.Id,
                    AllocatedAmount = 63
                }
            ]
        });

        Assert.NotNull(receipt);
        Assert.Equal("paid", receiptStatus);
        Assert.Equal("paid", (await db.SalesInvoices.SingleAsync()).Status);
        Assert.Equal(63, (await db.BankAccounts.SingleAsync()).CurrentBalance);
        Assert.Contains(await db.AccountingVouchers.ToListAsync(), voucher =>
            voucher.ReferenceType == "CashReceipt" && voucher.ReferenceId == receipt!.Id);

        var receiptVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "CashReceipt");
        Assert.Equal(63, receiptVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(63, receiptVoucher.Entries.Sum(entry => entry.CreditAmount));
    }

    [Fact]
    public async Task CashReceipt_StoresUnappliedBalanceAsCustomerAdvance()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-ADV-001", CustomerName = "Advance customer" };
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-ADV-001",
            CustomerId = customer.Id,
            Customer = customer,
            TotalAmount = 100,
            Status = "approved"
        };
        var cashGl = new GLAccount
        {
            AccountCode = "1103",
            AccountName = "Cash",
            AccountType = "Asset",
            IsCashAccount = true
        };
        var advanceGl = new GLAccount
        {
            AccountCode = "2101",
            AccountName = "Customer advances",
            AccountType = "Liability"
        };
        var arGl = new GLAccount
        {
            AccountCode = "1131",
            AccountName = "Accounts receivable",
            AccountType = "Asset"
        };
        var bankAccount = new BankAccount
        {
            BankName = "Settlement account",
            AccountNumber = "ADV-001",
            GLAccount = cashGl
        };
        db.Customers.Add(customer);
        db.SalesInvoices.Add(invoice);
        db.GLAccounts.AddRange(cashGl, advanceGl, arGl);
        db.GLAccountMappings.Add(new GLAccountMapping
        {
            MappingKey = "accounts_receivable",
            GLAccount = arGl
        });
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync();

        var service = new CashReceiptService(db, new AccountingService(db));
        var request = new CreateCashReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = "wire",
            ReceivedAmount = 125,
            BankAccountId = bankAccount.Id
        };

        var (blockedReceipt, blockedStatus) = await service.CreateAsync(request);
        Assert.Null(blockedReceipt);
        Assert.Equal("unapplied_account_mapping_required", blockedStatus);
        Assert.Empty(await db.CashReceipts.ToListAsync());
        Assert.Equal(0, (await db.BankAccounts.SingleAsync()).CurrentBalance);

        db.GLAccountMappings.Add(new GLAccountMapping
        {
            MappingKey = "customer_advances",
            GLAccount = advanceGl
        });
        await db.SaveChangesAsync();

        var (receipt, status) = await service.CreateAsync(request);
        Assert.NotNull(receipt);
        Assert.Equal("unapplied", status);
        Assert.Equal(125, receipt!.UnappliedAmount);
        Assert.Equal(125, (await db.BankAccounts.SingleAsync()).CurrentBalance);

        var voucher = await db.AccountingVouchers
            .Include(item => item.Entries)
            .SingleAsync(item => item.ReferenceType == "CashReceipt");
        Assert.Equal(125, voucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(125, voucher.Entries.Sum(entry => entry.CreditAmount));
        Assert.Contains(voucher.Entries, entry =>
            entry.GLAccountId == advanceGl.Id && entry.CreditAmount == 125);

        var (overApplied, overAppliedStatus) = await service.ApplyUnappliedAsync(
            receipt.Id,
            new ApplyCashReceiptUnappliedRequestDto { SalesInvoiceId = invoice.Id, Amount = 126 });
        Assert.Null(overApplied);
        Assert.Equal("over_unapplied", overAppliedStatus);

        var (appliedReceipt, applyStatus) = await service.ApplyUnappliedAsync(
            receipt.Id,
            new ApplyCashReceiptUnappliedRequestDto { SalesInvoiceId = invoice.Id, Amount = 100 });
        Assert.NotNull(appliedReceipt);
        Assert.Equal("paid", applyStatus);
        Assert.Equal(25, appliedReceipt!.UnappliedAmount);
        Assert.Equal(125, (await db.BankAccounts.SingleAsync()).CurrentBalance);
        Assert.Equal(100, (await db.SalesInvoices.SingleAsync()).PaidAmount);

        var applicationVoucher = await db.AccountingVouchers
            .Include(item => item.Entries)
            .SingleAsync(item => item.ReferenceType == "CashReceiptApplication");
        Assert.Equal(100, applicationVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(100, applicationVoucher.Entries.Sum(entry => entry.CreditAmount));
    }

    [Fact]
    public async Task CashReceipt_AllowsPartialPaymentAndKeepsInvoiceOutstanding()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-PART-001", CustomerName = "Partial payment customer" };
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-PART-001",
            CustomerId = customer.Id,
            Customer = customer,
            TotalAmount = 150,
            Status = "approved"
        };
        var cashGl = new GLAccount
        {
            AccountCode = "1103",
            AccountName = "Cash",
            AccountType = "Asset",
            IsCashAccount = true
        };
        var arGl = new GLAccount
        {
            AccountCode = "1131",
            AccountName = "Accounts receivable",
            AccountType = "Asset"
        };
        var bankAccount = new BankAccount
        {
            BankName = "Settlement account",
            AccountNumber = "PART-001",
            GLAccount = cashGl
        };
        db.Customers.Add(customer);
        db.SalesInvoices.Add(invoice);
        db.GLAccounts.AddRange(cashGl, arGl);
        db.GLAccountMappings.Add(new GLAccountMapping
        {
            MappingKey = "accounts_receivable",
            GLAccount = arGl
        });
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync();

        var service = new CashReceiptService(db, new AccountingService(db));
        var (receipt, status) = await service.CreateAsync(new CreateCashReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = "wire",
            ReceivedAmount = 75,
            BankAccountId = bankAccount.Id,
            Allocations =
            [
                new CreateCashReceiptAllocationRequest
                {
                    SalesInvoiceId = invoice.Id,
                    AllocatedAmount = 75
                }
            ]
        });

        Assert.NotNull(receipt);
        Assert.Equal("ok", status);
        var updatedInvoice = await db.SalesInvoices.SingleAsync();
        Assert.Equal(75, updatedInvoice.PaidAmount);
        Assert.Equal(0, updatedInvoice.DiscountedAmount);
        Assert.Equal("approved", updatedInvoice.Status);
        var pendingInvoice = await service.GetPendingInvoicesAsync(customer.Id);
        Assert.Equal(75, Assert.Single(pendingInvoice).Outstanding);
    }

    [Fact]
    public async Task CashReceipt_BlocksDiscountUntilTaxPolicyIsConfirmed()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-DIS-001", CustomerName = "Discount customer" };
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-DIS-001",
            CustomerId = customer.Id,
            Customer = customer,
            TotalAmount = 100,
            Status = "approved"
        };
        var cashGl = new GLAccount
        {
            AccountCode = "1103",
            AccountName = "Cash",
            AccountType = "Asset",
            IsCashAccount = true
        };
        var arGl = new GLAccount
        {
            AccountCode = "1131",
            AccountName = "Accounts receivable",
            AccountType = "Asset"
        };
        var bankAccount = new BankAccount
        {
            BankName = "Settlement account",
            AccountNumber = "DIS-001",
            GLAccount = cashGl
        };
        db.Customers.Add(customer);
        db.SalesInvoices.Add(invoice);
        db.GLAccounts.AddRange(cashGl, arGl);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync();

        var service = new CashReceiptService(db, new AccountingService(db));
        var request = new CreateCashReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = "wire",
            ReceivedAmount = 90,
            DiscountAmount = 10,
            BankAccountId = bankAccount.Id,
            Allocations =
            [
                new CreateCashReceiptAllocationRequest
                {
                    SalesInvoiceId = invoice.Id,
                    AllocatedAmount = 90,
                    DiscountAmount = 10
                }
            ]
        };

        var (blockedReceipt, blockedStatus) = await service.CreateAsync(request);
        Assert.Null(blockedReceipt);
        Assert.Equal("discount_policy_pending", blockedStatus);
        Assert.Empty(await db.CashReceipts.ToListAsync());
        var settledInvoice = await db.SalesInvoices.SingleAsync();
        Assert.Equal(0, settledInvoice.PaidAmount);
        Assert.Equal(0, settledInvoice.DiscountedAmount);
        Assert.Equal("approved", settledInvoice.Status);
        Assert.Equal(0, (await db.BankAccounts.SingleAsync()).CurrentBalance);
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    [Fact]
    public async Task SignDeliveryAsync_RejectsLotWithoutRecordedCostWithoutChangingStock()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-COST-001", CustomerName = "Cost customer" };
        var sku = new Sku
        {
            SkuNumber = "FG-COST-001",
            SkuName = "Cost tracked product",
            StandardCost = 9,
            Qty = 1
        };
        var order = new SalesOrder
        {
            OrderNumber = "SO-COST-001",
            CustomerId = customer.Id,
            Customer = customer
        };
        order.Items.Add(new SalesOrderItem
        {
            SalesOrderId = order.Id,
            SalesOrder = order,
            ProductId = sku.Id,
            SkuId = sku.Id,
            Quantity = 1,
            UnitPrice = 10
        });
        var warehouse = new Warehouse { WarehouseCode = "FG-COST", WarehouseName = "Finished goods" };
        var bin = new BinLocation { WarehouseId = warehouse.Id, Warehouse = warehouse, LocationCode = "C-01" };
        db.Customers.Add(customer);
        db.Skus.Add(sku);
        db.SalesOrders.Add(order);
        db.Warehouses.Add(warehouse);
        db.BinLocations.Add(bin);
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            Sku = sku,
            LotNumber = "LOT-NO-COST",
            Qty = 1,
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id
        });
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-NO-COST",
            SkuId = sku.Id,
            Type = "receipt",
            QtyChange = 1,
            QtyBefore = 0,
            QtyAfter = 1,
            LotNumber = "LOT-NO-COST",
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id
        });
        await db.SaveChangesAsync();

        var deliveryService = new DeliveryOrderService(db, new AccountingService(db));
        var (delivery, createStatus) = await deliveryService.CreateDeliveryOrderAsync(new CreateDeliveryOrderRequest
        {
            SalesOrderId = order.Id,
            Items =
            [
                new CreateDeliveryOrderItemRequest
                {
                    SkuId = sku.Id,
                    Quantity = 1,
                    WarehouseId = warehouse.Id,
                    BinLocationId = bin.Id,
                    LotNumber = "LOT-NO-COST"
                }
            ]
        });
        Assert.Equal("ok", createStatus);
        Assert.NotNull(delivery);

        var (found, signStatus) = await deliveryService.SignDeliveryAsync(
            delivery!.Id, new SignDeliveryRequest { SignedBy = "Operator" });

        Assert.True(found);
        Assert.Equal("lot_cost_unavailable", signStatus);
        Assert.Equal("pending", (await db.DeliveryOrders.SingleAsync()).Status);
        Assert.Equal(1m, (await db.Skus.SingleAsync()).Qty);
        Assert.Equal(1m, (await db.InventoryLots.SingleAsync()).Qty);
        Assert.DoesNotContain(await db.InventoryTransactions.ToListAsync(),
            transaction => transaction.Type == "sales_delivery");
    }

    [Fact]
    public async Task SignDeliveryAsync_RejectsUnconfiguredCogsAccountsBeforeChangingStock()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var customer = new Customer { CustomerCode = "C-MAP-001", CustomerName = "Mapping customer" };
        var sku = new Sku
        {
            SkuNumber = "FG-MAP-001",
            SkuName = "Mapped cost product",
            StandardCost = 9,
            Qty = 1
        };
        var order = new SalesOrder
        {
            OrderNumber = "SO-MAP-001",
            CustomerId = customer.Id,
            Customer = customer
        };
        order.Items.Add(new SalesOrderItem
        {
            SalesOrderId = order.Id,
            SalesOrder = order,
            ProductId = sku.Id,
            SkuId = sku.Id,
            Quantity = 1,
            UnitPrice = 10
        });
        var warehouse = new Warehouse { WarehouseCode = "FG-MAP", WarehouseName = "Finished goods" };
        var bin = new BinLocation { WarehouseId = warehouse.Id, Warehouse = warehouse, LocationCode = "M-01" };
        db.Customers.Add(customer);
        db.Skus.Add(sku);
        db.SalesOrders.Add(order);
        db.Warehouses.Add(warehouse);
        db.BinLocations.Add(bin);
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            Sku = sku,
            LotNumber = "LOT-MAP-001",
            Qty = 1,
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id
        });
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-MAP-001",
            SkuId = sku.Id,
            Type = "receipt",
            QtyChange = 1,
            QtyBefore = 0,
            QtyAfter = 1,
            LotNumber = "LOT-MAP-001",
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id,
            UnitCost = 3,
            TotalCost = 3
        });
        await db.SaveChangesAsync();

        var deliveryService = new DeliveryOrderService(db, new AccountingService(db));
        var (delivery, createStatus) = await deliveryService.CreateDeliveryOrderAsync(new CreateDeliveryOrderRequest
        {
            SalesOrderId = order.Id,
            Items =
            [
                new CreateDeliveryOrderItemRequest
                {
                    SkuId = sku.Id,
                    Quantity = 1,
                    WarehouseId = warehouse.Id,
                    BinLocationId = bin.Id,
                    LotNumber = "LOT-MAP-001"
                }
            ]
        });
        Assert.Equal("ok", createStatus);
        Assert.NotNull(delivery);

        var (found, signStatus) = await deliveryService.SignDeliveryAsync(
            delivery!.Id, new SignDeliveryRequest { SignedBy = "Operator" });

        Assert.True(found);
        Assert.Equal("account_mapping_missing:sales_cogs", signStatus);
        Assert.Equal("pending", (await db.DeliveryOrders.SingleAsync()).Status);
        Assert.Equal(1m, (await db.Skus.SingleAsync()).Qty);
        Assert.Equal(1m, (await db.InventoryLots.SingleAsync()).Qty);
        Assert.DoesNotContain(await db.InventoryTransactions.ToListAsync(),
            transaction => transaction.Type == "sales_delivery");
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    [Fact]
    public async Task FinanceService_OnlyAcceptsActiveAccountsOfTheRequiredTypeForMappings()
    {
        await using var db = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var cogsAccount = new GLAccount
        {
            AccountCode = "5101",
            AccountName = "COGS",
            AccountType = "Expense"
        };
        var inventoryAccount = new GLAccount
        {
            AccountCode = "1205",
            AccountName = "Finished goods",
            AccountType = "Asset"
        };
        db.GLAccounts.AddRange(cogsAccount, inventoryAccount);
        await db.SaveChangesAsync();

        var financeService = new FinanceService(db);
        var initialMappings = await financeService.GetAccountingMappingsAsync();
        Assert.Equal(18, initialMappings.Count);
        Assert.All(initialMappings, mapping => Assert.False(mapping.IsConfigured));

        var (mapped, mapStatus) = await financeService.SetAccountingMappingAsync(
            "sales_cogs", cogsAccount.Id);
        var (wrongTypeMapped, wrongTypeStatus) = await financeService.SetAccountingMappingAsync(
            "finished_goods_inventory", cogsAccount.Id);
        var (unknownMapped, unknownStatus) = await financeService.SetAccountingMappingAsync(
            "unknown_mapping", inventoryAccount.Id);

        Assert.True(mapped);
        Assert.Equal("ok", mapStatus);
        Assert.False(wrongTypeMapped);
        Assert.Equal("invalid_account", wrongTypeStatus);
        Assert.False(unknownMapped);
        Assert.Equal("unknown_mapping", unknownStatus);
        Assert.Contains(await financeService.GetAccountingMappingsAsync(), mapping =>
            mapping.MappingKey == "sales_cogs" &&
            mapping.GLAccountId == cogsAccount.Id &&
            mapping.IsConfigured);
    }

    private static InvoiceService CreateInvoiceService(ErpGeneralDbContext db) =>
        new(db, new AccountingService(db));

    private static CreateInvoiceRequest CreateInvoiceRequest(
        Guid customerId,
        Guid deliveryOrderId,
        Guid skuId,
        decimal quantity) =>
        new()
        {
            CustomerId = customerId,
            DeliveryOrderId = deliveryOrderId,
            TaxType = "taxable_5pct",
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    SkuId = skuId,
                    Quantity = quantity,
                    UnitPrice = 10
                }
            ]
        };
}
