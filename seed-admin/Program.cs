using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BCrypt.Net;

Console.WriteLine("ERP Admin User Seeder");
Console.WriteLine("====================");

var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connString))
{
    Console.WriteLine("DB_CONNECTION_STRING is required.");
    Environment.Exit(1);
}

var adminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD");
if (string.IsNullOrWhiteSpace(adminPassword))
{
    Console.WriteLine("ADMIN_INITIAL_PASSWORD is required.");
    Environment.Exit(1);
}
var requiredAdminPassword = adminPassword!;

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

Console.WriteLine("Connected to database.");

// Check if admin exists
var checkCmd = new NpgsqlCommand("SELECT \"Id\" FROM \"Users\" WHERE \"Username\" = 'admin'", conn);
var adminId = await checkCmd.ExecuteScalarAsync();

if (adminId == null)
{
    Console.WriteLine("Creating admin user...");
    
    // Create admin role if not exists
    var roleCheckCmd = new NpgsqlCommand("SELECT \"Id\" FROM \"Roles\" WHERE \"RoleName\" = 'Admin'", conn);
    var roleId = await roleCheckCmd.ExecuteScalarAsync();
    
    if (roleId == null)
    {
        var insertRole = new NpgsqlCommand(
            "INSERT INTO \"Roles\" (\"Id\", \"RoleName\", \"Description\", \"IsSystem\", \"CreatedAt\") VALUES (@id, 'Admin', '系統管理員', true, @created) RETURNING \"Id\"",
            conn);
        insertRole.Parameters.AddWithValue("id", Guid.NewGuid());
        insertRole.Parameters.AddWithValue("created", DateTime.UtcNow);
        roleId = await insertRole.ExecuteScalarAsync();
        Console.WriteLine("Admin role created.");
    }
    
    // Create admin user
    var userId = Guid.NewGuid();
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(requiredAdminPassword);
    
    var insertUser = new NpgsqlCommand(
        @"INSERT INTO ""Users"" (""Id"", ""Username"", ""PasswordHash"", ""Email"", ""IsActive"", ""MustChangePassword"", ""CreatedAt"", ""UpdatedAt"", ""PasswordHistoryCount"") 
          VALUES (@id, 'admin', @password, 'admin@erp.local', true, false, @created, @updated, 0)",
        conn);
    insertUser.Parameters.AddWithValue("id", userId);
    insertUser.Parameters.AddWithValue("password", passwordHash);
    insertUser.Parameters.AddWithValue("created", DateTime.UtcNow);
    insertUser.Parameters.AddWithValue("updated", DateTime.UtcNow);
    await insertUser.ExecuteNonQueryAsync();
    
    // Create user role
    var insertUserRole = new NpgsqlCommand(
        @"INSERT INTO ""UserRoles"" (""Id"", ""UserId"", ""RoleId"") VALUES (@id, @userId, @roleId)",
        conn);
    insertUserRole.Parameters.AddWithValue("id", Guid.NewGuid());
    insertUserRole.Parameters.AddWithValue("userId", userId);
    insertUserRole.Parameters.AddWithValue("roleId", roleId!);
    await insertUserRole.ExecuteNonQueryAsync();
    
    Console.WriteLine("Admin user created successfully!");
}
else
{
    Console.WriteLine("Admin user already exists. Resetting password...");
    
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(requiredAdminPassword);
    var updateCmd = new NpgsqlCommand(
        "UPDATE \"Users\" SET \"PasswordHash\" = @password, \"IsActive\" = true, \"UpdatedAt\" = @updated WHERE \"Username\" = 'admin'",
        conn);
    updateCmd.Parameters.AddWithValue("password", passwordHash);
    updateCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);
    await updateCmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("Password reset to: admin123");
}

Console.WriteLine("====================");
Console.WriteLine("Login credentials:");
Console.WriteLine("  Username: admin");
Console.WriteLine("  Password: from ADMIN_INITIAL_PASSWORD");
