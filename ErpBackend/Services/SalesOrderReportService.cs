using Dapper;
using ErpBackend.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ErpBackend.Services;

public interface ISalesOrderReportService
{
    Task<IEnumerable<dynamic>> GetOrderSummaryAsync(DateTime? startDate, DateTime? endDate);
}

public class SalesOrderReportService : ISalesOrderReportService
{
    private readonly ErpGeneralDbContext _db;

    public SalesOrderReportService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<dynamic>> GetOrderSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        using var connection = _db.Database.GetDbConnection();
        
        // Use raw SQL with Dapper for high performance multi-table join
        string sql = @"
            SELECT 
                so.order_number as OrderNumber,
                c.customer_name as CustomerName,
                so.order_date as OrderDate,
                so.total_amount as TotalAmount,
                so.status as Status,
                (SELECT COUNT(*) FROM sal_order_items WHERE sales_order_id = so.id) as ItemCount
            FROM sal_orders so
            JOIN sal_customers c ON so.customer_id = c.id
            WHERE (@StartDate IS NULL OR so.order_date >= @StartDate)
              AND (@EndDate IS NULL OR so.order_date <= @EndDate)
            ORDER BY so.order_date DESC";

        return await connection.QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
    }
}
