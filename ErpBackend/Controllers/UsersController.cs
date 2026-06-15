using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using ErpBackend.Validation;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ErpBackend.Controllers;

public class UsersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public UsersController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll()
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var search = GetSearch();

        var query = _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || (u.Email != null && u.Email.Contains(search)));

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = users.Select(MapToDto).ToList();
        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound(ApiHelper.Fail("使用者不存在"));
        return Ok(MapToDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        if (request == null)
            return BadRequest(ApiHelper.Fail("請提供使用者資訊"));

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(ApiHelper.Fail("帳號已存在"));

        // 密碼強度驗證
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
            return BadRequest(ApiHelper.Fail("密碼長度至少 8 個字元"));
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"[A-Za-z]") ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"[0-9]"))
            return BadRequest(ApiHelper.Fail("密碼必須包含英文字母和數字"));

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Email = request.Email,
            EmployeeId = request.EmployeeId
        };

        _db.Users.Add(user);

        foreach (var roleId in request.RoleIds)
        {
            if (await _db.Roles.AnyAsync(r => r.Id == roleId))
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await _db.SaveChangesAsync();

        var created = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id);

        return Ok(MapToDto(created), "建立成功");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(ApiHelper.Fail("¨Ï¥ÎªÌ¤£¦s¦b"));

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok(new object(), "使用者已停用");
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        EmployeeId = user.EmployeeId,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        Roles = user.UserRoles.Select(ur => ur.Role?.RoleName ?? "").ToList()
    };
}
