using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Procurement;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace ErpBackend.Services;

public class SupplierPortalService : ISupplierPortalService
{
    private readonly ErpGeneralDbContext _context;
    private readonly IConfiguration _configuration;

    public SupplierPortalService(ErpGeneralDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<SupplierPortalLoginResponse?> LoginAsync(SupplierPortalLoginRequest request)
    {
        var user = await _context.SupplierUsers
            .Include(u => u.Supplier)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        // For suppliers, we'll support both SHA256 and BCrypt for transition if needed, 
        // but here we check SHA256 as currently implemented in seed/portal models
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = new List<string> { "Supplier", user.Role };
        var token = GenerateSupplierJwtToken(user.Id, user.SupplierId, user.Username, roles);

        return new SupplierPortalLoginResponse
        {
            SupplierUserId = user.Id,
            SupplierId = user.SupplierId,
            SupplierName = user.Supplier.SupplierName,
            Username = user.Username,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "480")),
            Role = user.Role
        };
    }

    private string GenerateSupplierJwtToken(Guid userId, Guid supplierId, string username, List<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "ERP_SECRET_KEY_2026_CHANGE_ME";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("SupplierId", supplierId.ToString()),
            new("SupplierUserId", userId.ToString())
        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "480")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<IEnumerable<RfqDto>> GetActiveRfqsAsync(Guid supplierId)
    {
        var rfqs = await _context.Rfqs
            .Include(r => r.Items)
            .Where(r => r.Status == "open" && (r.Deadline == null || r.Deadline > DateTime.UtcNow))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rfqs.Select(r => new RfqDto
        {
            Id = r.Id,
            RfqNumber = r.RfqNumber,
            RfqDate = r.RfqDate,
            Deadline = r.Deadline,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ItemCount = r.Items.Count,
            Items = r.Items.Select(i => new RfqItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Specifications = i.Specifications
            }).ToList()
        });
    }

    public async Task<bool> SubmitQuoteAsync(Guid supplierUserId, Guid supplierId, SubmitSupplierQuoteRequest request)
    {
        var rfq = await _context.Rfqs.FindAsync(request.RfqId);
        if (rfq == null || rfq.Status != "open") return false;

        var existingQuote = await _context.SupplierQuotes
            .FirstOrDefaultAsync(q => q.RfqId == request.RfqId && q.SupplierId == supplierId);

        if (existingQuote != null)
        {
            existingQuote.QuotedPrice = request.QuotedPrice;
            existingQuote.LeadTimeDays = request.LeadTimeDays;
            existingQuote.Moq = request.Moq;
            existingQuote.Notes = request.Notes;
            existingQuote.SubmittedAt = DateTime.UtcNow;
        }
        else
        {
            var quote = new SupplierQuote
            {
                RfqId = request.RfqId,
                SupplierUserId = supplierUserId,
                SupplierId = supplierId,
                QuotedPrice = request.QuotedPrice,
                LeadTimeDays = request.LeadTimeDays,
                Moq = request.Moq,
                Notes = request.Notes
            };
            _context.SupplierQuotes.Add(quote);
            
            var response = new RfqSupplierResponse
            {
                RfqId = request.RfqId,
                SupplierId = supplierId,
                UnitPrice = request.QuotedPrice,
                LeadTimeDays = request.LeadTimeDays,
                Moq = request.Moq,
                Notes = request.Notes
            };
            _context.RfqSupplierResponses.Add(response);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetActivePurchaseOrdersAsync(Guid supplierId)
    {
        var pos = await _context.PurchaseOrders
            .Include(p => p.Items)
            .Where(p => p.SupplierId == supplierId && (p.Status == "open" || p.Status == "confirmed"))
            .OrderByDescending(p => p.PoDate)
            .ToListAsync();

        return pos.Select(p => new PurchaseOrderDto
        {
            Id = p.Id,
            PoNumber = p.PoNumber,
            PoDate = p.PoDate,
            TotalAmount = p.TotalAmount,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            Items = p.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ReceivedQty = i.ReceivedQty
            }).ToList()
        });
    }

    public async Task<bool> ConfirmPurchaseOrderAsync(Guid poId, Guid supplierId)
    {
        var po = await _context.PurchaseOrders.FindAsync(poId);
        if (po == null || po.SupplierId != supplierId || po.Status != "open")
            return false;

        po.Status = "confirmed";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SupplierAsnDto>> GetAsnsAsync(Guid supplierId)
    {
        var asns = await _context.Asns
            .Include(a => a.Items)
            .Include(a => a.PurchaseOrder)
            .Where(a => a.SupplierId == supplierId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return asns.Select(MapAsnDto);
    }

    public async Task<Guid> CreateAsnAsync(Guid supplierId, CreateSupplierAsnRequest request)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId && p.SupplierId == supplierId);

        if (purchaseOrder == null)
            throw new Exception("Purchase order not found or doesn't belong to this supplier.");

        var asn = new Asn
        {
            AsnNumber = string.IsNullOrWhiteSpace(request.AsnNumber)
                ? $"ASN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
                : request.AsnNumber.Trim(),
            SupplierId = supplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            ShipDate = DateTime.UtcNow,
            ExpectedArrival = request.ExpectedArrivalDate,
            Carrier = request.CarrierName,
            TrackingNumber = request.TrackingNumber,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new AsnItem
            {
                SkuId = i.SkuId,
                ShippedQty = i.ShippedQty,
                LotNumber = i.LotNumber,
                ManufactureDate = i.ManufactureDate,
                ExpiryDate = i.ExpiryDate
            }).ToList()
        };

        _context.Asns.Add(asn);
        await _context.SaveChangesAsync();
        return asn.Id;
    }

    public async Task<SupplierPerformanceReportDto?> GetPerformanceScoreAsync(Guid supplierId, string period)
    {
        // Simple mock performance score logic for now
        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null) return null;

        return new SupplierPerformanceReportDto
        {
            SupplierId = supplierId,
            Period = period,
            QualityScore = 95.0,
            DeliveryScore = 92.5,
            CostScore = 88.0,
            OverallScore = 91.8,
            Trend = "up"
        };
    }

    private bool VerifyPassword(string password, string hash)
    {
        // Check if it's a BCrypt hash
        if (hash.StartsWith("$2") || hash.Length == 60)
        {
            try { return BCrypt.Net.BCrypt.Verify(password, hash); } catch { }
        }

        // Fallback to SHA256 for legacy/seeded supplier users
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hashedString = Convert.ToBase64String(hashedBytes);
        return hashedString == hash;
    }

    private static SupplierAsnDto MapAsnDto(Asn asn)
    {
        return new SupplierAsnDto
        {
            AsnId = asn.Id,
            AsnNumber = asn.AsnNumber,
            SupplierId = asn.SupplierId,
            SupplierName = asn.Supplier?.SupplierName,
            PurchaseOrderId = asn.PurchaseOrderId ?? Guid.Empty,
            PurchaseOrderNumber = asn.PurchaseOrder?.PoNumber,
            ShipDate = asn.ShipDate,
            ExpectedArrivalDate = asn.ExpectedArrival ?? DateTime.MinValue,
            CarrierName = asn.Carrier,
            TrackingNumber = asn.TrackingNumber,
            Status = asn.Status,
            CreatedAt = asn.CreatedAt,
            ItemCount = asn.Items.Count,
            TotalShippedQty = asn.Items.Sum(i => i.ShippedQty),
            Items = asn.Items.Select(i => new SupplierAsnItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                ShippedQty = i.ShippedQty,
                LotNumber = i.LotNumber,
                ManufactureDate = i.ManufactureDate,
                ExpiryDate = i.ExpiryDate
            }).ToList()
        };
    }
}
